using System;
using System.Collections.Generic;
using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FPackageFileSummary : ISerializable
{
    internal uint Tag;
    internal short EngineVersion;
    internal short LicenseeVersion;
    private int TotalHeaderSize;
    private string FolderName;
    private PackageFlags PackageFlags;

    internal int NameCount;
    private int NameOffset;
    internal int ExportCount;
    private int ExportOffset;
    internal int ImportCount;
    private int ImportOffset;
    private int DependsOffset;
    
    private int ImportExportGuidsOffset;
    private int ImportGuidsCount;
    private int ExportGuidsCount;
    
    private int ThumnailTableOffset;

    private FGuid Guid;
    private List<FGenerationInfo> Generations;

    internal int EngineBuild;
    internal int CookerVersion;

    private CompressionFlags CompressionFlags;
    private List<FCompressedChunk> CompressedChunks;
    private int PackageSource;

    internal List<string> AdditionalPackagesToCook;

    private FTextureAllocations TextureAllocations;

    public void Serialize(UnrealStream stream)
    {
        stream.Serialize(ref Tag);

        if (Tag != Globals.PackageTag)
        {
            if (Tag == Globals.PackageTagSwapped) throw new Exception("Big-endian packages are not supported!");
            else throw new Exception("Unknown file signature!");
        }
        
        stream.Serialize(ref EngineVersion);
        stream.Serialize(ref LicenseeVersion);
        stream.Serialize(ref TotalHeaderSize);
        stream.Serialize(ref FolderName);
        stream.Serialize(ref PackageFlags);
        
        stream.Serialize(ref NameCount);
        stream.Serialize(ref NameOffset);
        stream.Serialize(ref ExportCount);
        stream.Serialize(ref ExportOffset);
        stream.Serialize(ref ImportCount);
        stream.Serialize(ref ImportOffset);
        stream.Serialize(ref DependsOffset);
        
        stream.Serialize(ref ImportExportGuidsOffset);
        stream.Serialize(ref ImportGuidsCount);
        stream.Serialize(ref ExportGuidsCount);
        
        stream.Serialize(ref ThumnailTableOffset);
        
        stream.Serialize(ref Guid);
        stream.Serialize(ref Generations);
        
        stream.Serialize(ref EngineBuild);
        stream.Serialize(ref CookerVersion);
        
        stream.Serialize(ref CompressionFlags);
        stream.Serialize(ref CompressedChunks);
        stream.Serialize(ref PackageSource);
        
        stream.Serialize(ref AdditionalPackagesToCook);
        stream.Serialize(ref TextureAllocations);
    }
}