#if DEBUG
    // Print detailed compress info to the console if desired
    #define TRACK_COMPRESS_STATS
#endif

using System;
using System.Buffers;
using System.IO;

namespace UnrealLib.Core.Compression;

#region Enums

/// <summary>
/// Flags controlling [de]compression.
/// </summary>
/// <remarks>
/// NOTE: Gears of War: Ultimate/Reloaded use values '4' and '32' to denote LZ4 compression.
/// </remarks>
[Flags]
public enum CompressionFlags
{
    /// <summary>
    /// No compression.
    /// </summary>
    None = 0,

    /// <summary>
    /// ZLib compression.
    /// </summary>
    ZLib = 1 << 0,

    /// <summary>
    /// LZO compression.
    /// </summary>
    LZO = 1 << 1,

    /// <summary>
    /// LZX compression.
    /// </summary>
    LZX = 1 << 2,

    /// <summary>
    /// Favor smaller size over decompression speed when saving.
    /// </summary>
    BiasMemory = 1 << 4,

    /// <summary>
    /// Favor faster [de]compression speed at the expense of file size when saving.
    /// </summary>
    BiasSpeed = 1 << 5,

    /// <summary>
    /// Indicates decompression should not happen on PS3 SPUs.
    /// </summary>
    ForcePPUDecompressZLib = 1 << 7,

    // If entries are added these may need adjustment!
    TypeMask = 0x0F,
    OptionsMask = 0xF0,
}

#endregion

#region Structs

/// <summary>
/// Stores information regarding a compressed set of data and its corresponding chunks.
/// </summary>
public struct CompressedChunkHeader
{
    public uint PackageTag;         // This should not be a field...
    public int ChunkSize;
    public int CompressedSize;
    public int UncompressedSize;
}

/// <summary>
/// Stores information regarding a compressed chunk within a file.
/// </summary>
public struct CompressedChunk
{
    public int UncompressedOffset;
    public int UncompressedSize;
    public int CompressedOffset;
    public int CompressedSize;
}

/// <summary>Helper struct regarding a chunk of compressed data.</summary>
/// <remarks>
/// <see cref="CompressedChunkInfo"/> are followed immediately by the compressed chunk it describes.
/// </remarks>
public struct CompressedChunkInfo
{
    public int CompressedSize;
    public int UncompressedSize;
}

#endregion

/// <summary>
/// UE3-related compression methods.
/// </summary>
/// <remarks>
/// For generic compression methods (without any chunking), see <see cref="ZlibStream.CompressBuffer(byte[], Stream, int, CompressionLevel)"/>
/// and <see cref="ZlibStream.UncompressBuffer(byte[], Stream, int)"/>.
/// </remarks>
public static partial class CompressionManager
{
    /// <summary>
    /// Compresses an <see cref="UnrealPackage"/> using the ZLib compression scheme.
    /// </summary>
    /// <param name="upk">The UnrealPackage to compress.</param>
    internal unsafe static bool CompressPackage(UnrealPackage upk)
    {
        UDebug.Assert(!upk.IsSaving, "Package compression must come AFTER package has finished saving!");

#if TRACK_COMPRESS_STATS
        var compressTimer = System.Diagnostics.Stopwatch.StartNew();
        var initialSize = upk.Length;
#endif

        upk.CalculateCompressionChunks();

        // Open a temporary FileStream to store compressed data
        using FileStream outputStream = File.Create($"{upk.FileStream.Name}.compressed.temp");

        // Set the positions of both streams to the end of their summaries. OutputStream needs to account for the added compressed chunks
        upk.Position = upk.NameOffset;
        outputStream.Position = upk.NameOffset + (sizeof(CompressedChunk) * upk.CompressedChunks.Count);

        // Start compressing data according to the chunks we've just calculated
        foreach (ref var chunk in upk.CompressedChunks.AsSpan())
        {
            int compressedOffset = (int)outputStream.Position;

            if (!Compress(upk.FileStream, outputStream, chunk.UncompressedSize, upk.Summary.CompressionFlags))
            {
                // Compression failed.
                return false;
            }

            int compressedSize = (int)outputStream.Position - compressedOffset;

            chunk.CompressedOffset = compressedOffset;
            chunk.CompressedSize = compressedSize;
        }

        // Create an UnrealArchive wrapper around the output stream.
        // It's important that we specify position 0 in the constructor
        // so we can write the updated summary to the start of the file.
        var wrapper = new UnrealArchive(outputStream) { IsLoading = false, Position = 0 };

        // Set compression stats
        upk.Summary.CompressionFlags = CompressionFlags.ZLib;
        upk.Summary.PackageFlags |= PackageFlags.StoreCompressed;

        // Serialize the updated Summary
        wrapper.Serialize(ref upk.Summary);

#if TRACK_COMPRESS_STATS
        // Print compression stats
        compressTimer.Stop();

        int finalSize = (int)outputStream.Length;

        string sizeDeltaString = Globals.FormatSize(finalSize - initialSize);
        string sizePercentageString = $"{(float)finalSize / initialSize:F3}%";
        string timeString = $"{compressTimer.ElapsedMilliseconds}ms";

        UDebug.Log($"PACKAGE SAVE: Compressed package in {timeString} ({sizeDeltaString}, {sizePercentageString})");
#endif

        // Close both streams
        upk._stream.Close();
        outputStream.Close();

        // Overwrite the uncompressed upk with our new compressed one
        File.Move(outputStream.Name, upk.FileStream.Name, true);

        return true;
    }

