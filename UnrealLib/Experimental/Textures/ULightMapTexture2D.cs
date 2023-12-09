using UnrealLib.Core;
using UnrealLib.Enums.Textures;

namespace UnrealLib.Experimental.Textures;

public class ULightMapTexture2D(FObjectExport export) : UTexture2D(export)
{
    // private LightmapFlags LightmapFlags;

    //public override void Serialize(UnrealArchive stream)
    //{
    //    base.Serialize(stream);

    //    stream.Serialize(ref LightmapFlags);
    //    LODGroup = TextureGroup.Lightmap;
    //}
}
