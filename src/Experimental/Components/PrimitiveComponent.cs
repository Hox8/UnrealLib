using NetEscapades.EnumGenerators;
using System.Runtime.InteropServices;
using UnrealLib.Core;
using UnrealLib.Enums;
using UnrealLib.Experimental.Mesh;
using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Components;

/// <summary>
/// Enum indicating different type of objects for rigid-body collision purposes.
/// </summary>
[EnumExtensions]
public enum ERBCollisionChannel
{
    RBCC_Default,
    RBCC_Nothing, // Special channel that nothing should request collision with.
    RBCC_Pawn,
    RBCC_Vehicle,
    RBCC_Water,
    RBCC_GameplayPhysics,
    RBCC_EffectPhysics,
    RBCC_Untitled1,
    RBCC_Untitled2,
    RBCC_Untitled3,
    RBCC_Untitled4,
    RBCC_Cloth,
    RBCC_FluidDrain,
    RBCC_SoftBody,
    RBCC_FracturedMeshPart,
    RBCC_BlockingVolume,
    RBCC_DeadPawn,
    RBCC_Clothing,
    RBCC_ClothingCollision
}

public partial class LightingChannelContainer : PropertyHolder
{
    /// <summary>
    /// Whether the lighting channel has been initialized.
    /// </summary>
    [UProperty] public bool bInitialized;

    // User-settable channels that are auto set and true for lights
    [UProperty] public bool BSP;
    [UProperty] public bool Static;
    [UProperty] public bool Dynamic;

    // User-set channels
    [UProperty] public bool CompositeDynamic;
    [UProperty] public bool Skybox;
    [UProperty] public bool Unnamed_1;
    [UProperty] public bool Unnamed_2;
    [UProperty] public bool Unnamed_3;
    [UProperty] public bool Unnamed_4;
    [UProperty] public bool Unnamed_5;
    [UProperty] public bool Unnamed_6;
    [UProperty] public bool Cinematic_1;
    [UProperty] public bool Cinematic_2;
    [UProperty] public bool Cinematic_3;
    [UProperty] public bool Cinematic_4;
    [UProperty] public bool Cinematic_5;
    [UProperty] public bool Cinematic_6;
    [UProperty] public bool Cinematic_7;
    [UProperty] public bool Cinematic_8;
    [UProperty] public bool Cinematic_9;
    [UProperty] public bool Cinematic_10;
    [UProperty] public bool Gameplay_1;
    [UProperty] public bool Gameplay_2;
    [UProperty] public bool Gameplay_3;
    [UProperty] public bool Gameplay_4;
    [UProperty] public bool Crowd;
}

/// <summary>
/// Container for indicating a set of collision channel that this object will collide with.
/// </summary>
public partial class RBCollisionChannelContainer : PropertyHolder
{
    [UProperty] public bool Default;
    [UProperty] public bool Nothing; // This is reserved to allow an object to opt-out of all collisions, and should not be set.
    [UProperty] public bool Pawn;
    [UProperty] public bool Vehicle;
    [UProperty] public bool Water;
    [UProperty] public bool GameplayPhysics;
    [UProperty] public bool EffectPhysics;
    [UProperty] public bool Untitled1;
    [UProperty] public bool Untitled2;
    [UProperty] public bool Untitled3;
    [UProperty] public bool Untitled4;
    [UProperty] public bool Cloth;
    [UProperty] public bool FluidDrain;
    [UProperty] public bool SoftBody;
    [UProperty] public bool FracturedMeshPart;
    [UProperty] public bool BlockingVolume;
    [UProperty] public bool DeadPawn;
    [UProperty] public bool Clothing;
    [UProperty] public bool ClothingCollision;
}

/// <summary>
/// Cached vertex information at the time the mesh was painted.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct PaintedVertex
{
    public Vector Position;
    public FPackedNormal Normal;
    public Color Color;
}

public partial class UPrimitiveComponent(FObjectExport? export = null) : UActorComponent(export)
{
    #region UProperties

    [UProperty(ObjectProperty = true)] public /*UPrimitiveComponent*/ int ShadowParent;

    /// <summary>
    /// Replacement primitive to draw instead of this one (multiple UPrim's will point to the same Replacement).
    /// </summary>
    [UProperty(ObjectProperty = true)] public /*UPrimitiveComponent*/ int ReplacementPrimitive = -1;

    #region Rendering

