using NetEscapades.EnumGenerators;
using UnrealLib.Core;
using UnrealLib.Enums;
using UnrealLib.Experimental.Textures;
using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Materials;

/// <summary>
/// Material interface settings for Lightmass.
/// </summary>
public partial class LightmassMaterialInterfaceSettings : PropertyHolder
{
    #region UProperties

    /// <summary>
    /// If TRUE, forces translucency to cast static shadows as if the material were masked.
    /// </summary>
    [UProperty] public bool bCastShadowAsMasked;
    /// <summary>
    /// Scales the emissive contribution of this material to static lighting.
    /// </summary>
    [UProperty] public float EmissiveBoost = 1.0f;
    /// <summary>
    /// Scales the diffuse contribution of this material to static lighting.
    /// </summary>
    [UProperty] public float DiffuseBoost = 1.0f;
    /// <summary>
    /// Scales the specular contribution of this material to static lighting.
    /// </summary>
    [UProperty] public float SpecularBoost = 1.0f;
    /// <summary>
    /// Scales the resolution that this material's attributes were exported at.
    /// This is useful for increasing material resolution when details are needed.
    /// </summary>
    [UProperty] public float ExportResolutionScale = 1.0f;
    /// <summary>
    /// Scales the penumbra size of distance field shadows. 
    /// This is useful to get softer precomputed shadows on certain material types like foliage.
    /// </summary>
    [UProperty] public float DistanceFieldPenumbraScale = 1.0f;

    // Boolean override flags - only used in MaterialInstance* cases.

    /// <summary>
    /// If TRUE, override the bCastShadowAsMasked setting of the parent material.
    /// </summary>
    [UProperty] public bool bOverrideCastShadowAsMasked;
    /// <summary>
    /// If TRUE, override the emissive boost setting of the parent material.
    /// </summary>
    [UProperty] public bool bOverrideEmissiveBoost;
    /// <summary>
    /// If TRUE, override the diffuse boost setting of the parent material.
    /// </summary>
    [UProperty] public bool bOverrideDiffuseBoost;
    /// <summary>
    /// If TRUE, override the specular boost setting of the parent material.
    /// </summary>
    [UProperty] public bool bOverrideSpecularBoost;
    /// <summary>
    /// If TRUE, override the export resolution scale setting of the parent material.
    /// </summary>
    [UProperty] public bool bOverrideExportResolutionScale;
    /// <summary>
    /// If TRUE, override the distance field penumbra scale setting of the parent material.
    /// </summary>
    [UProperty] public bool bOverrideDistanceFieldPenumbraScale;

    #endregion
};

public partial class UMaterialInterface(FObjectExport? export = null) : USurface(export)
{
    #region Enums

    [EnumExtensions]
    public enum EMaterialUsage
    {
        MATUSAGE_SkeletalMesh,
        MATUSAGE_FracturedMeshes,
        MATUSAGE_ParticleSprites,
        MATUSAGE_BeamTrails,
        MATUSAGE_ParticleSubUV,
        MATUSAGE_SpeedTree,
        MATUSAGE_StaticLighting,
        MATUSAGE_GammaCorrection,
        MATUSAGE_LensFlare,
        MATUSAGE_InstancedMeshParticles,
        MATUSAGE_FluidSurface,
        MATUSAGE_Decals,
        MATUSAGE_MaterialEffect,
        MATUSAGE_MorphTargets,
        MATUSAGE_FogVolumes,
        MATUSAGE_RadialBlur,
        MATUSAGE_InstancedMeshes,
        MATUSAGE_SplineMesh,
        MATUSAGE_ScreenDoorFade,
        MATUSAGE_APEXMesh,
        MATUSAGE_Terrain,
        MATUSAGE_Landscape,
        MATUSAGE_MobileLandscape,
    };

    #endregion

    #region Properties

    /// <summary>
    /// The mesh used by the material editor to preview the material.
    /// </summary>
    /// <remarks>Editor-only.</remarks>
    [UProperty] string PreviewMesh;

    /// <summary>
    /// Unique ID for this material, used for caching during distributed lighting.
    /// </summary>
    /// <remarks>Editor-only for IB3.</remarks>
    [UProperty] private FGuid LightingGuid;

    // [UProperty] public LightmassMaterialInterfaceSettings LightmassSettings = new();

