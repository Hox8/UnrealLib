using System;
using System.Diagnostics;
using System.IO;
using UnrealLib.Enums;
using System.IO.Compression;


#if WITH_POOLING
using System.Buffers;
#endif

namespace UnrealLib.Core.Compression;

/// <summary>
/// A static class containing UE3-related compression methods.
/// </summary>
/// <remarks>
/// For generic compression methods (without any chunking), see <see cref="ZlibStream.CompressBuffer(byte[], Stream, int, CompressionLevel)"/>
/// and <see cref="ZlibStream.UncompressBuffer(byte[], Stream, int)"/>.
/// </remarks>
public static class CompressionManager
{
    /// <summary>
    /// Compresses an <see cref="UnrealPackage"/> using the ZLib compression scheme.
    /// </summary>
    /// <param name="upk">The UnrealPackage to compress.</param>
    internal unsafe static void CompressPackage(UnrealPackage upk)
    {
        Debug.Assert(!upk.Summary.IsStoredCompressed, "UnrealPackage is already compressed!");

        new ChunkHelper().CalculateChunks(upk);

        // Open a temporary FileStream to store compressed data
        var options = new FileStreamOptions { Access = FileAccess.ReadWrite, Mode = FileMode.Create, Options = FileOptions.DeleteOnClose };
        FileStream outputStream = File.Open(upk.FullName + ".temp", options);

        // Set the positions of both streams to the end of their summaries. OutputStream needs to account for the added compressed chunks
        upk.Position = upk.Summary.OffsetEnd;
        outputStream.Position = upk.Summary.OffsetEnd + (sizeof(FCompressedChunk) * upk.Summary.CompressedChunks.Length);

        // Start compressing data according to the chunks we've just calculated
        for (int i = 0; i < upk.Summary.CompressedChunks.Length; i++)
        {
            int compressedOffset = (int)outputStream.Position;

            Compress(upk._buffer, outputStream, upk.Summary.CompressedChunks[i].UncompressedSize);

            int compressedSize = (int)outputStream.Position - compressedOffset;

            upk.Summary.CompressedChunks[i].CompressedOffset = compressedOffset;
            upk.Summary.CompressedChunks[i].CompressedSize = compressedSize;
        }

        // Create an UnrealArchive wrapper around the output stream
        var wrapper = new UnrealArchive(outputStream) { IsLoading = false, Position = 0 };

        // Serialize the updated Summary
        wrapper.Serialize(ref upk.Summary);

        upk._buffer.Dispose();
        upk._buffer = wrapper._buffer;
    }

    /// <summary>
    /// Decompresses a compressed <see cref="UnrealPackage"/>. Its compression chunks and flags are cleared upon success.
    /// </summary>
    /// <param name="upk">The <see cref="UnrealPackage"/> to decompress.</param>
    internal static void DecompressPackage(UnrealPackage upk)
    {
        // Don't try to decompress packages without any stored chunks
        if (upk.Summary.CompressedChunks.Length > 0)
        {
            // Open a temporary delete-on-close stream for holding the decompressed package data
            var options = new FileStreamOptions { Access = FileAccess.ReadWrite, Mode = FileMode.Create, Options = FileOptions.DeleteOnClose };
            Stream outputStream = File.Open(upk.FullName + ".temp", options);

            // Position streams so they're ready to decompress the first chunk "collection"
            upk.Position = upk.Summary.CompressedChunks[0].CompressedOffset;
            outputStream.Position = upk.Summary.CompressedChunks[0].UncompressedOffset;

            // Decompress file chunks
            foreach (var chunk in upk.Summary.CompressedChunks)
            {
                Decompress(upk._buffer, outputStream);

                // Watch for bugs: verify we're at the expected offsets
                Debug.Assert(upk._buffer.Position == chunk.CompressedOffset + chunk.CompressedSize);
                Debug.Assert(outputStream.Position == chunk.UncompressedOffset + chunk.UncompressedSize);
            }

            // Close the original and switch over to the newly-decompressed stream
            upk._buffer.Dispose();
            upk._buffer = outputStream;

            // Seek back to the end of the uncompressed package summary so we can continue serializing the rest of the UPK
            upk.Position = upk.Summary.CompressedChunks[0].UncompressedOffset;

            // Clear out compressed chunks
            upk.Summary.CompressedChunks = [];
        }

        // Clear any compression flags
        upk.Summary.CompressionFlags = CompressionFlags.None;
        upk.Summary.PackageFlags &= ~PackageFlags.StoreCompressed;
    }

    /// <summary>
    /// Inflates a ZLib-compressed block of data from inputStream to outputBytes.
    /// </summary>
    /// <param name="inputStream">The stream to read the compressed data from.</param>
    /// <param name="outputBytes">The byte array to inflate the data to. Size must be set beforehand!</param>
    public static void Decompress(Stream inputStream, byte[] outputBytes)
    {
        var ms = new MemoryStream(outputBytes);
        Decompress(inputStream, ms);
    }

