using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealLib.Core;
using UnrealLib.Experimental.FBX;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Interfaces;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Mesh;

public partial class UStaticMesh(FObjectExport? export = null) : UObject(export)
{
    #region Properties

    [UProperty] public int BodySetup;  // object index
    [UProperty] public string SourceFileTimeStamp;
    [UProperty] public string SourceFilePath;
    [UProperty] public int LightMapCoordinateIndex;
    [UProperty] public int LightMapResolution;
    [UProperty] public float LODDistanceRatio;

    #endregion

    public FBoxSphereBounds Bounds;
    public TkDOPTree kDOPTree;

    /// <summary>
    /// The original raw triangles and generated render data.
    /// </summary>
    public FStaticMeshSourceData SourceData;

    /// <summary>
    /// Optimization settings used to simplify mesh LODs.
    /// </summary>
    public FStaticMeshOptimizationSettings[] OptimizationSettings;

    /// <summary>
    /// True if this mesh has been simplified.
    /// </summary>
    public bool bHasBeenSimplified;

    /// <summary>
    /// True if this mesh is a proxy.
    /// </summary>
    public bool bIsMeshProxy;

    /// <summary>
    /// Array of LODs, holding their associated rendering and collision data.
    /// </summary>
    public FStaticMeshRenderData[] LODModels;

    /// <summary>
    /// Per-LOD information exposed to the editor.
    /// </summary>
    public FStaticMeshLODElement[] LODInfo;

    public Rotator ThumbnailAngle;
    public float ThumbnailDistance;

    /// <summary>
    /// For simplified meshes, this is the fully qualified path and name of the static mesh object we were 
    /// originally duplicated from. This is serialized to disk, but is discarded when cooking for consoles.
    /// </summary>
    public string HighResSourceMeshName;

    /// <summary>
    /// For simplified meshes, this is the CRC of the high res mesh we were originally duplicated from.
    /// </summary>
    public uint HighResSourceMeshCRC;

    /// <summary>
    /// Unique ID for tracking/caching this mesh during distributed lighting.
    /// </summary>
    public FGuid LightingGuid;

    /// <summary>
    /// Incremented any time a change in the static mesh causes vertices to change position, such as a reimport.
    /// </summary>
    public int VertexPositionVersionNumber;

    /// <summary>
    /// The cached streaming texture factors.<br/>
    /// If the array doesn't have MAX_TEXCOORDS entries in it, the cache is outdated.
    /// </summary>
    public float[] CachedStreamingTextureFactors;

    /// <summary>
    /// If true during a rebuild, we will remove degenerate triangles. Otherwise they will be kept.
    /// </summary>
    public bool bRemoveDegenerates;

    /// <summary>
    /// If true, InstancedStaticMeshComponents will build static lighting for each LOD rather than all LODs sharing the top level LOD's lightmaps.
    /// </summary>
    public bool bPerLODStaticLightingForInstancing;

