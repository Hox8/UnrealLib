using UnrealLib.Core;
using UnrealLib.Enums;
using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Materials;

public partial class UMaterial(FObjectExport? export = null) : UMaterialInterface(export)
{
    #region UProperties

    #region Physics

    ///** Physical material to use for this graphics material. Used for sounds, effects etc.*/
    //public PhysicalMaterial PhysMaterial;

    ///** For backwards compatibility only. */
    //public PhysicalMaterial PhysicalMaterial;

    ///** A 1 bit monochrome texture that represents a mask for what physical material should be used if the collided texel is black or white. */
    //public UTexture2D PhysMaterialMask;
    ///** The UV channel to use for the PhysMaterialMask. */
    //public int PhysMaterialMaskUVChannel;
    ///** The physical material to use when a black pixel in the PhysMaterialMask texture is hit. */
    //public PhysicalMaterial BlackPhysicalMaterial;
    ///** The physical material to use when a white pixel in the PhysMaterialMask texture is hit. */
    //public PhysicalMaterial WhitePhysicalMaterial;

    #endregion

    #region Reflection

    [UProperty] public ColorMaterialInput DiffuseColor;
    [UProperty] public ScalarMaterialInput DiffusePower;
    [UProperty] public ColorMaterialInput SpecularColor;
    [UProperty] public ScalarMaterialInput SpecularPower;
    [UProperty] public VectorMaterialInput Normal;

    #endregion

    #region Emission

    [UProperty] public ColorMaterialInput EmissiveColor;

    #endregion

    [UProperty] public ScalarMaterialInput Opacity;
    [UProperty] public ScalarMaterialInput OpacityMask;

    /** If BlendMode is BLEND_Masked or BLEND_SoftMasked, the surface is not rendered where OpacityMask < OpacityMaskClipValue. */
    [UProperty] public float OpacityMaskClipValue;

    /** Can be used to bias shadows away from the surface. */
    [UProperty] public float ShadowDepthBias;

    /** Allows the material to distort background color by offsetting each background pixel by the amount of the distortion input for that pixel. */
    [UProperty] public Vector2MaterialInput Distortion;

    /** Determines how the material's color is blended with background colors. */
    [UProperty] public EBlendMode BlendMode;

    /** Determines how inputs are combined to create the material's final color. */
    [UProperty] public EMaterialLightingModel LightingModel;

    /** 
     * Use a custom light transfer equation to be factored with light color, attenuation and shadowing. 
     * This is currently only used for Movable, Toggleable and Dominant light contribution.
     * LightVector can be used in this material input and will be set to the tangent space light direction of the current light being rendered.
     */
    [UProperty] public ColorMaterialInput CustomLighting;

    /** 
     * Use a custom diffuse factor for attenuation with lights that only support a diffuse term. 
     * This should only be the diffuse color coefficient, and must not depend on LightVector.
     * This is currently used with skylights, SH lights, materials exported to lightmass and directional lightmap contribution.
     */
    [UProperty] public ColorMaterialInput CustomSkylightDiffuse;

    /** Specify a vector to use as anisotropic direction */
    [UProperty] public VectorMaterialInput AnisotropicDirection;

    /** Lerps between lighting color (diffuse * attenuation * Lambertian) and lighting without the Lambertian term color (diffuse * attenuation * TwoSidedLightingColor). */
    [UProperty] public ScalarMaterialInput TwoSidedLightingMask;

    /** Modulates the lighting without the Lambertian term in two sided lighting. */
    [UProperty] public ColorMaterialInput TwoSidedLightingColor;

    /** Adds to world position in the vertex shader. */
    [UProperty] public VectorMaterialInput WorldPositionOffset;

    /** Offset in tangent space applied to tessellated vertices.  A scalar connected to this input will be treated as the z component (float3(0,0,x)). */
    [UProperty] public VectorMaterialInput WorldDisplacement;

    /** Multiplies the tessellation factors applied when a tessellation mode is set. */
    [UProperty] public ScalarMaterialInput TessellationMultiplier;

    /** Modulates the local contribution to the subsurface scattered lighting of the material. */
    [UProperty] public ColorMaterialInput SubsurfaceInscatteringColor;

    /** Light from subsurface scattering is attenuated by SubsurfaceAbsorptionColor^Distance. */
    [UProperty] public ColorMaterialInput SubsurfaceAbsorptionColor;

    /** The maximum distance light from subsurface scattering will travel. */
    [UProperty] public ScalarMaterialInput SubsurfaceScatteringRadius;

    /** Indicates that the material should be rendered in the SeparateTranslucency Pass (does not affect bloom, not affected by DOF, requires bAllowSeparateTranslucency to be set in .ini). */
    [UProperty] public bool EnableSeparateTranslucency;

    /** Indicates that the material should be rendered without backface culling and the normal should be flipped for backfaces. */
    [UProperty] public bool TwoSided;

