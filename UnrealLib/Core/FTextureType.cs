using System.Collections.Generic;
using UnrealLib.Enums.Textures;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

class FTextureAllocations : ISerializable
{
    class FTextureType : ISerializable
    {
        private int SizeX;
        private int SizeY;
        private int NumMips;
        private PixelFormat Format;
        private TextureCreateFlags TexCreateFlags;
        private List<int> ExportIndicies;

        public void Serialize(UnrealStream stream)
        {
            stream.Serialize(ref SizeX);
            stream.Serialize(ref SizeY);
            stream.Serialize(ref NumMips);
            stream.Serialize(ref Format);
            stream.Serialize(ref TexCreateFlags);
            stream.Serialize(ref ExportIndicies);
        }
    }

    List<FTextureType> TextureTypes;

    public void Serialize(UnrealStream stream)
    {
        stream.Serialize(ref TextureTypes);
    }
}