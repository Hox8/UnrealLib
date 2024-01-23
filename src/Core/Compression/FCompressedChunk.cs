namespace UnrealLib.Core.Compression;

/// <summary>
/// Stores information regarding a compressed chunk within a file.
/// </summary>
public record struct FCompressedChunk
{
    public int UncompressedOffset;
    public int UncompressedSize;
    public int CompressedOffset;
    public int CompressedSize;
}