    /** Indicates that the material should be rendered in its own pass. Used for hair renderering */
    [UProperty] public bool TwoSidedSeparatePass;

    /**
     * Allows the material to disable depth tests, which is only meaningful with translucent blend modes.
     * Disabling depth tests will make rendering significantly slower since no occluded pixels can get zculled.
     */
    [UProperty] public bool bDisableDepthTest;

    /** 
     * If enabled and this material reads from scene texture, this material will be rendered behind all other translucency, 
     * Instead of the default behavior for materials that read from scene texture, which is for them to render in front of all other translucency in the same DPG.
     * This is useful for placing large spheres around a level that read from scene texture to do chromatic aberration.
     */
    [UProperty] public bool bSceneTextureRenderBehindTranslucency;

    /** Whether the material should allow fog or be unaffected by fog.  This only has meaning for materials with translucent blend modes. */
    [UProperty] public bool bAllowFog;

    /** 
     * Whether the material should receive dynamic dominant light shadows from static objects when the material is being lit by a light environment. 
     * This is useful for character hair.
     */
    [UProperty] public bool bTranslucencyReceiveDominantShadowsFromStatic;

    /** 
     * Whether the material should inherit the dynamic shadows that dominant lights are casting on opaque and masked materials behind this material.
     * This is useful for ground meshes using a translucent blend mode and depth biased alpha to hide seams.
     */
    [UProperty] public bool bTranslucencyInheritDominantShadowsFromOpaque;

    /** Whether the material should allow Depth of Field or be unaffected by DoF.  This only has meaning for materials with translucent blend modes. */
    [UProperty] public bool bAllowTranslucencyDoF;

    /**
     * Whether the material should use one-layer distortion, which can be cheaper than normal distortion for some primitive types (mainly fluid surfaces).
     * One layer distortion won't handle overlapping one layer distortion primitives correctly.
     * This causes an extra scene color resolve for the first primitive that uses one layer distortion and so should only be used in very specific circumstances.
     */
    [UProperty] public bool bUseOneLayerDistortion;

    /** If this is set, a depth-only pass for will be rendered for solid (A=255) areas of dynamic lit translucency primitives. This improves hair sorting at the extra render cost. */
    [UProperty] public bool bUseLitTranslucencyDepthPass;

    /** If this is set, a depth-only pass for will be rendered for any visible (A>0) areas of dynamic lit translucency primitives. This is necessary for correct fog and DoF of hair */
    [UProperty] public bool bUseLitTranslucencyPostRenderDepthPass;

    /** Whether to treat the material's opacity channel as a mask rather than fractional translucency in dynamic shadows. */
    [UProperty] public bool bCastLitTranslucencyShadowAsMasked;

    [UProperty] public bool bUsedAsLightFunction;
    /** Indicates that the material is used on fog volumes.  This usage flag is mutually exclusive with all other mesh type usage flags! */
    [UProperty] public bool bUsedWithFogVolumes;

    /** 
     * This is a special usage flag that allows a material to be assignable to any primitive type.
     * This is useful for materials used by code to implement certain viewmodes, for example the default material or lighting only material.
     * The cost is that nearly 20x more shaders will be compiled for the material than the average material, which will greatly increase shader compile time and memory usage.
     * This flag should only be set when absolutely necessary, and is purposefully not exposed to the UI to prevent abuse.
     */
    [UProperty] protected bool bUsedAsSpecialEngineMaterial;

    /** 
     * Indicates that the material and its instances can be assigned to skeletal meshes.  
     * This will result in the shaders required to support skeletal meshes being compiled which will increase shader compile time and memory usage.
     */
    [UProperty] public bool bUsedWithSkeletalMesh;
    [UProperty] public bool bUsedWithTerrain;
    [UProperty] public bool bUsedWithLandscape;
    [UProperty] public bool bUsedWithMobileLandscape;
    [UProperty] public bool bUsedWithFracturedMeshes;
    [UProperty] public bool bUsedWithParticleSystem;
    [UProperty] public bool bUsedWithParticleSprites;
    [UProperty] public bool bUsedWithBeamTrails;
    [UProperty] public bool bUsedWithParticleSubUV;
    [UProperty] public bool bUsedWithSpeedTree;
    [UProperty] public bool bUsedWithStaticLighting;
    [UProperty] public bool bUsedWithLensFlare;

