using System;
using UnrealLib.Core;
using UnrealLib.Enums;
using UnrealLib.Experimental.Textures;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.Materials;

public class UMaterialInterface(FObjectExport export) : USurface(export)
{
    #region Enums

    enum EMaterialUsage : byte
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

    #region Structs

    /// <summary>
    /// Material interface settings for Lightmass.
    /// </summary>
    public class LightmassMaterialInterfaceSettings : PropertyHolder
    {
        #region Properties

        /// <summary>
        /// If TRUE, forces translucency to cast static shadows as if the material were masked.
        /// </summary>
        public bool bCastShadowAsMasked;
        /// <summary>
        /// Scales the emissive contribution of this material to static lighting.
        /// </summary>
        public float EmissiveBoost = 1.0f;
        /// <summary>
        /// Scales the diffuse contribution of this material to static lighting.
        /// </summary>
        public float DiffuseBoost = 1.0f;
        /// <summary>
        /// Scales the specular contribution of this material to static lighting.
        /// </summary>
        public float SpecularBoost = 1.0f;
        /// <summary>
        /// Scales the resolution that this material's attributes were exported at.
        /// This is useful for increasing material resolution when details are needed.
        /// </summary>
        public float ExportResolutionScale = 1.0f;
        /// <summary>
        /// Scales the penumbra size of distance field shadows. 
        /// This is useful to get softer precomputed shadows on certain material types like foliage.
        /// </summary>
        public float DistanceFieldPenumbraScale = 1.0f;

        // Boolean override flags - only used in MaterialInstance* cases.

        /// <summary>
        /// If TRUE, override the bCastShadowAsMasked setting of the parent material.
        /// </summary>
        public bool bOverrideCastShadowAsMasked;
        /// <summary>
        /// If TRUE, override the emissive boost setting of the parent material.
        /// </summary>
        public bool bOverrideEmissiveBoost;
        /// <summary>
        /// If TRUE, override the diffuse boost setting of the parent material.
        /// </summary>
        public bool bOverrideDiffuseBoost;
        /// <summary>
        /// If TRUE, override the specular boost setting of the parent material.
        /// </summary>
        public bool bOverrideSpecularBoost;
        /// <summary>
        /// If TRUE, override the export resolution scale setting of the parent material.
        /// </summary>
        public bool bOverrideExportResolutionScale;
        /// <summary>
        /// If TRUE, override the distance field penumbra scale setting of the parent material.
        /// </summary>
        public bool bOverrideDistanceFieldPenumbraScale;

        #endregion

        internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
        {
            switch (tag.Name.GetString)
            {
                case nameof(bCastShadowAsMasked): Ar.Serialize(ref bCastShadowAsMasked); break;
                case nameof(EmissiveBoost): Ar.Serialize(ref EmissiveBoost); break;
                case nameof(DiffuseBoost): Ar.Serialize(ref DiffuseBoost); break;
                case nameof(SpecularBoost): Ar.Serialize(ref SpecularBoost); break;
                case nameof(ExportResolutionScale): Ar.Serialize(ref ExportResolutionScale); break;
                case nameof(DistanceFieldPenumbraScale): Ar.Serialize(ref DistanceFieldPenumbraScale); break;
                case nameof(bOverrideCastShadowAsMasked): Ar.Serialize(ref bOverrideCastShadowAsMasked); break;
                case nameof(bOverrideEmissiveBoost): Ar.Serialize(ref bOverrideEmissiveBoost); break;
                case nameof(bOverrideDiffuseBoost): Ar.Serialize(ref bOverrideDiffuseBoost); break;
                case nameof(bOverrideSpecularBoost): Ar.Serialize(ref bOverrideSpecularBoost); break;
                case nameof(bOverrideExportResolutionScale): Ar.Serialize(ref bOverrideExportResolutionScale); break;
                case nameof(bOverrideDistanceFieldPenumbraScale): Ar.Serialize(ref bOverrideDistanceFieldPenumbraScale); break;
                default: base.ParseProperty(Ar, tag); break;
            }
        }
    };

    #endregion