    /// <summary>
    /// Decompresses a compressed <see cref="UnrealPackage"/>. Its compression chunks and flags are cleared upon success.
    /// </summary>
    /// <param name="upk">The <see cref="UnrealPackage"/> to decompress.</param>
    internal static bool DecompressPackage(UnrealPackage upk)
    {
        // Don't try to decompress packages without any stored chunks
        if (upk.CompressedChunks.Count > 0)
        {
            // Open a temporary delete-on-close stream for holding the decompressed package data
            var outputStream = File.Open($"{upk.FileStream.Name}.decompressed.temp", Globals.TempReadWriteStream);

            // Position streams so they're ready to decompress the first chunk "collection"
            upk.Position = upk.CompressedChunks[0].CompressedOffset;
            outputStream.Position = upk.CompressedChunks[0].UncompressedOffset;

            // Decompress file chunks
            foreach (var chunk in upk.CompressedChunks)
            {
                if (!Decompress(upk.FileStream, outputStream, upk.Summary.CompressionFlags))
                {
                    // Decompression failed
                    return false;
                }

                // Watch for bugs: verify we're at the expected offsets
                UDebug.Assert(upk._stream.Position == chunk.CompressedOffset + chunk.CompressedSize);
                UDebug.Assert(outputStream.Position == chunk.UncompressedOffset + chunk.UncompressedSize);
            }

            // Close the original and switch over to the newly-decompressed stream
            upk._stream.Dispose();
            upk._stream = outputStream;

            // Seek to the start of the first chunk so we can continue serializing
            upk.Position = upk.CompressedChunks[0].UncompressedOffset;

            // Clear out our compressed chunks
            upk.CompressedChunks.Clear();
        }

        // Clear any compression flags
        // Actually, keep existing compression flags so we can determine if we should re-compress on save
        // upk.Summary.CompressionFlags = CompressionFlags.None;
        // upk.Summary.PackageFlags &= ~PackageFlags.StoreCompressed;

        return true;
    }

    /// <summary>
    /// Inflates a ZLib-compressed block of data from inputStream, writing the inflated data directly to outputStream.
    /// </summary>
    /// <param name="inputStream">The stream to read the compressed data from.</param>
    /// <param name="outputStream">The stream to receive the decompressed data.</param>
    internal static bool Decompress(FileStream inputStream, Stream outputStream, CompressionFlags flags)
    {
        // Create an UnrealArchive wrapper over the input so we can use the serialize API
        var inputWrapper = new UnrealArchive(inputStream);

        CompressedChunkHeader header = default;
        inputWrapper.Serialize(ref header);

        ArgumentOutOfRangeException.ThrowIfNotEqual(header.PackageTag, Globals.PackageTag);

        int chunkCount = (header.UncompressedSize + header.ChunkSize - 1) / header.ChunkSize;
        CompressedChunkInfo[] chunks = default!;
        inputWrapper.Serialize(ref chunks, chunkCount);

        if (chunks.Length > 0)
        {
            // We can't guarantee compressed chunks aren't _larger_ than their chunk size.
            // Find the biggest size so we can safely allocate one buffer for all incoming chunks

            int maxCompressedSize = chunks[0].CompressedSize;
            int maxUncompressedSize = chunks[0].UncompressedSize;

            for (int i = 1; i < chunks.Length; i++)
            {
                maxCompressedSize = Math.Max(chunks[i].CompressedSize, maxCompressedSize);
                maxUncompressedSize = Math.Max(chunks[i].UncompressedSize, maxUncompressedSize);
            }

            byte[] compressedBuffer = ArrayPool<byte>.Shared.Rent(maxCompressedSize);
            byte[] uncompressedBuffer = ArrayPool<byte>.Shared.Rent(maxUncompressedSize);

            // Decompress chunks
            foreach (var chunk in chunks)
            {
                inputStream.ReadExactly(compressedBuffer, 0, chunk.CompressedSize);

                switch (flags & CompressionFlags.TypeMask)
                {
                    case CompressionFlags.ZLib:
                        Decompress_ZLib(compressedBuffer, chunk.CompressedSize, uncompressedBuffer, chunk.UncompressedSize);
                        break;

                    case CompressionFlags.LZO:
                        Decompress_LZO(compressedBuffer, chunk.CompressedSize, uncompressedBuffer, chunk.UncompressedSize);
                        break;

                    default:
                        // Unsupported compression scheme!
                        return false;
                }

                outputStream.Write(uncompressedBuffer, 0, chunk.UncompressedSize);
            }

            ArrayPool<byte>.Shared.Return(compressedBuffer);
            ArrayPool<byte>.Shared.Return(uncompressedBuffer);
        }

        return true;
    }