    /// <summary>
    /// The lighting environment to take the primitive's lighting from.
    /// </summary>
    [UProperty(ObjectProperty = true)] public /*ULightEnvironmentComponent*/ int LightEnvironment;

    /// <summary>
    /// The minimum distance at which the primitive should be rendered,
    /// measured in world space units from the center of the primitive's bounding sphere to the camera position.
    /// </summary>
    [UProperty] public float MinDrawDistance;
    /// <summary>
    /// The distance at which the renderer will switch from parent (low LOD) to children (high LOD).
    /// This is basically the same as MinDrawDistance, except that the low LOD will draw even up close, if there are no children.
    /// This is needed so the high lod meshes can be in a streamable sublevel, and if streamed out, the low LOD will draw up close.
    /// </summary>
    [UProperty] public float MassiveLODDistance;
    /// <summary>
    /// Max draw distance exposed to LDs. The real max draw distance is the min (disregarding 0) of this and volumes affecting this object.
    /// </summary>
    [UProperty] public float MaxDrawDistance;
    /// <summary>
    /// The distance to cull this primitive at.
    /// A CachedMaxDrawDistance of 0 indicates that the primitive should not be culled by distance.
    /// </summary>
    [UProperty] public float CachedMaxDrawDistance;
    /// <summary>
    /// Scalar controlling the amount of motion blur to be applied when an object moves.
    /// 0 = none, 1 = full instance motion blur (default). Value should be 0 or bigger.
    /// </summary>
    [UProperty] public float MotionBlurInstanceScale = 1.0f;

    /// <summary>The scene depth priority group to draw the primitive in.</summary>
    [UProperty] public ESceneDepthPriorityGroup DepthPriorityGroup = ESceneDepthPriorityGroup.SDPG_World;
    /// <summary>The scene depth priority group to draw the primitive in, if it's being viewed by its owner.</summary>
    [UProperty] public ESceneDepthPriorityGroup ViewOwnerDepthPriorityGroup;
    /// <summary>If detail mode is > system detail mode, primitive won't be rendered.</summary>
    [UProperty] public EDetailMode DetailMode;

    /// <summary>
    /// Enum indicating what type of object this should be considered for rigid body collision.
    /// </summary>
    [UProperty] public ERBCollisionChannel RBChannel;
    /// <summary>
    /// Used for creating one-way physics interactions (via constraints or contacts).
    /// Groups with lower RBDominanceGroup push around higher values in a 'one way' fashion. Must be < 32.
    /// </summary>
    [UProperty] public byte RBDominanceGroup = 15;

    /// <summary>
    /// Environment shadow factor used when previewing unbuilt lighting on this primitive.
    /// </summary>
    [UProperty] public byte PreviewEnvironmentShadowing = 180;

    /// <summary>
    /// True if the primitive should be rendered using ViewOwnerDepthPriorityGroup if viewed by its owner.
    /// </summary>
    [UProperty] public bool bUseViewOwnerDepthPriorityGroup;

    [UProperty] public bool bAllowCullDistanceVolume = true;

    [UProperty] public bool HiddenGame;
    [UProperty] public bool HiddenEditor;
    /// <summary>
    /// If this is True, this component won't be visible when the view actor is the component's owner, directly or indirectly.
    /// </summary>
    [UProperty] public bool bOwnerNoSee;
    /// <summary>
    /// If this is True, this component will only be visible when the view actor is the component's owner, directly or indirectly.
    /// </summary>
    [UProperty] public bool bOnlyOwnerSee;
    /// <summary>
    /// If true, bHidden on the Owner of this component will be ignored.
    /// </summary>
    [UProperty] public bool bIgnoreOwnerHidden;

    /// <summary>
    /// Whether to render the primitive in the depth only pass.
    /// Setting this to FALSE will cause artifacts with dominant light shadows and potentially large performance loss,
    /// So it should be TRUE on all lit objects, setting it to FALSE is mostly only useful for debugging.
    /// </summary>
    [UProperty] public bool bUseAsOccluder;
    /// <summary>
    /// If this is True, this component doesn't need exact occlusion info.
    /// </summary>
    [UProperty] public bool bAllowApproximateOcclusion;
    /// <summary>
    /// If this is True, this component will return 0.0f as their occlusion when first rendered.
    /// </summary>
    [UProperty] public bool bFirstFrameOcclusion;
    /// <summary>
    /// If True, this component will still be queried for occlusion even when it intersects the near plane.
    /// </summary>
    [UProperty] public bool bIgnoreNearPlaneIntersection;

