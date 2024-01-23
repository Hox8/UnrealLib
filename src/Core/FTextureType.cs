using UnrealLib.Enums.Textures;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public struct FTextureAllocations : ISerializable
{
    public struct FTextureType : ISerializable
    {
        internal int SizeX;
        internal int SizeY;
        internal int NumMips;
        internal PixelFormat Format;
        internal TextureCreateFlags TexCreateFlags;
        internal int[] ExportIndicies;

        public void Serialize(UnrealArchive Ar)
        {
            Ar.Serialize(ref SizeX);
            Ar.Serialize(ref SizeY);
            Ar.Serialize(ref NumMips);
            Ar.Serialize(ref Format);
            Ar.Serialize(ref TexCreateFlags);
            Ar.Serialize(ref ExportIndicies);
        }

        public readonly override string ToString() => $"{SizeX}x{SizeY} ({ExportIndicies.Length})";
    }

    internal FTextureType[] TextureTypes;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref TextureTypes);
    }
}