    /// <summary>
    /// Set on materials (and their instances) that have a quality switch expression.
    /// </summary>
    [UProperty] public bool bHasQualitySwitch;

    #region Mobile material properties

    /// <summary>
    /// When enabled, the base texture will be generated automatically by statically 'flattening' the graph network into a texture.
    /// </summary>
    [UProperty] public bool bAutoFlattenMobile;

    /// <summary>
    /// When enabled, the normal texture will be generated automatically by statically 'flattening' the graph network into a texture.
    /// </summary>
    [UProperty] public bool bAutoFlattenMobileNormalTexture;

    /// <summary>
    /// Color to use when flattening a material.
    /// </summary>
    [UProperty] public Color FlattenBackgroundColor = new(0, 0, 0, 255);

    /// <summary>
    /// Base (diffuse) texture, or a texture that was generated by flattening the graph network.
    /// </summary>
    [UProperty] public UTexture MobileBaseTexture;

    /// <summary>
    /// Texture coordinates from mesh vertex to use when sampling base texture on mobile platforms.
    /// </summary>
    [UProperty] public EMobileTexCoordsSource MobileBaseTextureTexCoordsSource = EMobileTexCoordsSource.MTCS_TexCoords0;

    /// <summary>
    /// Normal map texture.  If specified, this enables per pixel lighting when used in combination with other material features.
    /// </summary>
    [UProperty] public UTexture MobileNormalTexture;

    /// <summary>
    /// Enables baked ambient occlusion from mesh vertices and selects which vertex color channel to get the AO data from.
    /// </summary>
    [UProperty] public EMobileAmbientOcclusionSource MobileAmbientOcclusionSource = EMobileAmbientOcclusionSource.MAOS_Disabled;

    /// <summary>
    /// When enabled, primitives using this material may be fogged.  Disable this to improve performance for primitives that don't need fog.
    /// </summary>
    [UProperty] public bool bMobileAllowFog = true;

    #region Generation

    /// <summary>
    /// Whether to generate the flattened texture as a subUV texture.
    /// </summary>
    [UProperty] public bool bGenerateSubUV;
    /// <summary>
    /// The frame rate to capture the subUV images at.
    /// </summary>
    [UProperty] public float SubUVFrameRate;
    /// <summary>
    /// The number of subUV images to capture along each axis of the texture.
    /// Since the generated texture is always going to be square, the same number of sub-images 
    /// will be captured on both the horizontal and vertical axes.
    /// </summary>
    [UProperty] public int SubUVFrameCountAlongAxes;
    /// <summary>
    /// The size of the subUV image to generate.
    /// The generated texture size will be:<br/><br/>
    /// (SubUVFrameCountAlongAxes * SubUVFrameSize, SubUVFrameCountAlongAxes * SubUVFrameSize)<br/><br/>
    /// This value will auto-adjust according to the SubUVFrameCountAlongAxes setting so that the 
    /// proper texture size (power-of-two) results.
    /// </summary>
    [UProperty] public float SubUVFrameSize;

    #endregion

    #region Specular

    /// <summary>
    /// Enables dynamic specular lighting from the single most prominent light source.
    /// </summary>
    [UProperty] public bool bUseMobileSpecular;

    /// <summary>
    /// Enables per-pixel specular for this material (requires normal map).
    /// </summary>
    [UProperty] public bool bUseMobilePixelSpecular;

    /// <summary>
    /// Material specular color.
    /// </summary>
    [UProperty] public LinearColor MobileSpecularColor = new(1.0f);

    /// <summary>
    /// When specular is enabled, this sets the specular power.
    /// Lower values yield a wider highlight, higher values yield a sharper highlight.
    /// </summary>
    [UProperty] public float MobileSpecularPower = 16.0f;

    /// <summary>
    /// Determines how specular values are masked.
    /// <br/>- Constant: Mask is disabled.
    /// <br/>- Luminance: Diffuse RGB luminance used as mask.
    /// <br/>- Diffuse Red/Green/Blue: Use a specific channel of the diffuse texture as the specular mask.
    /// <br/>- Mask Texture RGB: Uses the color from the mask texture.
    /// </summary>
    [UProperty] public EMobileSpecularMask MobileSpecularMask = EMobileSpecularMask.MSM_Constant;

