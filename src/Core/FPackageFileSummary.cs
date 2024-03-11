using UnrealLib.Core.Compression;
using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FPackageFileSummary : ISerializable
{
    internal uint Tag;                                      // Generic unreal magic identifier
    internal short PackageVersion;                          // Package version this archive was saved with
    internal short LicenseeVersion;                         // Licensee version this package was saved with
    internal int TotalHeaderSize;                           // Total size, in bytes, of this package summary
    internal string FolderName;                             // UnrealEd browser folder name this package is contained in
    internal PackageFlags PackageFlags;                     // Flags describing this package

    internal int NameCount;                                 // Number of names in this package's name table
    internal int NameOffset;                                // Offset into the package containing name table
    internal int ExportCount;                               // Number of exports in this package's export table
    internal int ExportOffset;                              // Offset into the package containing export table
    internal int ImportCount;                               // Number of imports in this package's import table
    internal int ImportOffset;                              // Offset into the package containing import table
    internal int DependsOffset;                             // Offset into the package containing Export dependencies

    internal int ImportExportGuidsOffset;
    internal int ImportGuidsCount;
    internal int ExportGuidsCount;
    internal int ThumbnailTableOffset;                      // Offset into the package containing saved editor thumbnails

    internal FGuid Guid;                                    // Unique identifier for the package
    internal FGenerationInfo[] Generations;                 // Stats regarding older versions of this package
    internal int EngineVersion;                             // Engine version this package was saved with
    internal int CookerVersion;                             // Cooker version this package was saved with

    internal CompressionFlags CompressionFlags;             // Flags determining compression on the package
    internal FCompressedChunk[] CompressedChunks;           // Contains compressed chunks should the package be compressed

    internal int PackageSource;                             // Value determining whether this package is "official" or user-created

    internal string[] AdditionalPackagesToCook;             // References to other packages, most commonly Kismet streamed levels
    internal FTextureAllocations TextureAllocations;        // Table containing exports with inlined textures

    public long OffsetEnd { get; set; }

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Tag);
        Ar.Serialize(ref PackageVersion);
        Ar.Serialize(ref LicenseeVersion);
        Ar.Serialize(ref TotalHeaderSize);
        Ar.Serialize(ref FolderName);
        Ar.Serialize(ref PackageFlags);

        Ar.Serialize(ref NameCount);
        Ar.Serialize(ref NameOffset);
        Ar.Serialize(ref ExportCount);
        Ar.Serialize(ref ExportOffset);
        Ar.Serialize(ref ImportCount);
        Ar.Serialize(ref ImportOffset);
        Ar.Serialize(ref DependsOffset);

        Ar.Serialize(ref ImportExportGuidsOffset);
        Ar.Serialize(ref ImportGuidsCount);
        Ar.Serialize(ref ExportGuidsCount);
        Ar.Serialize(ref ThumbnailTableOffset);

        Ar.Serialize(ref Guid);
        Ar.Serialize(ref Generations);
        Ar.Serialize(ref EngineVersion);
        Ar.Serialize(ref CookerVersion);

        Ar.Serialize(ref CompressionFlags);
        Ar.Serialize(ref CompressedChunks);

        Ar.Serialize(ref PackageSource);

        Ar.Serialize(ref AdditionalPackagesToCook);
        Ar.Serialize(ref TextureAllocations);

        OffsetEnd = Ar.Position;
    }

    public bool IsStoredCompressed => (PackageFlags & PackageFlags.StoreCompressed) != 0;
}