    /// <summary>
    /// Hint of the expected instance count for consoles to pre-allocate the duplicated index buffer.
    /// </summary>
    public int ConsolePreallocateInstanceCount;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref Bounds);
        Ar.Serialize(ref BodySetup);
        Ar.Serialize(ref kDOPTree);

        int version = 18;
        Ar.Serialize(ref version);
        Debug.Assert(version == 18);

        if (Ar.Version > 823)
        {
            Ar.Serialize(ref SourceData);

            if (Ar.Version > 829)
            {
                Ar.Serialize(ref OptimizationSettings);
            }
            else if (Ar.IsLoading)
            {
                throw new NotImplementedException("IB1 SM support not yet implemented");
                float[] maxDeviations = default;
                Ar.Serialize(ref maxDeviations);

                // Some stuff to load optimization settings from older versions @TODO
            }

            Ar.Serialize(ref bHasBeenSimplified); Ar.Position += 3;   // UBool == 4 bytes
        }

        if (Ar.Version > 859)
        {
            Ar.Serialize(ref bIsMeshProxy); Ar.Position += 3;   // UBool == 4 bytes
        }

        Ar.Serialize(ref LODModels);
        Ar.Serialize(ref LODInfo);
        Ar.Serialize(ref ThumbnailAngle);
        Ar.Serialize(ref ThumbnailDistance);

        Ar.Serialize(ref HighResSourceMeshName);
        Ar.Serialize(ref HighResSourceMeshCRC);

        Ar.Serialize(ref LightingGuid);

        if (Ar.Version > 801)
        {
            Ar.Serialize(ref VertexPositionVersionNumber);
        }

        if (Ar.Version > 797)
        {
            Ar.Serialize(ref CachedStreamingTextureFactors);
        }

        if (Ar.Version > 804)
        {
            Ar.Serialize(ref bRemoveDegenerates); Ar.Position += 3;
        }
        else
        {
            bRemoveDegenerates = true;
        }

        if (Ar.Version > 848)
        {
            Ar.Serialize(ref bPerLODStaticLightingForInstancing); Ar.Position += 3;
            Ar.Serialize(ref ConsolePreallocateInstanceCount);
        }
    }

    public void ExportOBJ(string path)
    {
        using var sw = new StreamWriter(path);
        var model = LODModels[0];

        // Write out vertices
        foreach (var vertex in model.PositionVertexBuffer.VertexData)
        {
            sw.WriteLine($"v {vertex.X} {vertex.Y} {vertex.Z}");
        }

        model.VertexBuffer.WriteVertexNormalsToObj(sw);
        model.VertexBuffer.WriteTextureCoordinatesToObj(sw);

        // Write out faces
        for (int i = 0; i < model.IndexBuffer.Length; i += 3)
        {
            var a = model.IndexBuffer[i + 2] + 1;
            var b = model.IndexBuffer[i + 1] + 1;
            var c = model.IndexBuffer[i] + 1;

            // Vertex / tex coord / vertex normal
            sw.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
        }
    }

    public void ExportFBX()
    {
        var model = LODModels[0];

        // Why does FBX require this to be stored as a double[]??
        double[] fbxVertices = new double[model.PositionVertexBuffer.NumVertices * 3];

        int fbxIndex = 0;
        foreach (var ue3Vertex in model.PositionVertexBuffer.VertexData)
        {
            fbxVertices[fbxIndex++] = ue3Vertex.X;
            fbxVertices[fbxIndex++] = ue3Vertex.Y;
            fbxVertices[fbxIndex++] = ue3Vertex.Z;
        }

        // The last vertex index is negated using bitwise ~ operator.
        // This is used to indicate the last vertex in a face, since FBX does not enforce triangles
        int[] polygonVertexIndices = new int[model.PositionVertexBuffer.NumVertices];
        for (int i = 0; i < model.IndexBuffer.Length; i += 3)
        {
            polygonVertexIndices[i] = model.IndexBuffer[i];
            polygonVertexIndices[i + 1] = model.IndexBuffer[i + 1];
            polygonVertexIndices[i + 2] = ~model.IndexBuffer[i + 2];    // mark negative to indicate final vertex of face
        }

        var fbx = new FbxDocument();
        fbx.CreateNew(); // @TODO move this into static ctor

        // @TODO abstract this away into a method in the fbx folder
        FbxNode geometry = new()
        {
            Name = "Geometry",
            Properties = [ new(152167664), new($"{ObjectName}\0\x1Geometry"), new("Mesh") ],
            Children =
            [
                new()
                {
                    Name = "Vertices",
                    Properties = [new(fbxVertices) ]
                },
                new()
                {
                    Name = "PolygonVertexIndex",
                    Properties = [new(polygonVertexIndices)]
                }
            ]
        };
    }
}

#region Vertex buffer classes

// @TODO these vertex buffer classes seem to have mostly identical fields. Perhaps make a base class, or consolidate