    #region Properties

    /// <summary>
    /// The mesh used by the material editor to preview the material.
    /// </summary>
    /// <remarks>Editor-only.</remarks>
    string PreviewMesh;

    /// <summary>
    /// Unique ID for this material, used for caching during distributed lighting.
    /// </summary>
    /// <remarks>Editor-only for IB3.</remarks>
    private Guid LightingGuid;

    public LightmassMaterialInterfaceSettings LightmassSettings = new();

    /// <summary>
    /// Set on materials (and their instances) that have a quality switch expression.
    /// </summary>
    public bool bHasQualitySwitch;

    #region Mobile material properties

    /// <summary>
    /// When enabled, the base texture will be generated automatically by statically 'flattening' the graph network into a texture.
    /// </summary>
    public bool bAutoFlattenMobile;

    /// <summary>
    /// When enabled, the normal texture will be generated automatically by statically 'flattening' the graph network into a texture.
    /// </summary>
    public bool bAutoFlattenMobileNormalTexture;

    /// <summary>
    /// Color to use when flattening a material.
    /// </summary>
    public Color FlattenBackgroundColor = new(0, 0, 0, 255);

    /// <summary>
    /// Base (diffuse) texture, or a texture that was generated by flattening the graph network.
    /// </summary>
    public UTexture MobileBaseTexture;

    /// <summary>
    /// Texture coordinates from mesh vertex to use when sampling base texture on mobile platforms.
    /// </summary>
    public EMobileTexCoordsSource MobileBaseTextureTexCoordsSource = EMobileTexCoordsSource.MTCS_TexCoords0;

    /// <summary>
    /// Normal map texture.  If specified, this enables per pixel lighting when used in combination with other material features.
    /// </summary>
    public UTexture MobileNormalTexture;

    /// <summary>
    /// Enables baked ambient occlusion from mesh vertices and selects which vertex color channel to get the AO data from.
    /// </summary>
    public EMobileAmbientOcclusionSource MobileAmbientOcclusionSource = EMobileAmbientOcclusionSource.MAOS_Disabled;

    /// <summary>
    /// When enabled, primitives using this material may be fogged.  Disable this to improve performance for primitives that don't need fog.
    /// </summary>
    public bool bMobileAllowFog = true;

    #region Generation

    /// <summary>
    /// Whether to generate the flattened texture as a subUV texture.
    /// </summary>
    public bool bGenerateSubUV;
    /// <summary>
    /// The frame rate to capture the subUV images at.
    /// </summary>
    public float SubUVFrameRate;
    /// <summary>
    /// The number of subUV images to capture along each axis of the texture.
    /// Since the generated texture is always going to be square, the same number of sub-images 
    /// will be captured on both the horizontal and vertical axes.
    /// </summary>
    public int SubUVFrameCountAlongAxes;
    /// <summary>
    /// The size of the subUV image to generate.
    /// The generated texture size will be:<br/><br/>
    /// (SubUVFrameCountAlongAxes * SubUVFrameSize, SubUVFrameCountAlongAxes * SubUVFrameSize)<br/><br/>
    /// This value will auto-adjust according to the SubUVFrameCountAlongAxes setting so that the 
    /// proper texture size (power-of-two) results.
    /// </summary>
    public float SubUVFrameSize;

    #endregion

    #region Specular

    /// <summary>
    /// Enables dynamic specular lighting from the single most prominent light source.
    /// </summary>
    public bool bUseMobileSpecular;

    /// <summary>
    /// Enables per-pixel specular for this material (requires normal map).
    /// </summary>
    public bool bUseMobilePixelSpecular;

    /// <summary>
    /// Material specular color.
    /// </summary>
    public LinearColor MobileSpecularColor = new(1.0f);

    /// <summary>
    /// When specular is enabled, this sets the specular power.
    /// Lower values yield a wider highlight, higher values yield a sharper highlight.
    /// </summary>
    public float MobileSpecularPower = 16.0f;

