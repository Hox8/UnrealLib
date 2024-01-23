using System;
using System.Reflection.Metadata;
using UnrealLib.Core;
using UnrealLib.Enums;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.Materials;

public class UMaterial(FObjectExport export) : UMaterialInterface(export)
{
    #region Structs

    // Material input structs.

    public class MaterialInput : PropertyHolder
    {
        /** Material expression that this input is connected to, or NULL if not connected. */
        public UMaterialExpression Expression;

        /** Index into Expression's outputs array that this input is connected to. */
        public int OutputIndex;

        /** 
         * Optional name of the input.  
         * Note that this is the only member which is not derived from the output currently connected. 
         */
        public string InputName;
        public int Mask, MaskR, MaskG, MaskB, MaskA;
        // public int					GCC64_Padding; // @todo 64: if the C++ didn't mismirror this structure (with ExpressionInput), we might not need this

        public bool UseConstant;

        internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
        {
            switch (tag.Name.GetString)
            {
                case nameof(Expression): throw new NotImplementedException();
                case nameof(OutputIndex): Ar.Serialize(ref OutputIndex); break;
                case nameof(InputName): Ar.Serialize(ref InputName); break;
                case nameof(Mask): Ar.Serialize(ref Mask); break;
                case nameof(MaskR): Ar.Serialize(ref MaskR); break;
                case nameof(MaskG): Ar.Serialize(ref MaskG); break;
                case nameof(MaskB): Ar.Serialize(ref MaskB); break;
                case nameof(MaskA): Ar.Serialize(ref MaskA); break;
                case nameof(UseConstant): Ar.Serialize(ref UseConstant); break;
                default: base.ParseProperty(Ar, tag); break;
            }
        }
    }

    public class ColorMaterialInput : MaterialInput
    {
        public Color Constant;

        internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
        {
            switch (tag.Name.GetString)
            {
                case nameof(Constant): Ar.Serialize(ref Constant); break;
                default: base.ParseProperty(Ar, tag); break;
            }
        }
    }

    public class ScalarMaterialInput : MaterialInput
    {
        public float Constant;

        internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
        {
            switch (tag.Name.GetString)
            {
                case nameof(Constant): Ar.Serialize(ref Constant); break;
                default: base.ParseProperty(Ar, tag); break;
            }
        }
    }

    public class VectorMaterialInput : MaterialInput
    {
        public Vector3 Constant;

        internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
        {
            switch (tag.Name.GetString)
            {
                case nameof(Constant): Ar.Serialize(ref Constant); break;
                default: base.ParseProperty(Ar, tag); break;
            }
        }
    }

    public class Vector2MaterialInput : MaterialInput
    {
        public float ConstantX, ConstantY;

        internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
        {
            switch (tag.Name.GetString)
            {
                case nameof(ConstantX): Ar.Serialize(ref ConstantX); break;
                case nameof(ConstantY): Ar.Serialize(ref ConstantY); break;
                default: base.ParseProperty(Ar, tag); break;
            }
        }
    }

    /// <summary>
    ///  Stores information about a function that this material references, used to know when the material needs to be recompiled.
    /// </summary>
    public struct MaterialFunctionInfo
    {
        /** Id that the function had when this material was last compiled. */
        public Guid StateId;

        /** The function which this material has a dependency on. */
        public UMaterialFunction Function;
    };

    #endregion

    #region Properties

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

    public ColorMaterialInput DiffuseColor = new();
    public ScalarMaterialInput DiffusePower = new();
    public ColorMaterialInput SpecularColor = new();
    public ScalarMaterialInput SpecularPower = new();
    public VectorMaterialInput Normal = new();

    #endregion

    #region Emission

    public ColorMaterialInput EmissiveColor = new();

    #endregion

    public ScalarMaterialInput Opacity = new();
    public ScalarMaterialInput OpacityMask = new();

    /** If BlendMode is BLEND_Masked or BLEND_SoftMasked, the surface is not rendered where OpacityMask < OpacityMaskClipValue. */
    public float OpacityMaskClipValue;

    /** Can be used to bias shadows away from the surface. */
    public float ShadowDepthBias;

    /** Allows the material to distort background color by offsetting each background pixel by the amount of the distortion input for that pixel. */
    public Vector2MaterialInput Distortion;

    /** Determines how the material's color is blended with background colors. */
    public EBlendMode BlendMode;

    /** Determines how inputs are combined to create the material's final color. */
    public EMaterialLightingModel LightingModel;

    /** 
     * Use a custom light transfer equation to be factored with light color, attenuation and shadowing. 
     * This is currently only used for Movable, Toggleable and Dominant light contribution.
     * LightVector can be used in this material input and will be set to the tangent space light direction of the current light being rendered.
     */
    public ColorMaterialInput CustomLighting;

    /** 
     * Use a custom diffuse factor for attenuation with lights that only support a diffuse term. 
     * This should only be the diffuse color coefficient, and must not depend on LightVector.
     * This is currently used with skylights, SH lights, materials exported to lightmass and directional lightmap contribution.
     */
    public ColorMaterialInput CustomSkylightDiffuse;

