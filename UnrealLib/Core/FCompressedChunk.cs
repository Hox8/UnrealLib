namespace UnrealLib.Core;

public struct FCompressedChunk
{
    private int UncompressedOffset, UncompressedSize, CompressedOffset, CompressedSize;

    public override string ToString() =>
        $"Compressed: {CompressedOffset}/{CompressedSize}, Uncompressed: {UncompressedOffset}/{UncompressedSize}";
}