    /// <summary>
    /// Inflates a ZLib-compressed block of data from inputStream, writing the inflated data directly to outputStream.
    /// </summary>
    /// <param name="inputStream">The stream to read the compressed data from.</param>
    /// <param name="outputStream">The stream to receive the decompressed data.</param>
    public static void Decompress(Stream inputStream, Stream outputStream)
    {
        uint packageTag = default;
        int chunkSize = default;
        int compressedSize = default;
        int uncompressedSize = default;

        // Create an UnrealArchive wrapper over the input so we can use the serialize API
        var inputWrapper = new UnrealArchive(inputStream);

        inputWrapper.Serialize(ref packageTag);
        inputWrapper.Serialize(ref chunkSize);
        inputWrapper.Serialize(ref compressedSize);
        inputWrapper.Serialize(ref uncompressedSize);

        ArgumentOutOfRangeException.ThrowIfNotEqual(packageTag, Globals.PackageTag);

        int chunkCount = (uncompressedSize + chunkSize - 1) / chunkSize;
        var chunks = new FCompressedChunkInfo[chunkCount];
        inputWrapper.Serialize(ref chunks, chunks.Length);

#if WITH_POOLING
        byte[] buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
#else
        byte[] buffer = new byte[chunkSize];
#endif

        // Inflate chunks
        foreach (var chunk in chunks)
        {
            inputStream.ReadExactly(buffer, 0, chunk.CompressedSize);

            // Compression window needs to be reset for every chunk
            // @TODO inefficient. CopyTo() creates its own set of buffers. Should use Read() instead
            new ZLibStream(new MemoryStream(buffer, 0, chunk.CompressedSize, false), CompressionMode.Decompress).CopyTo(outputStream);
        }

#if WITH_POOLING
        ArrayPool<byte>.Shared.Return(buffer);
#endif
    }

    /// <summary>
    /// Deflates the contents of inputBytes and writes the compressed data to outputStream.
    /// </summary>
    /// <param name="inputStream">The stream to write the compressed data to.</param>
    /// <param name="inputBytes">The data to compress.</param>
    public static void Compress(Stream inputStream, byte[] inputBytes)
    {
        var ms = new MemoryStream(inputBytes);
        Compress(inputStream, ms, inputBytes.Length);
    }

    /// <summary>
    /// Deflates the first uncompressedLength bytes from inputStream and writes the compressed data to outputStream.
    /// </summary>
    /// <param name="inputStream">The stream to read the uncompressed data from.</param>
    /// <param name="outputStream">The stream to receive the compressed data.</param>
    /// <param name="uncompressedLength">The number of bytes to read from inputStream.</param>
    public static void Compress(Stream inputStream, Stream outputStream, int uncompressedLength)
    {
        uint packageTag = Globals.PackageTag;
        int chunkSize = Globals.CompressionChunkSize;
        int compressedSize = default;
        int uncompressedSize = uncompressedLength;

        // Create an UnrealArchive wrapper over the output so we can use the serialize API
        var outputWrapper = new UnrealArchive(outputStream) { IsLoading = false };

        // Mark offset of chunk info
        long statStartPos = outputWrapper.Position;

        // Write out dummy stats. We'll reserialize later with accurate ones
        outputWrapper.Serialize(ref packageTag);
        outputWrapper.Serialize(ref chunkSize);
        outputWrapper.Serialize(ref compressedSize);
        outputWrapper.Serialize(ref uncompressedSize);

        int chunkCount = (uncompressedSize + chunkSize - 1) / chunkSize;
        var chunks = new FCompressedChunkInfo[chunkCount];

        // Initialize chunks
        for (int i = 0; i < chunks.Length - 1; i++)
        {
            // All non-final chunks' UncompressedSize will equal their ChunkSize
            chunks[i] = new() { CompressedSize = default, UncompressedSize = chunkSize };
        }
        // The final chunk is not guaranteed to have ChunkSize uncompressed size
        chunks[^1] = new() { CompressedSize = default, UncompressedSize = uncompressedSize % chunkSize };

        // Write out chunk infos
        outputWrapper.Serialize(ref chunks, chunks.Length);

        // Deflate chunks
        for (int i = 0; i < chunks.Length; i++)
        {
            long prePos = outputStream.Position;

            // Chunks are isolated--do not reuse the same ZLibStream across chunks!
            // Using guarantees the deflator flushes
            using (var deflator = new ZLibStream(outputStream, CompressionLevel.SmallestSize, true))
            {
                inputStream.ConstrainedCopy(deflator, chunks[i].UncompressedSize, chunkSize);
            }

            chunks[i].CompressedSize = (int)(outputStream.Position - prePos);
            compressedSize += chunks[i].CompressedSize;
        }

        // Reserialize updated chunk stats
        outputWrapper.Position = statStartPos;
        outputWrapper.Serialize(ref packageTag);
        outputWrapper.Serialize(ref chunkSize);
        outputWrapper.Serialize(ref compressedSize);
        outputWrapper.Serialize(ref uncompressedSize);
        outputWrapper.Serialize(ref chunks, chunks.Length);

        // Seek back to the end
        outputWrapper.Position = outputWrapper.Length;
    }
}
