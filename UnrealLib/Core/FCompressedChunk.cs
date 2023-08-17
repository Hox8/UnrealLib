using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FCompressedChunk : ISerializable
{
    private int UncompressedOffset;
    private int UncompressedSize;
    private int CompressedOffset;
    private int CompressedSize;

    public void Serialize(UnrealStream UStream)
    {
        UStream.Serialize(ref UncompressedOffset);
        UStream.Serialize(ref UncompressedSize);
        UStream.Serialize(ref CompressedOffset);
        UStream.Serialize(ref CompressedSize);
    }

    public override string ToString() =>
        $"Compressed: {CompressedOffset}/{CompressedSize}, Uncompressed: {UncompressedOffset}/{UncompressedSize}";
}