/// <summary>
/// A vertex buffer of positions.
/// </summary>
public class FPositionVertexBuffer : ISerializable// : FVertexBuffer
{
    /// <summary>
    /// The vertex data storage type.
    /// </summary>
    public Core.Vector[] VertexData;

    /// <summary>
    /// The number of texcoords/vertex in the buffer.
    /// </summary>
    public uint NumTexCoords;

    /// <summary>
    /// The cached vertex data pointer.
    /// </summary>
    public byte[] Data;

    /// <summary>
    /// The cached vertex stride.
    /// </summary>
    public uint Stride;

    /// <summary>
    /// The cached number of vertices.
    /// </summary>
    public uint NumVertices;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Stride);
        Ar.Serialize(ref NumVertices);
        Ar.BulkSerialize(ref VertexData);

        // Allocate VertexData buffer if bNeedsCpuAccess
        // Serialize VertexData
    }
}

/// <summary>
/// Vertex buffer for a static mesh LOD.
/// </summary>
public class FStaticMeshVertexBuffer : ISerializable// : public FVertexBuffer
{
    /// <summary>
    /// The vertex data storage type.
    /// </summary>
    public FStaticMeshFullVertex[] VertexData2;
    public byte[] VertexData;   // Unfortunately have to store as raw bytes due to C# generic limitations

    /// <summary>
    /// The number of texcoords/vertex in the buffer.
    /// </summary>
    public uint NumTexCoords;

    /// <summary>
    /// The cached vertex data pointer.
    /// </summary>
    public byte[] Data;

    /// <summary>
    /// The cached vertex stride.
    /// </summary>
    public uint Stride;

    /// <summary>
    /// The cached number of vertices.
    /// </summary>
    public uint NumVertices;

    /// <summary>
    /// Corresponds to UStaticMesh::UseFullPrecisionUVs. If TRUE, then 32-bit UVs are used.
    /// </summary>
    public bool bUseFullPrecisionUVs;