    /// <summary>
    /// Determines how specular values are masked.
    /// <br/>- Constant: Mask is disabled.
    /// <br/>- Luminance: Diffuse RGB luminance used as mask.
    /// <br/>- Diffuse Red/Green/Blue: Use a specific channel of the diffuse texture as the specular mask.
    /// <br/>- Mask Texture RGB: Uses the color from the mask texture.
    /// </summary>
    public EMobileSpecularMask MobileSpecularMask = EMobileSpecularMask.MSM_Constant;

    #endregion

    #region Emissive

    /// <summary>
    /// Emissive texture. If the emissive color source is set to 'Emissive Texture', setting this texture will enable emissive lighting
    /// </summary>
    public UTexture MobileEmissiveTexture;

    /// <summary>
    /// Mobile emissive color source.
    /// </summary>
    public EMobileEmissiveColorSource MobileEmissiveColorSource = EMobileEmissiveColorSource.MECS_EmissiveTexture;

    /// <summary>
    /// Mobile emissive color. If MobileEmissiveColorSource is set to 'Constant' this acts as the emissive color,
    /// if not it blends with the other input to form the final color.
    /// </summary>
    public LinearColor MobileEmissiveColor = new(1.0f);

    /// <summary>
    /// Selects a source for emissive light masking.
    /// </summary>
    public EMobileValueSource MobileEmissiveMaskSource = EMobileValueSource.MVS_Constant;

    #endregion

    #region Environment

    /// <summary>
    /// Spherical environment map texture. When specified, spherical environment mapping will be enabled for this material.
    /// </summary>
    public UTexture MobileEnvironmentTexture;

    /// <summary>
    /// Selects a source for environment map amount.
    /// </summary>
    public EMobileValueSource MobileEnvironmentMaskSource = EMobileValueSource.MVS_Constant;

    /// <summary>
    /// Sets how much the environment map texture contributes to the final color.
    /// </summary> 
    public float MobileEnvironmentAmount = 1.0f;

    /// <summary>
    /// When environment mapping is enabled, this sets how the environment color is blended with the base color.
    /// </summary>
    public EMobileEnvironmentBlendMode MobileEnvironmentBlendMode = EMobileEnvironmentBlendMode.MEBM_Add;

    /// <summary>
    /// Environment map color scale.
    /// </summary>
    public LinearColor MobileEnvironmentColor = new(1.0f);

    /// <summary>
    /// Environment mapping fresnel amount. Set this to zero for best performance.
    /// </summary>
    public float MobileEnvironmentFresnelAmount = 0.0f;

    /// <summary>
    /// Environment mapping fresnel exponent. Set this to 1.0 for best performance.
    /// </summary>
    /// <remarks>Min 0.01, Max 8.0</remarks>
    public float MobileEnvironmentFresnelExponent = 1.0f;

    #endregion

    #region Rim Lighting

    /// <summary>
    /// When set to anything other than zero, enables rim lighting for this material and sets the amount of rim lighting to apply.
    /// </summary>
    /// <remarks>Min 0.0, Max 4.0</remarks>
    public float MobileRimLightingStrength = 0.0f;

    /// <summary>
    /// Sets the exponent used for rim lighting.
    /// </summary>
    /// <remarks>Min 0.01, Max 8.0</remarks>
    public float MobileRimLightingExponent = 2.0f;

    /// <summary>
    /// Selects a source for rim light masking.
    /// </summary>
    public EMobileValueSource MobileRimLightingMaskSource = EMobileValueSource.MVS_Constant;

    /// <summary>
    /// Rim light color.
    /// </summary>
    public LinearColor MobileRimLightingColor = new(1.0f);

    #endregion

    #region Bump Offset

    /// <summary>
    /// Enables a bump offset effect for this material. A mask texture must be supplied. The bump offset amount is stored in the mask texture's RED channel.
    /// </summary>
    public bool bUseMobileBumpOffset;

    /// <summary>
    /// Bump offset reference plane.
    /// </summary>
    public float MobileBumpOffsetReferencePlane = 0.5f;

    /// <summary>
    /// Bump height ratio.
    /// </summary>
    public float MobileBumpOffsetHeightRatio = 0.05f;

    #endregion

    #region Masking

