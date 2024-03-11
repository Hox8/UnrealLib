using UnrealLib.Core;
using UnrealLib.Enums.Textures;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Textures;

public partial class ULightMapTexture2D(FObjectExport export) : UTexture2D(export)
{
    [UProperty] public new TextureGroup LODGroup = TextureGroup.TEXTUREGROUP_Lightmap;

    private ELightmapFlags LightmapFlags;

    public override void Serialize(UnrealArchive stream)
    {
        base.Serialize(stream);

        stream.Serialize(ref LightmapFlags);

        LODGroup = TextureGroup.TEXTUREGROUP_Lightmap;
    }
}