    /// <summary>
    /// If this is True, this component can be selected in the editor.
    /// </summary>
    [UProperty] public bool bSelectable = true;

    /// <summary>
    /// If TRUE, forces mips for textures used by this component to be resident when this component's level is loaded.
    /// </summary>
    [UProperty] public bool bForceMipStreaming;

    /// <summary>If TRUE, this primitive accepts static level placed decals in the editor.</summary>
    [UProperty] public bool bAcceptsStaticDecals;
    /// <summary>If TRUE, this primitive accepts dynamic decals spawned during gameplay.</summary>
    [UProperty] public bool bAcceptsDynamicDecals = true;

    /// <summary>If true, a hit-proxy will be generated for each instance of instanced static meshes.</summary>
    [UProperty] public bool bUsePerInstanceHitProxies;

    #endregion

    #region Lighting

    /// <summary>
    /// Controls whether the primitive component should cast a shadow or not.
    /// Dynamic primitives will not receive shadows from static objects unless both this flag and bCastDynamicShadow are enabled.
    /// </summary>
    [UProperty] public bool CastShadow;

    /// <summary>
    /// If true, forces all static lights to use light-maps for direct lighting on this primitive,
    /// regardless of the light's UseDirectLightMap property.
    /// </summary>
    [UProperty] public bool bForceDirectLightMap;

    /// <summary>
    /// If false, primitive does not cast dynamic shadows.
    /// </summary>
    [UProperty] public bool bCastDynamicShadow = true;
    /// <summary>
    /// Whether the object should cast a static shadow from shadow casting lights.
    /// Also requires Cast Shadow to be set to True.
    /// </summary>
    [UProperty] public bool bCastStaticShadow = true;
    /// <summary>
    /// If true, the primitive will only shadow itself and will not cast a shadow on other primitives.
    /// This can be used as an optimization when the shadow on other primitives won't be noticeable.
    /// </summary>
    [UProperty] public bool bSelfShadowOnly;
    /// <summary>
    /// For mobile platforms only! If true, the primitive will not receive projected mod shadows, not from itself nor any other mod shadow caster.
    /// This can be used to avoid self-shadowing artifacts.
    /// </summary>
    [UProperty] public bool bNoModSelfShadow;
    /// <summary>
    /// Optimization for objects which don't need to receive dominant light shadows.
    /// This is useful for objects which eat up a lot of GPU time and are heavily texture bound yet never receive noticeable shadows from dominant lights like trees.
    /// </summary>
    [UProperty] public bool bAcceptsDynamicDominantLightShadows = true;
    /// <summary>
    /// Controls whether the primitive should cast shadows when hidden.
    /// This flag is only used if CastShadow is TRUE.
    /// </summary>
    [UProperty] public bool bCastHiddenShadow;
    /// <summary>
    /// Whether this primitive should cast dynamic shadows as if it were a two sided material.
    /// </summary>
    [UProperty] public bool bCastShadowAsTwoSided;

    /// <summary>
    /// Controls whether the primitive accepts any lights.
    /// Should be set to FALSE for e.g. unlit objects as it's a nice optimization - especially for larger objects.
    /// </summary>
    [UProperty] public bool bAcceptsLights;
    /// <summary>
    /// Controls whether the object should be affected by dynamic lights.
    /// </summary>
    [UProperty] public bool bAcceptsDynamicLights = true;

    /// <summary>
    /// If TRUE, dynamically lit translucency on this primitive will render in one pass,
    /// Which is cheaper and ensures correct blending but approximates lighting using one directional light and all other lights in an unshadowed SH environment.
    /// If FALSE, dynamically lit translucency will render in multiple passes which uses more shader instructions and results in incorrect blending.
    /// </summary>
    [UProperty] public bool bUseOnePassLightingOnTranslucency;

    /// <summary>
    /// Whether to allow the primitive to use precomputed shadows or lighting.
    /// </summary>
    [UProperty] public bool bUsePrecomputedShadows;

    [UProperty] public bool bAllowAmbientOcclusion = true; // Deprecated? When?

    #endregion

    #region Collision

    [UProperty] public bool CollideActors;