    #endregion

    #region Emissive

    /// <summary>
    /// Emissive texture. If the emissive color source is set to 'Emissive Texture', setting this texture will enable emissive lighting
    /// </summary>
    [UProperty] public UTexture MobileEmissiveTexture;

    /// <summary>
    /// Mobile emissive color source.
    /// </summary>
    [UProperty] public EMobileEmissiveColorSource MobileEmissiveColorSource = EMobileEmissiveColorSource.MECS_EmissiveTexture;

    /// <summary>
    /// Mobile emissive color. If MobileEmissiveColorSource is set to 'Constant' this acts as the emissive color,
    /// if not it blends with the other input to form the final color.
    /// </summary>
    [UProperty] public LinearColor MobileEmissiveColor = new(1.0f);

    /// <summary>
    /// Selects a source for emissive light masking.
    /// </summary>
    [UProperty] public EMobileValueSource MobileEmissiveMaskSource = EMobileValueSource.MVS_Constant;

    #endregion

    #region Environment

    /// <summary>
    /// Spherical environment map texture. When specified, spherical environment mapping will be enabled for this material.
    /// </summary>
    [UProperty] public UTexture MobileEnvironmentTexture;

    /// <summary>
    /// Selects a source for environment map amount.
    /// </summary>
    [UProperty] public EMobileValueSource MobileEnvironmentMaskSource = EMobileValueSource.MVS_Constant;

    /// <summary>
    /// Sets how much the environment map texture contributes to the final color.
    /// </summary> 
    [UProperty] public float MobileEnvironmentAmount = 1.0f;

    /// <summary>
    /// When environment mapping is enabled, this sets how the environment color is blended with the base color.
    /// </summary>
    [UProperty] public EMobileEnvironmentBlendMode MobileEnvironmentBlendMode = EMobileEnvironmentBlendMode.MEBM_Add;

    /// <summary>
    /// Environment map color scale.
    /// </summary>
    [UProperty] public LinearColor MobileEnvironmentColor = new(1.0f);

    /// <summary>
    /// Environment mapping fresnel amount. Set this to zero for best performance.
    /// </summary>
    [UProperty] public float MobileEnvironmentFresnelAmount = 0.0f;

    /// <summary>
    /// Environment mapping fresnel exponent. Set this to 1.0 for best performance.
    /// </summary>
    /// <remarks>Min 0.01, Max 8.0</remarks>
    [UProperty] public float MobileEnvironmentFresnelExponent = 1.0f;

    #endregion

    #region Rim Lighting

    /// <summary>
    /// When set to anything other than zero, enables rim lighting for this material and sets the amount of rim lighting to apply.
    /// </summary>
    /// <remarks>Min 0.0, Max 4.0</remarks>
    [UProperty] public float MobileRimLightingStrength = 0.0f;

    /// <summary>
    /// Sets the exponent used for rim lighting.
    /// </summary>
    /// <remarks>Min 0.01, Max 8.0</remarks>
    [UProperty] public float MobileRimLightingExponent = 2.0f;

    /// <summary>
    /// Selects a source for rim light masking.
    /// </summary>
    [UProperty] public EMobileValueSource MobileRimLightingMaskSource = EMobileValueSource.MVS_Constant;

    /// <summary>
    /// Rim light color.
    /// </summary>
    [UProperty] public LinearColor MobileRimLightingColor = new(1.0f);

    #endregion

    #region Bump Offset

    /// <summary>
    /// Enables a bump offset effect for this material. A mask texture must be supplied. The bump offset amount is stored in the mask texture's RED channel.
    /// </summary>
    [UProperty] public bool bUseMobileBumpOffset;

    /// <summary>
    /// Bump offset reference plane.
    /// </summary>
    [UProperty] public float MobileBumpOffsetReferencePlane = 0.5f;

    /// <summary>
    /// Bump height ratio.
    /// </summary>
    [UProperty] public float MobileBumpOffsetHeightRatio = 0.05f;

    #endregion

    #region Masking

    /// <summary>
    /// General purpose mask texture used for bump offset amount, texture blending, etc.
    /// </summary>
    [UProperty] public UTexture MobileMaskTexture;

