//using System.Numerics;
//using UnrealLib.Experimental.UnObj;

//namespace UnrealLib.Experimental.Mesh;

///// <summary>
///// Describes the collision geometry used by the rigid body physics. This is a collection of
///// primitives, each with a transformation matrix from the mesh origin.
///// </summary>
//public class FKAggregateGeom
//{
//    public FKSphereElem[] SphereElems;
//    public FKBoxElem[] BoxElems;
//    public FKSphylElem[] SphylElems;
//    public FKConvexElem[] ConvexElems;
//    public FKConvexGeomRenderInfo* RenderInfo;
//    public bool bSkipCloseAndParallelChecks = true;
//}

//public class UKMeshProps : UObject
//{
//    public Vector3 COMNudge;
//    public FKAggregateGeom AggGeom;
//}