    public int ArrayCount;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref NumTexCoords);
        Ar.Serialize(ref Stride);
        Ar.Serialize(ref NumVertices);
        Ar.Serialize(ref bUseFullPrecisionUVs); Ar.Position += 3;

        // Inline SerializeBulkData() to cheat with this particular type

        // int elementSize = (int)Stride;
        // Ar.Serialize(ref elementSize);
        Ar.Serialize(ref Stride);

        // int arrayCount = Ar.IsLoading ? default : (VertexData.Length / (int)Stride);    // need to do this as VertexData for this class is not implemented correctly
        // Ar.Serialize(ref arrayCount);

        ArrayCount = Ar.IsLoading ? default : VertexData.Length / (int)Stride;   // Required as VertexData implemented as byte array
        Ar.Serialize(ref ArrayCount);

        if (Ar.IsLoading)
        {
            //VertexData2 = NumTexCoords switch
            //{
            //    1 when bUseFullPrecisionUVs => new TStaticMeshFullVertexFloat32UVs_1[arrayCount],
            //    2 when bUseFullPrecisionUVs => new TStaticMeshFullVertexFloat32UVs_2[arrayCount],
            //    3 when bUseFullPrecisionUVs => new TStaticMeshFullVertexFloat32UVs_3[arrayCount],
            //    4 when bUseFullPrecisionUVs => new TStaticMeshFullVertexFloat32UVs_4[arrayCount],
            //    1 => new TStaticMeshFullVertexFloat16UVs_1[arrayCount],
            //    2 => new TStaticMeshFullVertexFloat16UVs_2[arrayCount],
            //    3 => new TStaticMeshFullVertexFloat16UVs_3[arrayCount],
            //    4 => new TStaticMeshFullVertexFloat16UVs_4[arrayCount]
            //};

            // Read in bulk data. Cast on use
            VertexData = new byte[ArrayCount * Stride];
            Ar.ReadExactly(VertexData);
        }
        else
        {
            Ar.Write(VertexData);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly record struct TStaticMeshFullVertexFloat16
    {
        public readonly FPackedNormal TangentX, TangentZ;
        public readonly FVector2Half UV0, UV1, UV2, UV3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly record struct TStaticMeshFullVertexFloat32
    {
        public readonly FPackedNormal TangentX, TangentZ;
        public readonly FVector2 UV0, UV1, UV2, UV3;
    }

    public unsafe void WriteVertexNormalsToObj(StreamWriter sw)
    {
        var ms = new MemoryStream(VertexData, false);

        TStaticMeshFullVertexFloat32 vertex = default;
        var span = new Span<byte>(&vertex, (int)Stride);

        while (ms.Read(span) != 0)
        {
            // Unpack z tangent
            var normal = vertex.TangentZ.Unpack();

            // Write to OBJ
            sw.WriteLine($"vn {normal.X} {normal.Y} {normal.Z}");
        }
    }

    public unsafe void WriteTextureCoordinatesToObj(StreamWriter sw)
    {
        var ms = new MemoryStream(VertexData, false);

        if (bUseFullPrecisionUVs)
        {
            TStaticMeshFullVertexFloat32 vertex = default;
            var span = new Span<byte>(&vertex, (int)Stride);

            while (ms.Read(span) != 0)
            {
                // OBJ only supports one channel, so only use the first one?

                sw.WriteLine(NumTexCoords switch
                {
                    1 => $"vt {vertex.UV0.X} {vertex.UV0.Y}",
                    2 => $"vt {vertex.UV1.X} {vertex.UV1.Y}",
                    3 => $"vt {vertex.UV2.X} {vertex.UV2.Y}",
                    4 => $"vt {vertex.UV3.X} {vertex.UV3.Y}"
                });
            }
        }
        else
        {
            TStaticMeshFullVertexFloat16 vertex = default;
            var span = new Span<byte>(&vertex, (int)Stride);

            while (ms.Read(span) != 0)
            {
                // OBJ only supports one channel, so only use the first one?

                sw.WriteLine(NumTexCoords switch
                {
                    1 => $"vt {vertex.UV0.X} {vertex.UV0.Y}",
                    2 => $"vt {vertex.UV1.X} {vertex.UV1.Y}",
                    3 => $"vt {vertex.UV2.X} {vertex.UV2.Y}",
                    4 => $"vt {vertex.UV3.X} {vertex.UV3.Y}"
                });
            }
        }
    }
}

/// <summary>
/// A vertex buffer of colors.
/// Needs to match FColorVertexBuffer_Mirror in UnrealScript.
/// </summary>
public class FColorVertexBuffer : ISerializable //: public FVertexBuffer
{
    /// <summary>
    /// The vertex data storage type.
    /// </summary>
    public Color[] VertexData;    // 12 bytes?

    /// <summary>
    /// The number of texcoords/vertex in the buffer.
    /// </summary>
    public uint NumTexCoords;

    /// <summary>
    /// The cached vertex data pointer.
    /// </summary>
    public byte[] Data;

    /// <summary>
    /// The cached vertex stride.
    /// </summary>
    public uint Stride;

    /// <summary>
    /// The cached number of vertices.
    /// </summary>
    public uint NumVertices;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Stride);
        Ar.Serialize(ref NumVertices);

        if (NumVertices > 0)
        {
            Ar.BulkSerialize(ref VertexData);
        }
    }
}

#endregion

public class FStaticMeshRenderData : ISerializable
{
    public FUntypedBulkData RawTriangles;
    public FStaticMeshElement[] Elements;

    /// <summary>The buffer containing the position vertex data.</summary>
    public FPositionVertexBuffer PositionVertexBuffer;
    /// <summary>The buffer containing vertex data.</summary>
    public FStaticMeshVertexBuffer VertexBuffer;
    /// <summary>The buffer containing the vertex color data.</summary>
    public FColorVertexBuffer ColorVertexBuffer;

