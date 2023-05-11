namespace UnrealLib.Core
{
    public class FGenerationInfo : IDeserializable<FGenerationInfo>
    {
        public int ExportCount { get; internal set; }
        public int NameCount { get; internal set; }
        public int NetObjectCount { get; internal set; }

        public FGenerationInfo Deserialize(UnrealStream unStream)
        {
            ExportCount = unStream.ReadInt32();
            NameCount = unStream.ReadInt32();
            NetObjectCount = unStream.ReadInt32();
            return this;
        }

        public override string ToString()
        {
            return $"ExportCount: {ExportCount}, NameCount: {NameCount}, NetObjectCount: {NetObjectCount}";
        }
    }
}
