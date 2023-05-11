namespace UnrealLib.Core
{
    public class FCompressedChunk : IDeserializable<FCompressedChunk>
    {
        public int UncompressedOffset { get; internal set; }
        public int UncompressedSize { get; internal set; }
        public int CompressedOffset { get; internal set; }
        public int CompressedSize { get; internal set; }

        public FCompressedChunk Deserialize(UnrealStream unStream)
        {
            UncompressedOffset = unStream.ReadInt32();
            UncompressedSize = unStream.ReadInt32();
            CompressedOffset = unStream.ReadInt32();
            CompressedSize = unStream.ReadInt32();
            return this;
        }

        public override string ToString()
        {
            return
                $"Compressed: (Offset: {CompressedOffset}, Size: {CompressedSize}), " +
                $"Uncompressed: (Offset: {UncompressedOffset}, Size: {UncompressedSize})";
        }
    }
}
