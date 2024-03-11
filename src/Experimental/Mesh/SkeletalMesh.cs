using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UnrealLib.Experimental.Mesh;

public class USkeletalMesh(FObjectExport? export = null) : UObject(export)
{
    public FBoxSphereBounds Bounds;
    public /* UMaterialInterface* */ int[] Materials;
    // public /* UApexClothingAsset* */ int[] ClothingAssets;       // Infinity Blade _probably_ doesn't use Apex clothing. Implement this only if needed
    // public FApexClothingAssetInfo[] ClothingLodMap;              // "" ditto
    public Vector Origin;
    public Rotator RotOrigin;
    public FMeshBone[] RefSkeleton;
    public int SkeletalDepth;
    public NameToIndex[] NameIndexMap;
    public FStaticLODModel[] LODModels;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref Bounds);
        Ar.Serialize(ref Materials);
        Ar.Serialize(ref Origin);
        Ar.Serialize(ref RotOrigin);
        Ar.Serialize(ref RefSkeleton);
        Ar.Serialize(ref SkeletalDepth);
        Ar.Serialize(ref LODModels);

        Ar.Serialize(ref NameIndexMap);


        // Serialize LODModels
    }
}

public readonly record struct FBoxSphereBounds
{
    public readonly Vector Origin, BoxExtent;
    public readonly float SphereRadius;
}

public class FMeshBone : ISerializable
{
    public FName Name;
    public int Flags;
    public VJointPos BonePos;
    public int NumChildren;
    public int ParentIndex;
    public Color BoneColor;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Name);
        Ar.Serialize(ref Flags);
        Ar.Serialize(ref BonePos);
        Ar.Serialize(ref ParentIndex);
        Ar.Serialize(ref NumChildren);
        Ar.Serialize(ref BoneColor);
    }
}

public readonly record struct VJointPos
{
    public readonly FVector4 Orientation;
    public readonly Vector Position;
}

public class NameToIndex : ISerializable
{
    public FName Name;
    public int Index;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Name);
        Ar.Serialize(ref Index);
    }
}

#region FStaticLODModel

public class FStaticLODModel : ISerializable
{
    public FSkelMeshSection[] Sections;
    public FSkelMeshChunk[] Chunks;
    public short[] ActiveBoneIndices;
    public byte[] RequiredBones;

    public FMultiSizeIndexContainer MultiSizeIndexContainer;

    public uint Size, NumVertices, NumTexCoords;

    public FUntypedBulkData RawPointIndices;
    public FSkeletalMeshVertexBuffer VertexBufferGPUSkin;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Sections);
        Ar.Serialize(ref MultiSizeIndexContainer);
        Ar.Serialize(ref ActiveBoneIndices);
        Ar.Serialize(ref Chunks);
        Ar.Serialize(ref Size);
        Ar.Serialize(ref NumVertices);
        Ar.Serialize(ref RequiredBones);

        if (Ar.Version < 806)
        {
            // Do some legacy stuff
            throw new NotImplementedException();
        }
        else
        {
            Ar.Serialize(ref RawPointIndices);
        }

        Ar.Serialize(ref NumTexCoords);
        Ar.Serialize(ref VertexBufferGPUSkin);

        throw new NotImplementedException();

        // ...
    }
}

public class FSkelMeshSection : ISerializable
{
    public short MaterialIndex, ChunkIndex;
    public int BaseIndex;
    public int NumTriangles;
    public byte TriangleSorting;
    public byte bSelected;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref MaterialIndex);
        Ar.Serialize(ref ChunkIndex);

        if (Ar.Version < 806)
        {
            // IB1 stores NumTriangles as a short
            short numTriangles = (short)NumTriangles;
            Ar.Serialize(ref numTriangles);

            NumTriangles = numTriangles;
        }
        else
        {
            Ar.Serialize(ref NumTriangles);
        }

        Ar.Serialize(ref TriangleSorting);
        Ar.Serialize(ref bSelected);
    }
}

public class FSkelMeshChunk : ISerializable
{
    public uint BaseVertexIndex;
    public FRigidSkinVertex[] RigidVertices;
    public FSoftSkinVertex[] SoftVertices;
    public short[] BoneMap;
    public int NumRigidVertices;
    public int NumSoftVertices;
    public int MaxBoneInfluences;// = 4;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref BaseVertexIndex);
        Ar.Serialize(ref RigidVertices);
        Ar.Serialize(ref SoftVertices);
        Ar.Serialize(ref BoneMap);
        Ar.Serialize(ref NumRigidVertices);
        Ar.Serialize(ref NumSoftVertices);
        Ar.Serialize(ref MaxBoneInfluences);
    }
}

public abstract class FSkinVertexBase : ISerializable
{
    public Vector Position;
    public FPackedNormal TangentX, TangentY, TangentZ;
    public FVector2[] UVs;
    public Color Color;

    public virtual void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Position);
        Ar.Serialize(ref TangentX);
        Ar.Serialize(ref TangentY);
        Ar.Serialize(ref TangentZ);
        Ar.Serialize(ref UVs, 4);
        Ar.Serialize(ref Color);
    }
}

public class FRigidSkinVertex : FSkinVertexBase
{
    public byte Bone;

    public void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref Bone);
    }
}

public class FSoftSkinVertex : FSkinVertexBase
{
    public byte[] InfluenceBones;
    public byte[] InfluenceWeights;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref InfluenceBones, 4);
        Ar.Serialize(ref InfluenceWeights, 4);
    }
}

public class FMultiSizeIndexContainer : ISerializable
{
    public bool NeedsCPUAccess;
    public byte DataTypeSize;
    public FRawStaticIndexBuffer IndexBuffer;

    public void Serialize(UnrealArchive Ar)
    {
        if (Ar.Version < 806)
        {
            NeedsCPUAccess = true;
            DataTypeSize = sizeof(short);
        }
        else
        {
            Ar.Serialize(ref NeedsCPUAccess);
            Ar.Serialize(ref DataTypeSize);
        }

        if (Ar.IsLoading)
        {
            IndexBuffer = new(DataTypeSize == sizeof(short));
        }

        Debug.Assert(IndexBuffer is not null);

        Ar.Serialize(ref IndexBuffer);
    }
}

public class FRawStaticIndexBuffer : ISerializable
{
    public int[] Indices32;
    public short[] Indices16;
    public bool Is16Bit;

    public FRawStaticIndexBuffer() { }

    public FRawStaticIndexBuffer(bool is16Bit)
    {
        Is16Bit = is16Bit;
    }

    public void Serialize(UnrealArchive Ar)
    {
        if (Is16Bit)
        {
            Ar.Serialize(ref Indices16);
        }
        else
        {
            Ar.Serialize(ref Indices32);
        }
    }
}

public class FSkeletalMeshVertexBuffer : ISerializable
{
    public int Stride, NumVertices;
    public byte[] VertexData;

    public uint NumTexCoords;
    public bool bUsePackedPosition;
    public Vector MeshExtesnion, MeshOrigin;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref NumTexCoords);

        Ar.Serialize(ref bUsePackedPosition);
        Ar.Serialize(ref MeshExtesnion);
        Ar.Serialize(ref MeshOrigin);

        Debug.Assert(false, "Step through this manually. Ensure work");

        Ar.Serialize(ref Stride);
        Ar.Serialize(ref NumVertices);

        Ar.BulkSerialize(ref VertexData);
    }
}

#endregion