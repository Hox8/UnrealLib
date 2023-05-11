namespace UnrealLib.Core
{
    public class FTextureType : IDeserializable<FTextureType>
    {
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int NumMips { get; set; }
        public EPixelFormat Format { get; set; }
        public ETextureCreateFlags TexCreateFlags { get; set; }
        public List<int> ExportIndicies { get; set; }

        public FTextureType Deserialize(UnrealStream unStream)
        {
            SizeX = unStream.ReadInt32();
            SizeY = unStream.ReadInt32();
            NumMips = unStream.ReadInt32();
            Format = (EPixelFormat)unStream.ReadInt32();
            TexCreateFlags = (ETextureCreateFlags)unStream.ReadInt32();
            ExportIndicies = unStream.ReadIntList();
            return this;
        }

        public override string ToString()
        {
            return $"{SizeX}x{SizeY}, {NumMips} mips, Format: {Format}, TexCreateFlags: {TexCreateFlags}, referenced by {ExportIndicies.Count} objects";
        }
    }
}