    /// <summary>
    /// The number of vertices in the LOD.
    /// </summary>
    public uint NumVertices;

    /** Index buffer resource for rendering */
    public short[] IndexBuffer;
    /** Index buffer resource for rendering wireframe mode */
    public short[] WireframeIndexBuffer;
    /** Resources neede to render the model with PN-AEN */
    public short[] AdjacencyIndexBuffer;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref RawTriangles);
        Ar.Serialize(ref Elements);

        Ar.Serialize(ref PositionVertexBuffer);
        Ar.Serialize(ref VertexBuffer);

        if (Ar.Version < 842)
        {
            // Save the position in the archive and see what the stride and count of the color buffer will be.
            int ArPos = (int)Ar.Position;

            uint ExpectedVertCount = VertexBuffer.NumVertices;
            int ColorBufferVertCount = 0;
            int ColorBufferStride = 0;

            Ar.Serialize(ref ColorBufferStride);
            Ar.Serialize(ref ColorBufferVertCount);

            // Speculatively read the stride of the bulk vertex color data.
            int ColorBulkDataStride = 0;
            Ar.Serialize(ref ColorBulkDataStride);
            Ar.Position -= sizeof(int);

            if (ColorBufferVertCount == ExpectedVertCount || (ColorBufferVertCount > 0 && ColorBulkDataStride == ColorBufferStride))
            {
                // The count is what we expect it to be, or at least it looks like the data has been serialized.
                // There is one corner case:
                //   ExpectedVertCount == 4 && ColorBufferVertCount > 0 && ColorBufferVertCount != 4 && ColorBufferStride == 4 && this->NumVertices == 4
                // In this case, it could be a broken buffer but we can't tell for sure so do the conservative thing: try serializing it.
                Ar.Position = ArPos;
                ColorVertexBuffer.Serialize(Ar);
            }
        }
        else
        {
            Ar.Serialize(ref ColorVertexBuffer);
        }

        Ar.Serialize(ref NumVertices);

        Ar.BulkSerialize(ref IndexBuffer);
        Ar.BulkSerialize(ref WireframeIndexBuffer);

        if (Ar.Version > 841)
        {
            Ar.BulkSerialize(ref AdjacencyIndexBuffer);
        }
    }
}

public class FStaticMeshSourceData : ISerializable
{
    // public FStaticMeshRenderData RenderData;

    public void Serialize(UnrealArchive Ar)
    {
        bool hasSourceData = false;
        Ar.Serialize(ref hasSourceData);
        Ar.Position += 3;   // UBool == 4 bytes

        Debug.Assert(!hasSourceData);
        if (hasSourceData)
        {
            throw new NotImplementedException();
            //Ar.Serialize(ref RenderData);
        }
    }
}

/// <summary>
/// The settings used to optimize a static mesh LOD.
/// </summary>
public record class FStaticMeshOptimizationSettings : ISerializable
{
    public enum ENormalMode : byte
    {
        NM_PreserveSmoothingGroups,
        NM_RecalculateNormals,
        NM_RecalculateNormalsSmooth,
        NM_RecalculateNormalsHard,
        NM_Max
    };

    public enum EImportanceLevel : byte
    {
        IL_Off,
        IL_Lowest,
        IL_Low,
        IL_Normal,
        IL_High,
        IL_Highest,
        IL_Max
    };

    /// <summary>
    /// Enum specifying the reduction type to use when simplifying static meshes.
    /// </summary>
    public enum EOptimizationType : byte
    {
        OT_NumOfTriangles,
        OT_MaxDeviation,
        OT_MAX,
    };

