using UnrealLib.Core;
using UnrealLib.Enums;
using UnrealLib.Experimental.UnObj;

namespace UnrealLib.Experimental.Component;

public class UComponent(FObjectExport export) : UObject(export)
{
    public UObject TemplateOwnerClass;
    public FName TemplateName;
}

public class ActorComponent(FObjectExport export) : UComponent(export)
{
    // public Actor Owner;
    //public bool bAttached;
    //public bool bTickInEditor;

    //public ETickingGroup TickGroup;
}

public class PrimitiveComponent(FObjectExport export) : ActorComponent(export)
{
    #region Properties
    // public LightEnvironment LightEnvironment;

    /// <summary>
    /// The minimum distance at which the primitive should be rendered, measured in world
    /// space units from the center of the primitive's bounding sphere to the camera position.
    /// </summary>
    public float MinDrawDistance;

    /// <summary>
    /// The distance at which the renderer will switch from parent (low LOD) to children (high LOD).
    /// This is basically the same as MinDrawDistance, except that the low LOD will draw even up close, if there are no children.
    /// This is needed so the high lod meshes can be in a streamable sublevel, and if streamed out, the low LOD will draw up close.
    /// </summary>
    public float MassiveLODDistance;

    /// <summary>
    /// Max draw distance exposed to LDs. The real max draw distance is the min (disregarding 0) of this and volumes affecting this object.
    /// </summary>
    private float MaxDrawDistance;

    /// <summary>
    /// The distance to cull this primitive at.
    /// A CachedMaxDrawDistance of 0 indicates that the primitive should not be culled by distance.
    /// </summary>
    private float CachedMaxDrawDistance;

    /// <summary>
    ///  Scalar controlling the amount of motion blur to be applied when object moves.
    ///  0 == none, 1 == full instance motion blur (default). Value should be 0 or bigger.
    /// </summary>
    public float MotionBlurInstanceScale;

    /// <summary>
    /// The scene depth priority group to draw the primitive in.
    /// </summary>
    public ESceneDepthPriorityGroup DepthPriorityGroup;
    /// <summary>
    /// The scene depth priority group to draw the primitive in, if it's being viewed by its owner.
    /// </summary>
    public ESceneDepthPriorityGroup ViewOwnerDepthPriorityGroup;
    /// <summary>
    /// If detail mode is > system detail mode, primitive won't be rendered.
    /// </summary>
    public EDetailMode DetailMode;

    /// <summary>
    /// Enum indicating different type of objects for rigid-body collision purposes.
    /// </summary>
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
    };

    /// <summary>
    /// Enum indicating what type of object this should be considered for rigid body collision.
    /// </summary>
    public ERBCollisionChannel RBChannel;

    // got bored. MANY more properties...
    #endregion
}

public class CylinderComponent(FObjectExport export) : PrimitiveComponent(export)
{
    public float CollisionHeight;
    public float CollisionRadius;

    // public Color CylinderColor; // Probably don't use C# built-in...

    public bool bDrawBoundingBox;
    public bool bDrawNonColliding;
    public bool bAlwaysRenderIfSelected;
}