    /** 
     * Gamma corrects the output of the base pass using the current render target's gamma value. 
     * This must be set on materials used with UIScenes to get correct results.
     */
    [UProperty] public bool bUsedWithGammaCorrection;
    /** Enables instancing for mesh particles.  Use the "Vertex Color" node when enabled, not "MeshEmit VertColor." */
    [UProperty] public bool bUsedWithInstancedMeshParticles;
    [UProperty] public bool bUsedWithFluidSurfaces;
    /** WARNING: bUsedWithDecals is mutually exclusive with all other mesh type usage flags!  A material with bUsedWithDecals=true will not work on any other mesh type. */
    [UProperty] public bool bUsedWithDecals;
    [UProperty] public bool bUsedWithMaterialEffect;
    [UProperty] public bool bUsedWithMorphTargets;
    [UProperty] public bool bUsedWithRadialBlur;
    [UProperty] public bool bUsedWithInstancedMeshes;
    [UProperty] public bool bUsedWithSplineMeshes;
    [UProperty] public bool bUsedWithAPEXMeshes;

    /** Enables support for screen door fading for primitives rendering with this material.  This adds an extra texture lookup and a few extra instructions. */
    [UProperty] public bool bUsedWithScreenDoorFade;

    [UProperty] public bool Wireframe;

    /// <summary>
    /// When enabled, the camera vector will be computed in the pixel shader instead of the vertex shader which may improve the quality of the reflection.
    /// Enabling this setting also allows VertexColor expressions to be used alongside Transform expressions.
    /// </summary>
    [UProperty] public bool bPerPixelCameraVector;

    /// <summary>
    /// Controls whether lightmap specular will be rendered or not.  Can be disabled to reduce instruction count.
    /// </summary>
    [UProperty] public bool bAllowLightmapSpecular;

    // indexed by EMaterialShaderQuality
    // Set of compiled materials at all of the MaterialShaderQuality levels
    // public const native duplicatetransient pointer MaterialResources[2]{FMaterialResource};
    [UProperty] public FMaterialResource[] MaterialResources;

    // second is used when selected
    // public const native duplicatetransient pointer DefaultMaterialInstances[3]{class FDefaultMaterialInstance};
    // public FDefaultMaterialIsntance[3] DefaultMaterialInstances = [null, null, null];

    [UProperty] public int EditorX, EditorY, EditorPitch, EditorYaw;

    /// <summary>
    /// Array of material expressions, excluding Comments. Used by the material editor.
    /// </summary>
    [UProperty] public UMaterialExpression[] Expressions;

    /// <summary>
    /// Array of comments associated with this material; viewed in the material editor.
    /// </summary>
    /// <remarks>Editor-only.</remarks>
    [UProperty] public UMaterialExpressionComment[] EditorComments;

    /// <summary>
    /// Array of all functions this material depends on.
    /// </summary>
    [UProperty] public MaterialFunctionInfo[] MaterialFunctionInfos;

    // public native map{FName, TArray<UMaterialExpression*>} EditorParameters;

    /// <summary>
    /// TRUE if Material uses distortion.
    /// </summary>
    [UProperty] public bool bUsesDistortion;

    /// <summary>
    /// TRUE if Material is masked and uses custom opacity.
    /// </summary>
    [UProperty] public bool bIsMasked;

    /// <summary>
    /// TRUE if Material is the preview material used in the material editor.
    /// </summary>
    [UProperty] protected bool bIsPreviewMaterial;

    /// <remarks>Editor-only.</remarks>
    [UProperty] public FGuid[] ReferencedTextureGuids;

    #endregion
}

public partial class MaterialInput : PropertyHolder
{
    #region UProperties

    /// <summary>
    /// Material expression that this input is connected to, or NULL if not connected.
    /// </summary>
    [UProperty] public UMaterialExpression Expression;

    /// <summary>
    /// Index into Expression's outputs array that this input is connected to.
    /// </summary>
    [UProperty] public int OutputIndex;

    /// <summary>
    /// Optional name of the input.
    /// Note that this is the only member which is not derived from the output currently connected. 
    /// </summary>
    [UProperty] public string InputName;

    [UProperty] public int Mask, MaskR, MaskG, MaskB, MaskA;

    [UProperty] public bool UseConstant;

    #endregion
}

public partial class ColorMaterialInput : MaterialInput
{
    #region UProperties

    [UProperty] public Color Constant;

    #endregion
}

public partial class ScalarMaterialInput : MaterialInput
{
    #region UProperties

    [UProperty] public float Constant;

    #endregion
}

public partial class VectorMaterialInput : MaterialInput
{
    #region UProperties

    [UProperty] public Vector Constant;

    #endregion
}

public partial class Vector2MaterialInput : MaterialInput
{
    #region UProperties

    [UProperty] public float ConstantX, ConstantY;

    #endregion
}

/// <summary>
/// Stores information about a function that this material references, used to know when the material needs to be recompiled.
/// </summary>
public partial class MaterialFunctionInfo : PropertyHolder
{
    #region UProperties

    /// <summary>
    /// ID that the function had when this material was last compiled.
    /// </summary>
    [UProperty] public FGuid StateId;

    /// <summary>
    /// The function which this material has a dependency on.
    /// </summary>
    [UProperty] /*public Pointer<UMaterialFunction>*/ int Function;

    #endregion
};