    /// <summary>
    /// The method to use when optimizing the skeletal mesh LOD.
    /// </summary>
    public EOptimizationType ReductionMethod = EOptimizationType.OT_MaxDeviation;
    /// <summary>
    /// If ReductionMethod equals SMOT_NumOfTriangles, this value is the ratio of triangles [0-1] to remove from the mesh.
    /// </summary>
    public float NumOfTrianglesPercentage = 1.0f;
    /// <summary>
    /// If ReductionMethod equals SMOT_MaxDeviation, this value is the maximum deviation from the base mesh as a percentage of the bounding sphere.
    /// </summary>
    public float MaxDeviationPercentage = 0.0f;
    /// <summary>
    /// The welding threshold distance. Vertices under this distance will be welded.
    /// </summary>
    public float WeldingThreshold = 0.1f;
    /// <summary>
    /// Whether Normal smoothing groups should be preserved. If false then NormalsThreshold is used.
    /// </summary>
    public bool bRecalcNormals = true;
    /// <summary>
    /// If the angle between two triangles are above this value, the normals will not be smooth over the edge between those two triangles. Set in degrees.
    /// This is only used when PreserveNormals is set to false.
    /// </summary>
    public float NormalsThreshold = 60.0f;
    /// <summary>
    /// How important the shape of the geometry is (EImportanceLevel).
    /// </summary>
    public EImportanceLevel SilhouetteImportance = EImportanceLevel.IL_Normal;
    /// <summary>
    /// How important texture density is (EImportanceLevel).
    /// </summary>
    public EImportanceLevel TextureImportance = EImportanceLevel.IL_Normal;
    /// <summary>
    /// How important shading quality is.
    /// </summary>
    public EImportanceLevel ShadingImportance = EImportanceLevel.IL_Normal;

    public FStaticMeshOptimizationSettings() { }

    public void Serialize(UnrealArchive Ar)
    {
        // @IB3
        if (Ar.Version < 863)
        {
            Ar.Serialize(ref MaxDeviationPercentage);

            // Remap Importance Settings
            Ar.Serialize(ref SilhouetteImportance);
            Ar.Serialize(ref TextureImportance);

            // IL_Normal was previously the first enum value. We add the new index of IL_Normal to correctly offset the old values. 
            SilhouetteImportance += (int)EImportanceLevel.IL_Normal;
            TextureImportance += (int)EImportanceLevel.IL_Normal;

            // Carry over old welding threshold values.
            WeldingThreshold = 0.008f;

            // Remap NormalMode enum value to new threshold variable.
            ENormalMode NormalMode = default;
            Ar.Serialize(ref NormalMode);

            if (NormalMode == ENormalMode.NM_PreserveSmoothingGroups)
            {
                bRecalcNormals = false;
            }
            else
            {
                bRecalcNormals = true;
                NormalsThreshold = NormalMode switch
                {
                    ENormalMode.NM_PreserveSmoothingGroups => 60.0f,
                    ENormalMode.NM_RecalculateNormalsSmooth => 80.0f,
                    ENormalMode.NM_RecalculateNormalsHard => 45.0f
                };
            }
        }
        else
        {
            Ar.Serialize(ref ReductionMethod);
            Ar.Serialize(ref MaxDeviationPercentage);
            Ar.Serialize(ref NumOfTrianglesPercentage);
            Ar.Serialize(ref SilhouetteImportance);
            Ar.Serialize(ref TextureImportance);
            Ar.Serialize(ref ShadingImportance);
            Ar.Serialize(ref bRecalcNormals);
            Ar.Serialize(ref NormalsThreshold);
            Ar.Serialize(ref WeldingThreshold);
        }
    }
}

public record class FStaticMeshElement : ISerializable
{
    public /*UMaterialInterface*/ int Material; // Object index

    public bool EnableCollision, OldEnableCollision, bEnableShadowCasting;
    public uint FirstIndex, NumTriangles, MinVertexIndex, MaxVertexIndex;

    /// <summary>
    /// The index used by a StaticMeshComponent to override this element's material.<br/>
    /// This will be the index of the element in uncooked content, but after cooking may be different from the element index due to splitting elements for platform constraints.
    /// </summary>
    public int MaterialIndex;