    /// <summary>
    /// Deflates the first uncompressedLength bytes from inputStream and writes the compressed data to outputStream.
    /// </summary>
    /// <param name="inputStream">The stream to read the uncompressed data from.</param>
    /// <param name="outputStream">The stream to receive the compressed data.</param>
    /// <param name="uncompressedLength">The number of bytes to read from inputStream.</param>
    internal static bool Compress(FileStream inputStream, FileStream outputStream, int uncompressedLength, CompressionFlags flags)
    {
        CompressedChunkHeader header = new()
        {
            PackageTag = Globals.PackageTag,
            ChunkSize = Globals.CompressionChunkSize,
            UncompressedSize = uncompressedLength,
        };

        // Create an UnrealArchive wrapper over the output so we can use the serialize API
        var outputWrapper = new UnrealArchive(outputStream) { IsLoading = false };

        // Mark offset of chunk info
        var statStartPos = outputWrapper.Position;

        // Write out dummy stats. We'll reserialize later with accurate ones
        outputWrapper.Serialize(ref header);

        int chunkCount = (header.UncompressedSize + header.ChunkSize - 1) / header.ChunkSize;
        var chunks = new CompressedChunkInfo[chunkCount];

        // Initialize chunks
        for (int i = 0; i < chunks.Length - 1; i++)
        {
            // All non-final chunks' UncompressedSize will equal their ChunkSize
            chunks[i].UncompressedSize = header.ChunkSize;
        }

        // The final chunk is not guaranteed to have ChunkSize uncompressed size
        chunks[^1].UncompressedSize = header.UncompressedSize % header.ChunkSize;

        // If the final chunk has a size of zero, it must be the exact same size as the max chunk size
        if (chunks[^1].UncompressedSize == 0)
        {
            chunks[^1].UncompressedSize = header.ChunkSize;
        }

        // Write out chunk infos
        outputWrapper.Serialize(ref chunks, chunks.Length);

        // Compress chunks
        for (int i = 0; i < chunks.Length; i++)
        {
            var prePos = outputStream.Position;

            switch (flags & CompressionFlags.TypeMask)
            {
                case CompressionFlags.ZLib:
                    Compress_ZLib(inputStream, outputStream, chunks[i].UncompressedSize);
                    break;

                case CompressionFlags.LZO:
                    Compress_LZO(inputStream, outputStream, chunks[i].UncompressedSize);
                    break;

                default:
                    // Unsupported compression scheme!
                    return false;
            }

            chunks[i].CompressedSize = (int)(outputStream.Position - prePos);
            header.CompressedSize += chunks[i].CompressedSize;

            if (chunks[i].CompressedSize > chunks[i].UncompressedSize)
            {
                UDebug.Warn($"Compressed chunk is larger than chunk size! {Globals.FormatSize(chunks[i].CompressedSize)} > {Globals.FormatSize(chunks[i].UncompressedSize)}");
            }
        }

        // Reserialize updated chunk stats
        outputWrapper.Position = statStartPos;
        outputWrapper.Serialize(ref header);
        outputWrapper.Serialize(ref chunks, chunks.Length);

        // Seek back to the end
        outputWrapper.Position = outputWrapper.Length;

        // Succeeded.
        return true;
    }
}
