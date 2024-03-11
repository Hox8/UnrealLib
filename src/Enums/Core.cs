using NetEscapades.EnumGenerators;

namespace UnrealLib.Enums;

/// <summary>Detail mode for primitive component rendering.</summary>
[EnumExtensions]
public enum EDetailMode
{
    DM_Low,
    DM_Medium,
    DM_High,
};

/// <summary>
/// A priority for sorting scene elements by depth.
/// Elements with higher priority occlude elements with lower priority, disregarding distance.
/// </summary>

[EnumExtensions]
public enum ESceneDepthPriorityGroup
{
    /// <summary>UnrealEd background scene DPG.</summary>
    SDPG_UnrealEdBackground,
    /// <summary>World scene DPG.</summary>
    SDPG_World,
    /// <summary>Foreground scene DPG.</summary>
    SDPG_Foreground,
    /// <summary>UnrealEd scene DPG.</summary>
    SDPG_UnrealEdForeground,
    /// <summary>After all scene rendering.</summary>
    SDPG_PostProcess
};

/// <summary>
/// Possible options for blend factor source for blending between textures.
/// </summary>
[EnumExtensions]
public enum EMobileTextureBlendFactorSource
{
    /// <summary>
    /// From the vertex color's red channel.
    /// </summary>
    MTBFS_VertexColor,

    /// <summary>
    /// From the mask texture's alpha.
    /// </summary>
    MTBFS_MaskTexture,
};

/// <summary>
/// Possible vertex texture coordinate sets that may used to sample textures on mobile platforms.
/// </summary>
[EnumExtensions]
public enum EMobileTexCoordsSource
{
    /// <summary>
    /// First texture coordinate from mesh vertex.
    /// </summary>
    MTCS_TexCoords0,

    /// <summary>
    /// Second texture coordinate from mesh vertex.
    /// </summary>
    MTCS_TexCoords1,

    /// <summary>
    /// Third texture coordinate from mesh vertex.
    /// </summary>
    MTCS_TexCoords2,

    /// <summary>
    /// Fourth texture coordinate from mesh vertex.
    /// </summary>
    MTCS_TexCoords3,
};

/// <summary>
/// Possible vertex texture coordinate sets that may used to sample textures on mobile platforms.
/// </summary>
[EnumExtensions]
public enum EMobileAlphaValueSource
{
    /// <summary>
    /// Default in the diffuse channel alpha.
    /// </summary>
    MAVS_DiffuseTextureAlpha,

    /// <summary>
    /// Mask Texture Red Channel (platforms with no alpha texture compression).
    /// </summary>
    MAVS_MaskTextureRed,

    /// <summary>
    /// Mask Texture Green Channel (platforms with no alpha texture compression).
    /// </summary>
    MAVS_MaskTextureGreen,

    /// <summary>
    /// Mask Texture Blue Channel (platforms with no alpha texture compression).
    /// </summary>
    MAVS_MaskTextureBlue,
};

/// <summary>
/// Possible environment map blend modes.
/// </summary>
[EnumExtensions]
public enum EMobileEnvironmentBlendMode
{
    /// <summary>
    /// Add environment map to base color.
    /// </summary>
    MEBM_Add,

    /// <summary>
    /// Lerp between base color and environment color.
    /// </summary>
    MEBM_Lerp
};

[EnumExtensions]
public enum EMobileSpecularMask
{
    MSM_Constant,
    MSM_Luminance,
    MSM_DiffuseRed,
    MSM_DiffuseGreen,
    MSM_DiffuseBlue,
    MSM_DiffuseAlpha,
    MSM_MaskTextureRGB,
    MSM_MaskTextureRed,
    MSM_MaskTextureGreen,
    MSM_MaskTextureBlue,
    MSM_MaskTextureAlpha
};

[EnumExtensions]
public enum EMobileAmbientOcclusionSource
{
    MAOS_Disabled,
    MAOS_VertexColorRed,
    MAOS_VertexColorGreen,
    MAOS_VertexColorBlue,
    MAOS_VertexColorAlpha,
};

/// <summary>
/// Possible sources for mobile emissive color.
/// </summary>
[EnumExtensions]
public enum EMobileEmissiveColorSource
{
    /// <summary>
    /// Emissive texture color.
    /// </summary>
    MECS_EmissiveTexture,

    /// <summary>
    /// Base texture color.
    /// </summary>
    MECS_BaseTexture,

    /// <summary>
    /// Constant color specified in the material properties.
    /// </summary>
    MECS_Constant,
};

/// <summary>
/// Possible sources for mask values and such.
/// </summary>
[EnumExtensions]
public enum EMobileValueSource
{
    MVS_Constant,
    MVS_VertexColorRed,
    MVS_VertexColorGreen,
    MVS_VertexColorBlue,
    MVS_VertexColorAlpha,
    MVS_BaseTextureRed,
    MVS_BaseTextureGreen,
    MVS_BaseTextureBlue,
    MVS_BaseTextureAlpha,
    MVS_MaskTextureRed,
    MVS_MaskTextureGreen,
    MVS_MaskTextureBlue,
    MVS_MaskTextureAlpha,
    MVS_NormalTextureAlpha,
    MVS_EmissiveTextureRed,
    MVS_EmissiveTextureGreen,
    MVS_EmissiveTextureBlue,
    MVS_EmissiveTextureAlpha
};

/// <summary>
/// Possible vertex texture coordinate sets that may used to sample textures on mobile platforms.
/// </summary>
[EnumExtensions]
public enum EMobileColorMultiplySource
{
    MCMS_None,
    MCMS_BaseTextureRed,
    MCMS_BaseTextureGreen,
    MCMS_BaseTextureBlue,
    MCMS_BaseTextureAlpha,
    MCMS_MaskTextureRed,
    MCMS_MaskTextureGreen,
    MCMS_MaskTextureBlue,
    MCMS_MaskTextureAlpha,
};

[EnumExtensions]
public enum EBlendMode
{
    BLEND_Opaque,
    BLEND_Masked,
    BLEND_Translucent,
    BLEND_Additive,
    BLEND_Modulate,
    BLEND_ModulateAndAdd,
    BLEND_SoftMasked,
    BLEND_AlphaComposite,
    BLEND_DitheredTranslucent
};

[EnumExtensions]
public enum EMaterialLightingModel
{
    MLM_Phong,
    MLM_NonDirectional,
    MLM_Unlit,
    MLM_SHPRT,
    MLM_Custom,
    MLM_Anisotropic
};