    /// <summary>
    /// Texture coordinates from mesh vertex to use when sampling mask texture.
    /// </summary>
    [UProperty] public EMobileTexCoordsSource MobileMaskTextureTexCoordsSource = EMobileTexCoordsSource.MTCS_TexCoords0;

    /// <summary>
    /// Enables the override of base texture alpha with the red channel of the mask texture to support platforms that don't have alpha texture compression.
    /// </summary>
    [UProperty] public EMobileAlphaValueSource MobileAlphaValueSource = EMobileAlphaValueSource.MAVS_DiffuseTextureAlpha;

    /// <summary>
    /// Acts as a multiplier for the final opacity value.
    /// </summary>
    [UProperty] public float MobileOpacityMultiplier = 1.0f;

    #endregion

    #region Texture blending

    /// <summary>
    /// Detail texture to use for blending the base texture (red channel or mask texture alpha based on MobileTextureBlendFactorSource).
    /// </summary>
    [UProperty] public UTexture MobileDetailTexture;
    /// <summary>
    /// Detail texture to use for blending the base texture (green of vertex color).
    /// </summary>
    [UProperty] public UTexture MobileDetailTexture2;
    /// <summary>
    /// Detail texture to use for blending the base texture (blue of vertex color).
    /// </summary>
    [UProperty] public UTexture MobileDetailTexture3;

    /// <summary>
    /// Texture coordinates from mesh vertex to use when sampling detail texture.
    /// </summary>
    [UProperty] public EMobileTexCoordsSource MobileDetailTextureTexCoordsSource = EMobileTexCoordsSource.MTCS_TexCoords1;

    /// <summary>
    /// Where the blend factor comes from, for blending the base texture with the detail texture.
    /// </summary>
    [UProperty] public EMobileTextureBlendFactorSource MobileTextureBlendFactorSource = EMobileTextureBlendFactorSource.MTBFS_VertexColor;

    /// <summary>
    /// Locks use of the detail texture and does not allow it to be forced off by system settings.
    /// </summary>
    [UProperty] public bool bLockColorBlending;

    #endregion

    #region Color blending

    /// <summary>
    /// Whether to use uniform color scaling (mesh particles) or not.
    /// </summary>
    [UProperty] public bool bUseMobileUniformColorMultiply;

    /// <summary>
    /// Default color to modulate each vertex by.
    /// </summary>
    [UProperty] public LinearColor MobileDefaultUniformColor;

    /// <summary>
    /// Whether to use per vertex color scaling.
    /// </summary>
    [UProperty] public bool bUseMobileVertexColorMultiply;

    /// <summary>
    /// Whether to use detail normal for mobile.
    /// </summary>
    [UProperty] public bool bUseMobileDetailNormal;

    /// <summary>
    /// Enables the user to specify a channel of a texture to use for the Color multiplication.
    /// </summary>
    [UProperty] public EMobileColorMultiplySource MobileColorMultiplySource = EMobileColorMultiplySource.MCMS_None;

    #endregion

    #region Texture transform

    // Which texture UVs to transform
    [UProperty] public bool bBaseTextureTransformed;
    [UProperty] public bool bEmissiveTextureTransformed;
    [UProperty] public bool bNormalTextureTransformed;
    [UProperty] public bool bMaskTextureTransformed;
    [UProperty] public bool bDetailTextureTransformed;

    /// <summary>
    /// Horizontal center for texture rotation/scale.
    /// </summary>
    [UProperty] public float MobileTransformCenterX = 0.5f;

    /// <summary>
    /// Vertical center for texture rotation/scale.
    /// </summary>
    [UProperty] public float MobileTransformCenterY;

    /// <summary>
    /// Horizontal speed for texture panning.
    /// </summary>
    [UProperty] public float MobilePannerSpeedX = 0.0f;

    /// <summary>
    /// Vertical speed for texture panning.
    /// </summary>
    [UProperty] public float MobilePannerSpeedY = 0.0f;

    /// <summary>
    /// Texture rotation speed in radians per second.
    /// </summary>
    [UProperty] public float MobileRotateSpeed = 0.0f;

    /// <summary>
    /// Fixed horizontal texture scale (around the rotation center).
    /// </summary>
    [UProperty] public float MobileFixedScaleX = 1.0f;

    /// <summary>
    /// Fixed vertical texture scale (around the rotation center).
    /// </summary>
    [UProperty] public float MobileFixedScaleY;

