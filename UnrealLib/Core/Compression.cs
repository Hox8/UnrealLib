namespace UnrealLib.Core;

/// <summary>
/// Information about a compressed chunk within a file.
/// </summary>
public readonly struct FCompressedChunk
{
    public readonly int UncompressedOffset, UncompressedSize, CompressedOffset, CompressedSize;
}

/// <summary>
/// Size information regarding an individual compressed chunk.
/// </summary>
public readonly struct FCompressedChunkInfo
{
    public readonly int CompressedSize, UncompressedSize;
}