    /// <summary>
    /// General purpose mask texture used for bump offset amount, texture blending, etc.
    /// </summary>
    public UTexture MobileMaskTexture;

    /// <summary>
    /// Texture coordinates from mesh vertex to use when sampling mask texture.
    /// </summary>
    public EMobileTexCoordsSource MobileMaskTextureTexCoordsSource = EMobileTexCoordsSource.MTCS_TexCoords0;

    /// <summary>
    /// Enables the override of base texture alpha with the red channel of the mask texture to support platforms that don't have alpha texture compression.
    /// </summary>
    public EMobileAlphaValueSource MobileAlphaValueSource = EMobileAlphaValueSource.MAVS_DiffuseTextureAlpha;

    /// <summary>
    /// Acts as a multiplier for the final opacity value.
    /// </summary>
    public float MobileOpacityMultiplier = 1.0f;

    #endregion

    #region Texture blending

    /// <summary>
    /// Detail texture to use for blending the base texture (red channel or mask texture alpha based on MobileTextureBlendFactorSource).
    /// </summary>
    public UTexture MobileDetailTexture;
    /// <summary>
    /// Detail texture to use for blending the base texture (green of vertex color).
    /// </summary>
    public UTexture MobileDetailTexture2;
    /// <summary>
    /// Detail texture to use for blending the base texture (blue of vertex color).
    /// </summary>
    public UTexture MobileDetailTexture3;

    /// <summary>
    /// Texture coordinates from mesh vertex to use when sampling detail texture.
    /// </summary>
    public EMobileTexCoordsSource MobileDetailTextureTexCoordsSource = EMobileTexCoordsSource.MTCS_TexCoords1;

    /// <summary>
    /// Where the blend factor comes from, for blending the base texture with the detail texture.
    /// </summary>
    public EMobileTextureBlendFactorSource MobileTextureBlendFactorSource = EMobileTextureBlendFactorSource.MTBFS_VertexColor;

    /// <summary>
    /// Locks use of the detail texture and does not allow it to be forced off by system settings.
    /// </summary>
    public bool bLockColorBlending;

    #endregion

    #region Color blending

    /// <summary>
    /// Whether to use uniform color scaling (mesh particles) or not.
    /// </summary>
    public bool bUseMobileUniformColorMultiply;

    /// <summary>
    /// Default color to modulate each vertex by.
    /// </summary>
    public LinearColor MobileDefaultUniformColor;

    /// <summary>
    /// Whether to use per vertex color scaling.
    /// </summary>
    public bool bUseMobileVertexColorMultiply;

    /// <summary>
    /// Whether to use detail normal for mobile.
    /// </summary>
    public bool bUseMobileDetailNormal;

    /// <summary>
    /// Enables the user to specify a channel of a texture to use for the Color multiplication.
    /// </summary>
    public EMobileColorMultiplySource MobileColorMultiplySource = EMobileColorMultiplySource.MCMS_None;

    #endregion

    #region Texture transform

    // Which texture UVs to transform
    public bool bBaseTextureTransformed;
    public bool bEmissiveTextureTransformed;
    public bool bNormalTextureTransformed;
    public bool bMaskTextureTransformed;
    public bool bDetailTextureTransformed;

    /// <summary>
    /// Horizontal center for texture rotation/scale.
    /// </summary>
    public float MobileTransformCenterX = 0.5f;

    /// <summary>
    /// Vertical center for texture rotation/scale.
    /// </summary>
    public float MobileTransformCenterY;

    /// <summary>
    /// Horizontal speed for texture panning.
    /// </summary>
    public float MobilePannerSpeedX = 0.0f;

    /// <summary>
    /// Vertical speed for texture panning.
    /// </summary>
    public float MobilePannerSpeedY = 0.0f;

    /// <summary>
    /// Texture rotation speed in radians per second.
    /// </summary>
    public float MobileRotateSpeed = 0.0f;

    /// <summary>
    /// Fixed horizontal texture scale (around the rotation center).
    /// </summary>
    public float MobileFixedScaleX = 1.0f;

