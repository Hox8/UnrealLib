using System;
using System.Runtime.InteropServices;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Interfaces;

namespace UnrealLib.Experimental.Shaders;

#region Stuff

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct FStaticSwitchParameter : ISerializable
{
    internal FName ParameterName;
    internal bool Value;
    internal bool bOverride;
    internal FGuid ExpressionGUID;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref ParameterName);
        Ar.Serialize(ref Value);
        Ar.Serialize(ref bOverride);
        Ar.Serialize(ref ExpressionGUID);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct FStaticComponentMaskParameter : ISerializable
{
    internal FName ParameterName;
    internal bool R, G, B, A;
    internal bool bOverride;
    internal FGuid ExpressionGUID;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref ParameterName);
        Ar.Serialize(ref R);
        Ar.Serialize(ref G);
        Ar.Serialize(ref B);
        Ar.Serialize(ref A);
        Ar.Serialize(ref bOverride);
        Ar.Serialize(ref ExpressionGUID);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct FNormalParameter : ISerializable
{
    internal FName ParameterName;
    internal byte CompressionSettings;
    internal bool bOverride;
    internal FGuid ExpressionGUID;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref ParameterName);
        Ar.Serialize(ref CompressionSettings);
        Ar.Serialize(ref bOverride);
        Ar.Serialize(ref ExpressionGUID);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal record struct FStaticTerrainLayerWeightParameter : ISerializable
{
    internal FName ParameterName;
    internal int WeightmapIndex;
    internal bool bOverride;
    internal FGuid ExpressionGUID;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref ParameterName);
        Ar.Serialize(ref WeightmapIndex);
        Ar.Serialize(ref bOverride);
        Ar.Serialize(ref ExpressionGUID);
    }
}

internal class FStaticParameterSet : ISerializable
{
    internal FGuid BaseMaterialId;
    internal FStaticSwitchParameter[] StaticSwitchParameters;
    internal FStaticComponentMaskParameter[] StaticComponentMaskParameters;
    internal FNormalParameter[] NormalParameters;
    internal FStaticTerrainLayerWeightParameter[] TerrainLayerWeightParameters;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref BaseMaterialId);
        Ar.Serialize(ref StaticSwitchParameters);
        Ar.Serialize(ref StaticComponentMaskParameters);
        Ar.Serialize(ref NormalParameters);
        Ar.Serialize(ref TerrainLayerWeightParameters);
    }
}

#endregion

public class UShaderCache : UObject
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    record struct VertexFactoryCrcMapStruct
    {
        long VertexFactoryType;
        uint Crc;
    }

    /// <summary>
    /// Shader map priority based on shader cache life span. Lower means it's a longer life span.
    /// </summary>
    internal int ShaderCachePriority;

    private VertexFactoryCrcMapStruct[] VertexFactoryCrcMap;

    public UShaderCache(FObjectExport export) : base(export) { }

    public override void Serialize(UnrealArchive Ar)
    {
        throw new NotImplementedException("Don't use this. Shader serialization is not supported; if you want to ");
    }

    public void HackyIncrementSkipOffsets(UnrealArchive Ar, int offset)
    {
        bool wasLoading = Ar.IsLoading;

        Ar.StartLoading();

        base.Serialize(Ar);

        if (Ar.Version >= 805)
        {
            Ar.Serialize(ref ShaderCachePriority);
        }

        new FShaderCache().Load(Ar, offset);

        if (Ar.Version < 796)
        {
            Ar.Serialize(ref VertexFactoryCrcMap);
        }

        int numMaterialShaderMaps = default;
        Ar.Serialize(ref numMaterialShaderMaps);

        for (int materialIndex = 0; materialIndex < numMaterialShaderMaps; materialIndex++)
        {
            FStaticParameterSet staticParams = default;
            Ar.Serialize(ref staticParams);

            int shaderMapVersion = default, shaderMapLicenseeVersion = default;
            Ar.Serialize(ref shaderMapVersion);
            Ar.Serialize(ref shaderMapLicenseeVersion);

            // Deserialize the offset of the next material
            int skipOffset = default;
            Ar.Serialize(ref skipOffset);

            // Serialize updated skip offset
            Ar.StartSaving();
            Ar.Position -= 4;
            skipOffset += offset;
            Ar.Serialize(ref skipOffset);
            Ar.StartLoading();

            Ar.Position = skipOffset;
        }

        Ar.IsLoading = wasLoading;
    }
}
