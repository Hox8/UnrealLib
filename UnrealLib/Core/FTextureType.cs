using UnrealLib.Enums.Textures;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FTextureType : ISerializable
{
    private int SizeX;
    private int SizeY;
    private int NumMips;
    private PixelFormat Format;
    private TextureCreateFlags TexCreateFlags;
    private List<int> ExportIndicies;

    public void Serialize(UnrealStream UStream)
    {
        UStream.Serialize(ref SizeX);
        UStream.Serialize(ref SizeY);
        UStream.Serialize(ref NumMips);
        UStream.Serialize(ref Format);
        UStream.Serialize(ref TexCreateFlags);
        UStream.Serialize(ref ExportIndicies);
    }
    
    // @TODO: ToString() override
}