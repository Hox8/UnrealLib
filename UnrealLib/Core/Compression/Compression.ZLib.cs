using System.IO;
using System.IO.Compression;

namespace UnrealLib.Core.Compression;

public static partial class CompressionManager
{
    // Compression level 9 with default strategy is equivalent to CompressionLevel.SmallestSize

#if NET9_0_OR_GREATER
    internal static readonly ZLibCompressionOptions CompressionOptions = new()
    {
        CompressionLevel = 9,
        CompressionStrategy = ZLibCompressionStrategy.Default
    };
#else
    internal const CompressionLevel CompressionOptions = CompressionLevel.SmallestSize;
#endif

    /// <summary>
    /// Inflates a ZLib-compressed block of data from inputStream, writing the inflated data directly to outputStream.
    /// </summary>
    /// <param name="inputStream">The stream to read the compressed data from.</param>
    /// <param name="outputStream">The stream to receive the decompressed data.</param>
    public static void Decompress_ZLib(byte[] compressed, int compressedLength, byte[] uncompressed, int uncompressedLength)
    {
        using var ms = new MemoryStream(compressed, 0, compressedLength, false);
        using var inflator = new ZLibStream(ms, CompressionMode.Decompress);

        inflator.ReadExactly(uncompressed, 0, uncompressedLength);
    }

    /// <summary>
    /// Deflates the first uncompressedLength bytes from inputStream and writes the compressed data to outputStream.
    /// </summary>
    /// <param name="inputStream">The stream to read the uncompressed data from.</param>
    /// <param name="outputStream">The stream to receive the compressed data.</param>
    /// <param name="uncompressedLength">The number of bytes to read from inputStream.</param>
    public static void Compress_ZLib(FileStream inputStream, FileStream outputStream, int uncompressedLength)
    {
        // Chunks are isolated--do not reuse the same ZLibStream across chunks!
        // Using guarantees the deflator flushes
        using var deflator = new ZLibStream(outputStream, CompressionOptions, true);

        // @TODO may be good to set bufferSize to that of the chunk size (was previously using header.ChunkSize)
        inputStream.ConstrainedCopy(deflator, uncompressedLength);
    }
}