    /// <summary>
    /// When TRUE, this primitive component get collision tests even if it isn't the actor's collision component.
    /// </summary>
    [UProperty] public bool AlwaysCheckCollision;

    [UProperty] public bool BlockActors;
    [UProperty] public bool BlockZeroExtent;
    [UProperty] public bool BlockNonZeroExtent;
    [UProperty] public bool CanBlockCamera = true;
    [UProperty] public bool BlockRigidBody;
    [UProperty] public bool bBlockFootPlacement = true;

    /// <summary>Never create any physics engine representation for this body.</summary>
    [UProperty] public bool bDisableAllRigidBody;

    /// <summary>
    /// When creating rigid body, will skip normal geometry creation step, and will rely on ModifyNxActorDesc to fill in geometry.
    /// </summary>
    [UProperty] public bool bSkipRBGeomCreation;

    /// <summary>
    /// Flag that indicates if OnRigidBodyCollision function should be called for physics collisions involving this PrimitiveComponent.
    /// </summary>
    [UProperty] public bool bNotifyRigidBodyCollision;

    #endregion

    #region Novodex fluids

    /// <summary>
    /// Whether this object should act as a 'drain' for fluid, and destroy fluid particles when they contact it.
    /// </summary>
    [UProperty] public bool bFluidDrain;
    /// <summary>
    /// Indicates that fluid interaction with this object should be 'two-way' - that is, force should be applied to both fluid and object.
    /// </summary>
    [UProperty] public bool bFluidTwoWay;

    /// <summary>
    /// Will ignore radial impulses applied to this component.
    /// </summary>
    [UProperty] public bool bIgnoreRadialImpulse;
    /// <summary>
    /// Will ignore radial forces applied to this component.
    /// </summary>
    [UProperty] public bool bIgnoreRadialForce;
    /// <summary>
    /// Will ignore force field applied to this component.
    /// </summary>
    [UProperty] public bool bIgnoreForceField;
    /// <summary>
    /// Place into a NxCompartment that will run in parallel with the primary scene's physics with potentially different simulation parameters.
    /// If double buffering is enabled in the WorldInfo then physics will run in parallel with the entire game for this component.
    /// </summary>
    [UProperty] public bool bUseCompartment;

    #endregion

    #region General

    /// <summary>
    /// If this is True, this component must always be loaded on clients, even if HiddenGame && !CollideActors.
    /// </summary>
    [UProperty] public bool AlwaysLoadOnClient = true;
    /// <summary>
    /// If this is True, this component must always be loaded on servers, even if !CollideActors.
    /// </summary>
    [UProperty] public bool AlwaysLoadOnServer = true;

    /// <summary>
    /// Allow certain components to render even if the parent actor is part of the camera's HiddenActors array.
    /// </summary>
    [UProperty] public bool bIgnoreHiddenActorsMembership;

    [UProperty] public bool AbsoluteTranslation, AbsoluteRotation, AbsoluteScale;

    /// <summary>
    /// Determines whether or not we allow shadowing fading.<br/>
    /// Some objects (especially in cinematics) having the shadow fade/pop out looks really bad.
    /// </summary>
    [UProperty] public bool bAllowShadowFade = true;

    /// <summary>Whether or not this primitive type is supported on mobile.</summary>
    /// <remarks>For the emulate mobile rendering editor feature.</remarks>
    [UProperty] public bool bSupportedOnMobile = true;

    #endregion

    /// <summary>
    /// Translucent objects with a lower sort priority draw behind objects with a higher priority.<br/>
    /// Translucent objects with the same priority are rendered from back-to-front based on their bounds origin.
    /// </summary>
    [UProperty] public int TranslucencySortPriority;

    /// <summary>
    /// Used for precomputed visibility.
    /// </summary>
    [UProperty] public int VisibilityId = -1;

    /// <summary>
    /// Lighting channels controlling light/ primitive interaction. Only allows interaction if at least one channel is shared.
    /// </summary>
    [UProperty] public LightingChannelContainer LightingChannels = new();

    /// <summary>
    /// Types of objects that this physics objects will collide with.
    /// </summary>
    [UProperty] public RBCollisionChannelContainer RBCollideWithChannels;

    [UProperty] public Vector Translation;
    [UProperty] public Rotator Rotation;
    [UProperty] public float Scale = 1.0f;
    [UProperty] public Vector Scale3D = new(1.0f);
    [UProperty] public float BoundsScale = 1;

    #endregion
}