    /// <summary>
    /// Only required for fractured meshes.
    /// </summary>
    public FFragmentRange[] Fragments;

    public void Serialize(UnrealArchive Ar)
    {
        // UBool == 4 bytes...

        Ar.Serialize(ref Material);
        Ar.Serialize(ref EnableCollision); Ar.Position += 3;
        Ar.Serialize(ref OldEnableCollision); Ar.Position += 3;
        Ar.Serialize(ref bEnableShadowCasting); Ar.Position += 3;
        Ar.Serialize(ref FirstIndex);
        Ar.Serialize(ref NumTriangles);
        Ar.Serialize(ref MinVertexIndex);
        Ar.Serialize(ref MaxVertexIndex);
        Ar.Serialize(ref MaterialIndex);
        Ar.Serialize(ref Fragments);

        bool shouldLoadPlatformData = default;  // 1 byte
        Ar.Serialize(ref shouldLoadPlatformData);

        Debug.Assert(!shouldLoadPlatformData);
    }
}

/// <summary>
/// Identifies a single chunk of an index buffer.
/// </summary>
public readonly record struct FFragmentRange
{
    public readonly int BaseIndex;
    public readonly int NumPrimitives;
}

/////////////////////////////////////////////

// VertexDataType is of TStaticMesh VertexFloat type
public class TStaticMeshVertexData<VertexDataType> where VertexDataType : unmanaged
{
    public unsafe int GetStride() => sizeof(VertexDataType);


}

#region Struct dump

/// <summary>
/// A vertex that stores just position.
/// </summary>
public readonly record struct FPositionVertex
{
    public readonly Core.Vector Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct FPackedNormal
{
    public readonly byte X, Y, Z, W;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Core.Vector Unpack()
    {
        float x = X / 127.5f - 1.0f;
        float y = Y / 127.5f - 1.0f;
        float z = Z / 127.5f - 1.0f;

        x = Math.Abs(x) < 0.004f ? 0 : x;
        y = Math.Abs(y) < 0.004f ? 0 : y;
        z = Math.Abs(z) < 0.004f ? 0 : z;

        return new Core.Vector(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector4 Unpack4()
    {
        float x = X / 127.5f - 1.0f;
        float y = Y / 127.5f - 1.0f;
        float z = Z / 127.5f - 1.0f;
        float w = W / 127.5f - 1.0f;

        x = Math.Abs(x) < 0.004f ? 0 : x;
        y = Math.Abs(y) < 0.004f ? 0 : y;
        z = Math.Abs(z) < 0.004f ? 0 : z;
        w = Math.Abs(w) < 0.004f ? 0 : w;

        return new FVector4(x, y, z, w);
    }
}

/// <summary>
/// All information about a static-mesh vertex with a variable number of texture coordinates.
/// Position information is stored separately to reduce vertex fetch bandwidth in passes that only need position. (z prepass)
/// </summary>
public abstract class FStaticMeshFullVertex : ISerializable
{
    public FPackedNormal TangentX;
    public FPackedNormal TangentZ;

    public virtual void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref TangentX);
        Ar.Serialize(ref TangentZ);
    }
}

#region Vertex floats

/// <summary>
/// 16-bit UV version of static mesh vertex, with up to four UV channels.
/// </summary>
public sealed class TStaticMeshFullVertexFloat16UVs_1 : FStaticMeshFullVertex, ISerializable
{
    public FVector2Half UV_0;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);
        Ar.Serialize(ref UV_0);
    }
}

/// <summary>
/// 16-bit UV version of static mesh vertex, with up to four UV channels.
/// </summary>
public sealed class TStaticMeshFullVertexFloat16UVs_2 : FStaticMeshFullVertex, ISerializable
{
    public FVector2Half UV_0;
    public FVector2Half UV_1;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);
        Ar.Serialize(ref UV_0);
        Ar.Serialize(ref UV_1);
    }
}

