using UnrealLib.Core;
using UnrealLib.Enums.Textures;

namespace UnrealLib.Experimental.Textures;

public class ULightMapTexture2D(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UTexture2D(stream, pkg, export)
{
    // private LightmapFlags LightmapFlags;

    //public override void Serialize(UnrealStream stream)
    //{
    //    base.Serialize(stream);

    //    stream.Serialize(ref LightmapFlags);
    //    LODGroup = TextureGroup.Lightmap;
    //}
}
