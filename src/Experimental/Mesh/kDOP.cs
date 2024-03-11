using System;
using System.Diagnostics;
using UnrealLib.Interfaces;

namespace UnrealLib.Experimental.Mesh;

// Fake class, just enough to skip past this data
public class TkDOPTree : ISerializable
{
    public RootBound RootBound;
    public Node[] Nodes;
    public Triangle[] Triangles;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref RootBound);
        Ar.BulkSerialize(ref Nodes);
        Ar.BulkSerialize(ref Triangles);
    }
}

public struct RootBound : ISerializable
{
    public float[] Min;
    public float[] Max;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Min, 3);
        Ar.Serialize(ref Max, 3);
    }
}

// These are fake placeholder structs to mimic the sizes of their real counterparts

[System.Runtime.CompilerServices.InlineArray(6)]
public struct Node
{
    private byte element;
}

[System.Runtime.CompilerServices.InlineArray(8)]
public struct Triangle
{
    private byte element;
}