    /// <summary>
    /// Fixed vertical texture scale (around the rotation center).
    /// </summary>
    public float MobileFixedScaleY;

    /// <summary>
    /// Horizontal texture scale applied to a sine wave.
    /// </summary>
    public float MobileSineScaleX = 0.0f;

    /// <summary>
    /// Vertical texture scale applied to a sine wave.
    /// </summary>
    public float MobileSineScaleY = 0.0f;

    /// <summary>
    /// Multiplier for sine wave texture scaling frequency.
    /// </summary>
    public float MobileSineScaleFrequencyMultipler = 0.0f;

    /// <summary>
    /// Fixed offset for texture.
    /// </summary>
    public float MobileFixedOffsetX = 0.0f;

    /// <summary>
    /// Fixed offset for texture.
    /// </summary>
    public float MobileFixedOffsetY = 0.0f;

    #endregion

    #region Vertex animation

    /// <summary>
    /// Enables per-vertex movement on a wave (for use with trees and similar objects).
    /// </summary>
    public bool bUseMobileWaveVertexMovement;

    /// <summary>
    /// Frequency adjustment for wave on vertex positions.
    /// </summary>
    public float MobileTangentVertexFrequencyMultiplier = 0.125f;

    /// <summary>
    /// Frequency adjustment for wave on vertex positions.
    /// </summary>
    public float MobileVerticalFrequencyMultiplier = 0.1f;

    /// <summary>
    /// Amplitude of adjustments for wave on vertex positions.
    /// </summary>
    public float MobileMaxVertexMovementAmplitude = 5.0f;

    /// <summary>
    /// Frequency of entire object sway.
    /// </summary>
    public float MobileSwayFrequencyMultiplier = 0.07f;

    /// <summary>
    /// Frequency of entire object sway.
    /// </summary>
    public float MobileSwayMaxAngle = 2.0f;

    #endregion

    #region Flatten

    /// <summary>
    /// The direction of the directional light for flattening the mobile material.
    /// </summary>
    public Vector3 MobileDirectionalLightDirection = new(0.0f, -45.0f, 45.0f);
    /// <summary>
    /// The brightness of the directional light for flattening the mobile material.
    /// </summary>
    public float MobileDirectionalLightBrightness = -2.0f;
    /// <summary>
    /// The color of the directional light for flattening the mobile material.
    /// </summary>
    public Color MobileDirectionalLightColor = new(255);
    /// <summary>
    /// If TRUE, use a second directional light to simulate light bouncing when flattening the mobile material.
    /// </summary>
    public bool bMobileEnableBounceLight;
    /// <summary>
    /// The direction of the simulated bounce directional light for flattening the mobile material.
    /// </summary>
    public Vector3 MobileBounceLightDirection = new(0.0f, -45.0f, -27.5f);
    /// <summary>
    /// The brightness of the simulated bounce directional light for flattening the mobile material.
    /// </summary>
    public float MobileBounceLightBrightness = 0.25f;
    /// <summary>
    /// The color of the simulated bounce directional light for flattening the mobile material.
    /// </summary>
    public Color MobileBounceLightColor = new(255);
    /// <summary>
    /// The brightness of the skylight for flattening the mobile material.
    /// </summary>
    public float MobileSkyLightBrightness = 0.25f;
    /// <summary>
    /// The color of the skylight for flattening the mobile material.
    /// </summary>
    public Color MobileSkyLightColor = new(255);

    #endregion

    #region Landscape

    /// <summary>
    /// Whether to use monochrome layer Blending or regular Layer Blending.
    /// </summary>
    public bool bUseMobileLandscapeMonochromeLayerBlending;
    /// <summary>
    /// The names of the 4 Landscape layers supported on mobile.
    /// </summary>
    public readonly FName[] MobileLandscapeLayerNames = new FName[4];
    /// <summary>
    /// The RBG colors to colorize each monochome layer when using monochrome layer blending.
    /// </summary>
    public readonly Color[] MobileLandscapeMonochomeLayerColors = [new(255), new(255), new(255), new(255)];

    #endregion

    #endregion

    #endregion
}
