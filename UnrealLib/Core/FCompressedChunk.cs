namespace UnrealLib.Core;

public readonly struct FCompressedChunk
{
    public readonly int UncompressedOffset, UncompressedSize, CompressedOffset, CompressedSize;

    public override string ToString() =>
        $"Compressed: {CompressedOffset}/{CompressedSize}, Uncompressed: {UncompressedOffset}/{UncompressedSize}";
}
