using NetEscapades.EnumGenerators;
using System;
using System.Diagnostics;
using UnrealLib.Core;
using UnrealLib.Experimental.Component;
using UnrealLib.Experimental.Mesh;
using UnrealLib.Experimental.Textures;
using UnrealLib.Interfaces;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Components;

[EnumExtensions]
public enum ELightmapModificationFunction
{
    /// <summary>
    /// Lightmap.RGB * Modification.RGB.
    /// </summary>
    MLMF_Modulate,
    /// <summary>
    /// Lightmap.RGB * (Modification.RGB * Modification.A).
    /// </summary>
    MLMF_ModulateAlpha
}

public record struct StaticMeshComponentLODInfo : ISerializable
{
    private /*UShadowMap2D[]*/ int[] ShadowMaps;
    private /*UObject[]*/ int[] ShadowVertexBuffers;
    private FLightMap LightMap;

    /// <summary>
    /// Vertex colors to use for this mesh LOD.
    /// </summary>
    private FColorVertexBuffer OverrideVertexColors;

    /// <summary>
    /// Vertex data cached at the time this LOD was painted, if any.
    /// </summary>
    private PaintedVertex[] PaintedVertices;

    private bool bLoadVertexColorData;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref ShadowMaps);
        Ar.Serialize(ref ShadowVertexBuffers);
        FLightMap.Serialize(Ar, ref LightMap);

        Ar.Serialize(ref bLoadVertexColorData);
        if (bLoadVertexColorData)
        {
            Ar.Serialize(ref OverrideVertexColors);
        }

        if (Ar.Version >= 801 && Ar.Version < 823)
        {
            Vector[] VertexColorPositions = default;
            Ar.Serialize(ref VertexColorPositions);
        }

        if (Ar.Version >= 823)
        {
            Ar.Serialize(ref PaintedVertices);
        }
    }
}

/// <summary>
/// Per-object settings for Lightmass.
/// </summary>
public record struct LightmassPrimitiveSettings()
{
    /// <summary>If TRUE, this object will be lit as if it receives light from both sides of its polygons.</summary>
    public bool bUseTwoSidedLighting;
    /// <summary>If TRUE, this object will only shadow indirect lighting.</summary>
    public bool bShadowIndirectOnly;
    /// <summary>If TRUE, allow using the emissive for static lighting.</summary>
    public bool bUseEmissiveForStaticLighting;
    /// <summary>Direct lighting falloff exponent for mesh area lights created from emissive areas on this primitive.</summary>
    public float EmissiveLightFalloffExponent = 2.0f;

    /// <summary>
    /// Direct lighting influence radius.
    /// The default is 0, which means the influence radius should be automatically generated based on the emissive light brightness.
    /// Values greater than 0 override the automatic method.
    /// </summary>
    public float EmissiveLightExplicitInfluenceRadius;
    /// <summary>Scales the emissive contribution of all materials applied to this object.</summary>
    public float EmissiveBoost = 1.0f;
    /// <summary>Scales the diffuse contribution of all materials applied to this object.</summary>
    public float DiffuseBoost = 1.0f;
    /// <summary>Scales the specular contribution of all materials applied to this object.</summary>
    public float SpecularBoost = 1.0f;
    /// <summary>Fraction of samples taken that must be occluded in order to reach full occlusion.</summary>
    public float FullyOccludedSamplesFraction = 1.0f;
}

public partial class UStaticMeshComponent(FObjectExport? export = null) : UMeshComponent(export)
{
    #region UProperties

    #region Overrides

    // @TODO 'new' keyword makes a copy which is not ideal
    [UProperty] public new ETickingGroup TickGroup = ETickingGroup.TG_PreAsyncWork;
    [UProperty] public new bool CollideActors = true;
    [UProperty] public new bool BlockActors = true;
    [UProperty] public new bool BlockZeroExtent = true;
    [UProperty] public new bool BlockNonZeroExtent = true;
    [UProperty] public new bool BlockRigidBody = true;
    [UProperty] public new bool bAcceptsStaticDecals = true;

    #endregion

    /// <summary>
    /// If 0, auto-select LOD level. If > 0, force to (ForcedLodModel - 1).
    /// </summary>
    [UProperty] public int ForcedLodModel;
    [UProperty] public int PreviousLODLevel;

    [UProperty(ObjectProperty = true)] public /*UStaticMesh*/ int StaticMesh;
    [UProperty] public Color WireframeColor = new(r: 0, g: 255, b: 255, a: 255);

    [UProperty] public bool bIgnoreInstanceForTextureStreaming;

    // [UProperty] public bool bOverrideLightMapResolution;            // DEPRECATED. When? Replaced by 'bOverrideLightMapRes'.
    /// <summary>Whether to override the lightmap resolution defined in the static mesh.</summary>
    [UProperty] public bool bOverrideLightMapRes;
    /// <summary>Light map resolution used if bOverrideLightMapRes is TRUE.</summary>
    [UProperty] public int OverriddenLightMapRes = 64;

    /// <summary>
    /// With the default value of 0, the LODMaxRange from the UStaticMesh will be used to control LOD transitions, otherwise this value overrides.
    /// </summary>
    [UProperty] public float OverriddenLODMaxRange;

    /// <summary>
    /// Allows adjusting the desired streaming distance of streaming textures that uses UV 0.
    /// 1.0 is the default, whereas a higher value makes the textures stream in sooner from far away.
    /// A lower value (0.0 - 1.0) makes the textures stream in later (you have to be closer).
    /// </summary>
    [UProperty] public float StreamingDistanceMultiplier = 1.0f;

    /// <summary>Subdivision step size for static vertex lighting.</summary>
    [UProperty] public int SubDivisionStepSize = 32;
    /// <summary>Whether to use subdivisions or just the triangle's vertices.</summary>
    [UProperty] public bool bUseSubDivisions = true;

    /// <summary>Whether or not to use the optional simple lightmap modification texture.</summary>
    [UProperty] public bool bUseSimpleLightmapModifications;

    /// <summary>The texture to use when modifying the simple lightmap texture.</summary>
    [UProperty(ObjectProperty = true)] public /*UTexture*/ int SimpleLightmapModificationTexture;
    /// <summary>The function to use when modifying the simple lightmap texture.</summary>
    [UProperty] public ELightmapModificationFunction SimpleLightmapModificationFunction;

    /// <summary>
    /// Never become dynamic, even if mesh has bCanBecomeDynamic set to true.
    /// </summary>
    [UProperty] public bool bNeverBecomeDynamic;

    // public FGuid[] IrrelevantLights;     // This is likely only used during swarm

    /// <summary>
    /// Incremented any time the position of vertices from the source mesh change, used to determine if an update from the source static mesh is required.
    /// </summary>
    [UProperty] public int VertexPositionVersionNumber;

    /// <summary>
    /// The Lightmass settings for this object.
    /// </summary>
    [UProperty] public LightmassPrimitiveSettings LightmassSettings;

    #endregion

    // This should probably be marked as a UProperty
    /// <summary>
    /// Static mesh LOD data.
    /// Contains static lighting data along with instanced mesh vertex colors.
    /// </summary>
    public StaticMeshComponentLODInfo[] LODData;

    public unsafe override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref LODData);

        if (Ar.Version < 820)
        {
            if (Ar.Version >= 801)
            {
                int dummy = -1;
                Ar.Serialize(ref dummy);

                Debug.Assert(dummy == VertexPositionVersionNumber);
            }
        }
    }
}