    /// <summary>
    /// Horizontal texture scale applied to a sine wave.
    /// </summary>
    [UProperty] public float MobileSineScaleX = 0.0f;

    /// <summary>
    /// Vertical texture scale applied to a sine wave.
    /// </summary>
    [UProperty] public float MobileSineScaleY = 0.0f;

    /// <summary>
    /// Multiplier for sine wave texture scaling frequency.
    /// </summary>
    [UProperty] public float MobileSineScaleFrequencyMultipler = 0.0f;

    /// <summary>
    /// Fixed offset for texture.
    /// </summary>
    [UProperty] public float MobileFixedOffsetX = 0.0f;

    /// <summary>
    /// Fixed offset for texture.
    /// </summary>
    [UProperty] public float MobileFixedOffsetY = 0.0f;

    #endregion

    #region Vertex animation

    /// <summary>
    /// Enables per-vertex movement on a wave (for use with trees and similar objects).
    /// </summary>
    [UProperty] public bool bUseMobileWaveVertexMovement;

    /// <summary>
    /// Frequency adjustment for wave on vertex positions.
    /// </summary>
    [UProperty] public float MobileTangentVertexFrequencyMultiplier = 0.125f;

    /// <summary>
    /// Frequency adjustment for wave on vertex positions.
    /// </summary>
    [UProperty] public float MobileVerticalFrequencyMultiplier = 0.1f;

    /// <summary>
    /// Amplitude of adjustments for wave on vertex positions.
    /// </summary>
    [UProperty] public float MobileMaxVertexMovementAmplitude = 5.0f;

    /// <summary>
    /// Frequency of entire object sway.
    /// </summary>
    [UProperty] public float MobileSwayFrequencyMultiplier = 0.07f;

    /// <summary>
    /// Frequency of entire object sway.
    /// </summary>
    [UProperty] public float MobileSwayMaxAngle = 2.0f;

    #endregion

    #region Flatten

    /// <summary>
    /// The direction of the directional light for flattening the mobile material.
    /// </summary>
    [UProperty] public Vector MobileDirectionalLightDirection = new(0.0f, -45.0f, 45.0f);
    /// <summary>
    /// The brightness of the directional light for flattening the mobile material.
    /// </summary>
    [UProperty] public float MobileDirectionalLightBrightness = -2.0f;
    /// <summary>
    /// The color of the directional light for flattening the mobile material.
    /// </summary>
    [UProperty] public Color MobileDirectionalLightColor = new(255);
    /// <summary>
    /// If TRUE, use a second directional light to simulate light bouncing when flattening the mobile material.
    /// </summary>
    [UProperty] public bool bMobileEnableBounceLight;
    /// <summary>
    /// The direction of the simulated bounce directional light for flattening the mobile material.
    /// </summary>
    [UProperty] public Vector MobileBounceLightDirection = new(0.0f, -45.0f, -27.5f);
    /// <summary>
    /// The brightness of the simulated bounce directional light for flattening the mobile material.
    /// </summary>
    [UProperty] public float MobileBounceLightBrightness = 0.25f;
    /// <summary>
    /// The color of the simulated bounce directional light for flattening the mobile material.
    /// </summary>
    [UProperty] public Color MobileBounceLightColor = new(255);
    /// <summary>
    /// The brightness of the skylight for flattening the mobile material.
    /// </summary>
    [UProperty] public float MobileSkyLightBrightness = 0.25f;
    /// <summary>
    /// The color of the skylight for flattening the mobile material.
    /// </summary>
    [UProperty] public Color MobileSkyLightColor = new(255);

    #endregion

    #region Landscape

    /// <summary>
    /// Whether to use monochrome layer Blending or regular Layer Blending.
    /// </summary>
    [UProperty] public bool bUseMobileLandscapeMonochromeLayerBlending;
    /// <summary>
    /// The names of the 4 Landscape layers supported on mobile.
    /// </summary>
    [UProperty] public FName[] MobileLandscapeLayerNames = new FName[4];
    /// <summary>
    /// The RBG colors to colorize each monochome layer when using monochrome layer blending.
    /// </summary>
    [UProperty] public Color[] MobileLandscapeMonochomeLayerColors = [new(255), new(255), new(255), new(255)];

    #endregion

    #endregion

    #endregion
}