    /** Specify a vector to use as anisotropic direction */
    public VectorMaterialInput AnisotropicDirection;

    /** Lerps between lighting color (diffuse * attenuation * Lambertian) and lighting without the Lambertian term color (diffuse * attenuation * TwoSidedLightingColor). */
    public ScalarMaterialInput TwoSidedLightingMask;

    /** Modulates the lighting without the Lambertian term in two sided lighting. */
    public ColorMaterialInput TwoSidedLightingColor;

    /** Adds to world position in the vertex shader. */
    public VectorMaterialInput WorldPositionOffset;

    /** Offset in tangent space applied to tessellated vertices.  A scalar connected to this input will be treated as the z component (float3(0,0,x)). */
    public VectorMaterialInput WorldDisplacement;

    /** Multiplies the tessellation factors applied when a tessellation mode is set. */
    public ScalarMaterialInput TessellationMultiplier;

    /** Modulates the local contribution to the subsurface scattered lighting of the material. */
    public ColorMaterialInput SubsurfaceInscatteringColor;

    /** Light from subsurface scattering is attenuated by SubsurfaceAbsorptionColor^Distance. */
    public ColorMaterialInput SubsurfaceAbsorptionColor;

    /** The maximum distance light from subsurface scattering will travel. */
    public ScalarMaterialInput SubsurfaceScatteringRadius;

    /** Indicates that the material should be rendered in the SeparateTranslucency Pass (does not affect bloom, not affected by DOF, requires bAllowSeparateTranslucency to be set in .ini). */
    public bool EnableSeparateTranslucency;

    /** Indicates that the material should be rendered without backface culling and the normal should be flipped for backfaces. */
    public bool TwoSided;

    /** Indicates that the material should be rendered in its own pass. Used for hair renderering */
    public bool TwoSidedSeparatePass;

    /**
     * Allows the material to disable depth tests, which is only meaningful with translucent blend modes.
     * Disabling depth tests will make rendering significantly slower since no occluded pixels can get zculled.
     */
    public bool bDisableDepthTest;

    /** 
     * If enabled and this material reads from scene texture, this material will be rendered behind all other translucency, 
     * Instead of the default behavior for materials that read from scene texture, which is for them to render in front of all other translucency in the same DPG.
     * This is useful for placing large spheres around a level that read from scene texture to do chromatic aberration.
     */
    public bool bSceneTextureRenderBehindTranslucency;

    /** Whether the material should allow fog or be unaffected by fog.  This only has meaning for materials with translucent blend modes. */
    public bool bAllowFog;

    /** 
     * Whether the material should receive dynamic dominant light shadows from static objects when the material is being lit by a light environment. 
     * This is useful for character hair.
     */
    public bool bTranslucencyReceiveDominantShadowsFromStatic;

    /** 
     * Whether the material should inherit the dynamic shadows that dominant lights are casting on opaque and masked materials behind this material.
     * This is useful for ground meshes using a translucent blend mode and depth biased alpha to hide seams.
     */
    public bool bTranslucencyInheritDominantShadowsFromOpaque;

    /** Whether the material should allow Depth of Field or be unaffected by DoF.  This only has meaning for materials with translucent blend modes. */
    public bool bAllowTranslucencyDoF;

    /**
     * Whether the material should use one-layer distortion, which can be cheaper than normal distortion for some primitive types (mainly fluid surfaces).
     * One layer distortion won't handle overlapping one layer distortion primitives correctly.
     * This causes an extra scene color resolve for the first primitive that uses one layer distortion and so should only be used in very specific circumstances.
     */
    public bool bUseOneLayerDistortion;

    /** If this is set, a depth-only pass for will be rendered for solid (A=255) areas of dynamic lit translucency primitives. This improves hair sorting at the extra render cost. */
    public bool bUseLitTranslucencyDepthPass;

    /** If this is set, a depth-only pass for will be rendered for any visible (A>0) areas of dynamic lit translucency primitives. This is necessary for correct fog and DoF of hair */
    public bool bUseLitTranslucencyPostRenderDepthPass;

    /** Whether to treat the material's opacity channel as a mask rather than fractional translucency in dynamic shadows. */
    public bool bCastLitTranslucencyShadowAsMasked;

    public bool bUsedAsLightFunction;
    /** Indicates that the material is used on fog volumes.  This usage flag is mutually exclusive with all other mesh type usage flags! */
    public bool bUsedWithFogVolumes;

    /** 
     * This is a special usage flag that allows a material to be assignable to any primitive type.
     * This is useful for materials used by code to implement certain viewmodes, for example the default material or lighting only material.
     * The cost is that nearly 20x more shaders will be compiled for the material than the average material, which will greatly increase shader compile time and memory usage.
     * This flag should only be set when absolutely necessary, and is purposefully not exposed to the UI to prevent abuse.
     */
    protected bool bUsedAsSpecialEngineMaterial;

