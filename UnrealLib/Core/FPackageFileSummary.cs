using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FPackageFileSummary : ISerializable
{
    uint Tag;
    internal short EngineVersion;
    private short LicenseeVersion;
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
    
    private int EngineBuild;
    private int CookerVersion;

    private CompressionFlags CompressionFlags;
    private List<FCompressedChunk> CompressedChunks;
    private int PackageSource;

    private List<string> AdditionalCookedPackages;

    private List<FTextureType> TextureAllocations;

    public void Serialize(UnrealStream UStream)
    {
        UStream.Serialize(ref Tag);

        if (Tag != Globals.PackageTag)
        {
            if (Tag == Globals.PackageTagSwapped) throw new Exception("Big-endian packages are not supported!");
            else throw new Exception("Unknown file signature!");
        }
        
        UStream.Serialize(ref EngineVersion);
        UStream.Serialize(ref LicenseeVersion);
        UStream.Serialize(ref TotalHeaderSize);
        UStream.Serialize(ref FolderName);
        UStream.Serialize(ref PackageFlags);
        
        UStream.Serialize(ref NameCount);
        UStream.Serialize(ref NameOffset);
        UStream.Serialize(ref ExportCount);
        UStream.Serialize(ref ExportOffset);
        UStream.Serialize(ref ImportCount);
        UStream.Serialize(ref ImportOffset);
        UStream.Serialize(ref DependsOffset);
        
        UStream.Serialize(ref ImportExportGuidsOffset);
        UStream.Serialize(ref ImportGuidsCount);
        UStream.Serialize(ref ExportGuidsCount);
        
        UStream.Serialize(ref ThumnailTableOffset);
        
        UStream.Serialize(ref Guid);
        UStream.Serialize(ref Generations);
        
        UStream.Serialize(ref EngineBuild);
        UStream.Serialize(ref CookerVersion);   // '0' if uncooked
        
        UStream.Serialize(ref CompressionFlags);
        UStream.Serialize(ref CompressedChunks);
        UStream.Serialize(ref PackageSource);
        
        UStream.Serialize(ref AdditionalCookedPackages);
        UStream.Serialize(ref TextureAllocations);
    }
}