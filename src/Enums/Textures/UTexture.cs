using NetEscapades.EnumGenerators;

namespace UnrealLib.Enums.Textures;

[EnumExtensions]
public enum TextureCompressionSettings
{
    // Apparently this can be "None"??
    None,

    TC_Default,
    TC_Normalmap,
    TC_Displacementmap,
    TC_NormalmapAlpha,
    TC_Grayscale,
    TC_HighDynamicRange,
    TC_OneBitAlpha,
    TC_NormalmapUncompressed,
    TC_NormalmapBC5,
    TC_OneBitMonochrome,
    TC_SimpleLightmapModification,
    TC_VectorDisplacementmap
}

[EnumExtensions]
public enum TextureFilter
{
    TF_Nearest,
    TF_Linear
}

[EnumExtensions]
public enum TextureAddress
{
    TA_Wrap,
    TA_Clamp,
    TA_Mirror
}

[EnumExtensions]
public enum TextureGroup
{
    TEXTUREGROUP_World,
    TEXTUREGROUP_WorldNormalMap,
    TEXTUREGROUP_WorldSpecular,
    TEXTUREGROUP_Character,
    TEXTUREGROUP_CharacterNormalMap,
    TEXTUREGROUP_CharacterSpecular,
    TEXTUREGROUP_Weapon,
    TEXTUREGROUP_WeaponNormalMap,
    TEXTUREGROUP_WeaponSpecular,
    TEXTUREGROUP_Vehicle,
    TEXTUREGROUP_VehicleNormalMap,
    TEXTUREGROUP_VehicleSpecular,
    TEXTUREGROUP_Cinematic,
    TEXTUREGROUP_Effects,
    TEXTUREGROUP_EffectsNotFiltered,
    TEXTUREGROUP_Skybox,
    TEXTUREGROUP_UI,
    TEXTUREGROUP_Lightmap,
    TEXTUREGROUP_RenderTarget,
    TEXTUREGROUP_MobileFlattened,
    TEXTUREGROUP_ProcBuilding_Face,
    TEXTUREGROUP_ProcBuilding_LightMap,
    TEXTUREGROUP_Shadowmap,
    TEXTUREGROUP_ColorLookupTable,
    TEXTUREGROUP_Terrain_Heightmap,
    TEXTUREGROUP_Terrain_Weightmap,
    TEXTUREGROUP_ImageBasedReflection,
    TEXTUREGROUP_Bokeh
}

[EnumExtensions]
public enum TextureMipGenSettings
{
    // default for the "texture"
    TMGS_FromTextureGroup,
    // 2x2 average, default for the "texture group"
    TMGS_SimpleAverage,
    // 8x8 with sharpening: 0=no sharpening but better quality which is softer, 1..little, 5=medium, 10=extreme
    TMGS_Sharpen0,
    TMGS_Sharpen1,
    TMGS_Sharpen2,
    TMGS_Sharpen3,
    TMGS_Sharpen4,
    TMGS_Sharpen5,
    TMGS_Sharpen6,
    TMGS_Sharpen7,
    TMGS_Sharpen8,
    TMGS_Sharpen9,
    TMGS_Sharpen10,
    TMGS_NoMipmaps,
    // Do not touch existing mip chain as it contains generated data
    TMGS_LeaveExistingMips,
    // blur further (useful for image based reflections)
    TMGS_Blur1,
    TMGS_Blur2,
    TMGS_Blur3,
    TMGS_Blur4,
    TMGS_Blur5
}

[EnumExtensions]
public enum ETextureMipCount
{
    ResidentMips,
    AllMips,
    AllMipsBiased,
}