    /** 
     * Indicates that the material and its instances can be assigned to skeletal meshes.  
     * This will result in the shaders required to support skeletal meshes being compiled which will increase shader compile time and memory usage.
     */
    public bool bUsedWithSkeletalMesh;
    public bool bUsedWithTerrain;
    public bool bUsedWithLandscape;
    public bool bUsedWithMobileLandscape;
    public bool bUsedWithFracturedMeshes;
    public bool bUsedWithParticleSystem;
    public bool bUsedWithParticleSprites;
    public bool bUsedWithBeamTrails;
    public bool bUsedWithParticleSubUV;
    public bool bUsedWithSpeedTree;
    public bool bUsedWithStaticLighting;
    public bool bUsedWithLensFlare;

    /** 
     * Gamma corrects the output of the base pass using the current render target's gamma value. 
     * This must be set on materials used with UIScenes to get correct results.
     */
    public bool bUsedWithGammaCorrection;
    /** Enables instancing for mesh particles.  Use the "Vertex Color" node when enabled, not "MeshEmit VertColor." */
    public bool bUsedWithInstancedMeshParticles;
    public bool bUsedWithFluidSurfaces;
    /** WARNING: bUsedWithDecals is mutually exclusive with all other mesh type usage flags!  A material with bUsedWithDecals=true will not work on any other mesh type. */
    public bool bUsedWithDecals;
    public bool bUsedWithMaterialEffect;
    public bool bUsedWithMorphTargets;
    public bool bUsedWithRadialBlur;
    public bool bUsedWithInstancedMeshes;
    public bool bUsedWithSplineMeshes;
    public bool bUsedWithAPEXMeshes;

    /** Enables support for screen door fading for primitives rendering with this material.  This adds an extra texture lookup and a few extra instructions. */
    public bool bUsedWithScreenDoorFade;

    public bool Wireframe;

    /// <summary>
    /// When enabled, the camera vector will be computed in the pixel shader instead of the vertex shader which may improve the quality of the reflection.
    /// Enabling this setting also allows VertexColor expressions to be used alongside Transform expressions.
    /// </summary>
    public bool bPerPixelCameraVector;

    /// <summary>
    /// Controls whether lightmap specular will be rendered or not.  Can be disabled to reduce instruction count.
    /// </summary>
    public bool bAllowLightmapSpecular;

    // indexed by EMaterialShaderQuality
    // Set of compiled materials at all of the MaterialShaderQuality levels
    // public const native duplicatetransient pointer MaterialResources[2]{FMaterialResource};
    public FMaterialResource[] MaterialResources = [new(), new(), new()];

    // second is used when selected
    // public const native duplicatetransient pointer DefaultMaterialInstances[3]{class FDefaultMaterialInstance};
    // public FDefaultMaterialIsntance[3] DefaultMaterialInstances = [null, null, null];

    public int EditorX, EditorY, EditorPitch, EditorYaw;

    /// <summary>
    /// Array of material expressions, excluding Comments. Used by the material editor.
    /// </summary>
    public UMaterialExpression[] Expressions;

    /// <summary>
    /// Array of comments associated with this material; viewed in the material editor.
    /// </summary>
    /// <remarks>Editor-only.</remarks>
    public UMaterialExpressionComment[] EditorComments;

    /// <summary>
    /// Array of all functions this material depends on.
    /// </summary>
    public MaterialFunctionInfo[] MaterialFunctionInfos;

    // public native map{FName, TArray<UMaterialExpression*>} EditorParameters;

    /// <summary>
    /// TRUE if Material uses distortion.
    /// </summary>
    public bool bUsesDistortion;

    /// <summary>
    /// TRUE if Material is masked and uses custom opacity.
    /// </summary>
    public bool bIsMasked;

    /// <summary>
    /// TRUE if Material is the preview material used in the material editor.
    /// </summary>
    protected bool bIsPreviewMaterial;

    /// <remarks>Editor-only.</remarks>
    public Guid[] ReferencedTextureGuids;

    #endregion

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            case nameof(DiffuseColor): DiffuseColor.SerializeProperties(Ar); break;
            case nameof(DiffusePower): DiffusePower.SerializeProperties(Ar); break;
            case nameof(SpecularColor): SpecularColor.SerializeProperties(Ar); break;
            case nameof(SpecularPower): SpecularPower.SerializeProperties(Ar); break;
            case nameof(Normal): Normal.SerializeProperties(Ar); break;
            case nameof(EmissiveColor): EmissiveColor.SerializeProperties(Ar); break;
            case nameof(Opacity): Opacity.SerializeProperties(Ar); break;
            case nameof(OpacityMask): OpacityMask.SerializeProperties(Ar); break;

            default: base.ParseProperty(Ar, tag); break;
        }
    }

    //public override void Serialize(UnrealArchive Ar)
    //{
    //    base.Serialize(Ar);

    //    int QualityMask = 1;
    //    if (/*Ar.Version > 858*/ true)
    //    {
    //        Ar.Serialize(ref QualityMask);
    //    }

    //    for (int QualityIndex = 0; QualityIndex < 2; QualityIndex++)
    //    {
    //        if ((QualityMask & (1 << QualityIndex)) != 0) continue;

    //        MaterialResources[QualityIndex].Serialize(Ar);
    //    }
    //}
}
