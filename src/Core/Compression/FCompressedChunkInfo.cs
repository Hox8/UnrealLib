namespace UnrealLib.Core.Compression;

/// <summary>
/// Helper struct regarding a chunk of compressed data.
/// </summary>
/// <remarks>
/// <see cref="FCompressedChunkInfo"/> are followed immediately by the compressed chunk it describes.
/// </remarks>
public record struct FCompressedChunkInfo
{
    public int CompressedSize;
    public int UncompressedSize;
}