/// <summary>
/// 16-bit UV version of static mesh vertex, with up to four UV channels.
/// </summary>
public sealed class TStaticMeshFullVertexFloat16UVs_3 : FStaticMeshFullVertex, ISerializable
{
    public FVector2Half UV_0;
    public FVector2Half UV_1;
    public FVector2Half UV_2;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);
        Ar.Serialize(ref UV_0);
        Ar.Serialize(ref UV_1);
        Ar.Serialize(ref UV_2);
    }
}

/// <summary>
/// 16-bit UV version of static mesh vertex, with up to four UV channels.
/// </summary>
public sealed class TStaticMeshFullVertexFloat16UVs_4 : FStaticMeshFullVertex, ISerializable
{
    public FVector2Half UV_0;
    public FVector2Half UV_1;
    public FVector2Half UV_2;
    public FVector2Half UV_3;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);
        Ar.Serialize(ref UV_0);
        Ar.Serialize(ref UV_1);
        Ar.Serialize(ref UV_2);
        Ar.Serialize(ref UV_3);
    }
}

/// <summary>
/// 32-bit UV version of static mesh vertex, with up to four UV channels.
/// </summary>
public sealed class TStaticMeshFullVertexFloat32UVs_1 : FStaticMeshFullVertex, ISerializable
{
    public FVector2 UV_0;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);
        Ar.Serialize(ref UV_0);
    }
}


/// <summary>
/// 32-bit UV version of static mesh vertex, with up to four UV channels.
/// </summary>
public sealed class TStaticMeshFullVertexFloat32UVs_2 : FStaticMeshFullVertex, ISerializable
{
    public FVector2 UV_0;
    public FVector2 UV_1;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);
        Ar.Serialize(ref UV_0);
        Ar.Serialize(ref UV_1);
    }
}

/// <summary>
/// 32-bit UV version of static mesh vertex, with up to four UV channels.
/// </summary>
public sealed class TStaticMeshFullVertexFloat32UVs_3 : FStaticMeshFullVertex, ISerializable
{
    public FVector2 UV_0;
    public FVector2 UV_1;
    public FVector2 UV_2;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);
        Ar.Serialize(ref UV_0);
        Ar.Serialize(ref UV_1);
        Ar.Serialize(ref UV_2);
    }
}

/// <summary>
/// 32-bit UV version of static mesh vertex, with up to four UV channels.
/// </summary>
public sealed class TStaticMeshFullVertexFloat32UVs_4 : FStaticMeshFullVertex, ISerializable
{
    public FVector2 UV_0;
    public FVector2 UV_1;
    public FVector2 UV_2;
    public FVector2 UV_3;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);
        Ar.Serialize(ref UV_0);
        Ar.Serialize(ref UV_1);
        Ar.Serialize(ref UV_2);
        Ar.Serialize(ref UV_3);
    }
}

#endregion

#endregion

public class FStaticMeshLODElement : ISerializable
{
    /// <summary>
    /// Material to use for this section of this LOD.
    /// </summary>
    public /*UMaterialInterface*/ int Material;

    /// <summary>
    /// Whether to enable shadow casting for this section of this LOD.
    /// </summary>
    public bool bEnableShadowCasting;

    /// <summary>
    /// Whether or not this element is selected.
    /// </summary>
    public bool bIsSelected;

    /// <summary>
    /// Whether to enable collision for this section of this LOD.
    /// </summary>
    public bool bEnableCollision;

    public void Serialize(UnrealArchive Ar)
    {
        //Ar.Serialize(ref Material);
        //Ar.Serialize(ref bEnableShadowCasting); Ar.Position += 3;
        //Ar.Serialize(ref bIsSelected); Ar.Position += 3;
        //Ar.Serialize(ref bEnableCollision);
    }
}
