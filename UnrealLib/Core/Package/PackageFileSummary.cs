using System;
using System.Collections.Generic;
using UnrealLib.Core.Compression;
using UnrealLib.Core.Interfaces;

namespace UnrealLib.Core;

#region Enums

/// <summary>
/// Flags describing an <see cref="UnrealPackage"/>.
/// </summary>
[Flags]
public enum PackageFlags : uint
{
    /// <summary>
    /// Allow downloading package.
    /// </summary>
    AllowDownload = 1U << 0,

    /// <summary>
    /// Purely optional for clients.
    /// </summary>
    ClientOptional = 1U << 1,

    /// <summary>
    /// Only needed on the server side.
    /// </summary>
    ServerSideOnly = 1U << 2,

    /// <summary>
    /// Whether this package has been cooked for the target platform.
    /// </summary>
    Cooked = 1U << 3,

    /// <summary>
    /// Not trusted.
    /// </summary>
    Unsecure = 1U << 4,

    /// <summary>
    /// Package was saved with newer version.
    /// </summary>
    SavedWithNewerVersion = 1U << 5,

    /// <summary>
    /// Package was saved with desktop serialization rules (not console).
    /// </summary>
    DesktopSerialized = 1U << 6,

    /// <summary>
    /// Client needs to download this package.
    /// </summary>
    Need = 1U << 15,

    /// <summary>
    /// Package is currently being compiled.
    /// </summary>
    Compiling = 1U << 16,

    /// <summary>
    /// Set if the package contains a ULevel/ UWorld object.
    /// </summary>
    ContainsMap = 1U << 17,

    /// <summary>
    /// Set if the package was loaded from the trashcan.
    /// </summary>
    Trash = 1U << 18,

    /// <summary>
    /// Set if the archive serializing this package cannot use lazy loading.
    /// </summary>
    DisallowLazyLoading = 1U << 19,

    /// <summary>
    /// Set if the package was created for the purpose of PIE.
    /// </summary>
    PlayInEditor = 1U << 20,

    /// <summary>
    /// Package is allowed to contain UClasses and UnrealScript.
    /// </summary>
    ContainsScript = 1U << 21,

    /// <summary>
    /// Package contains debug info (for UDebugger).
    /// </summary>
    ContainsDebugInfo = 1U << 22,

    /// <summary>
    /// Package requires all its imports to already have been loaded.
    /// </summary>
    RequireImportsAlreadyLoaded = 1U << 23,

    /// <summary>
    /// All lighting in this package should be self-contained.
    /// </summary>
    /// <remarks>
    /// Only applicable to IB1!
    /// </remarks>
    SelfContainedLighting = 1U << 24,

    /// <summary>
    /// Package is being stored compressed, requires archive support for compression.
    /// </summary>
    StoreCompressed = 1U << 25,

    /// <summary>
    /// Package is serialized normally, and then fully compressed after (must be decompressed before LoadPackage is called).
    /// </summary>
    StoreFullyCompressed = 1U << 26,

    /// <summary>
    /// Package was cooked allowing materials to inline their FMaterials (and hence shaders).
    /// </summary>
    /// <remarks>
    /// Only applicable to IB1!
    /// </remarks>
    ContainsInlinedShaders = 1U << 27,

    /// <summary>
    /// Package contains FaceFX assets and/or animsets.
    /// </summary>
    ContainsFaceFXData = 1U << 28,

    /// <summary>
    /// Package was NOT created by a modder. Internal data not for export.
    /// </summary>
    NoExportAllowed = 1U << 29,

    /// <summary>
    /// Source has been removed to compress the package size.
    /// </summary>
    StrippedSource = 1U << 30,

    /// <summary>
    /// Package has editor-only data filtered.
    /// </summary>
    FilterEditorOnly = 1U << 31
}

#endregion

#region Structs

public struct GenerationInfo
{
    public int ExportCount;
    public int NameCount;
    public int NetObjectCount;
}

public class TextureType : ISerializable
{
    public int SizeX, SizeY;
    public int NumMips;
    public uint Format;
    public uint TexCreateFlags;
    public int[] ExportIndices = [];

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref SizeX);
        Ar.Serialize(ref SizeY);
        Ar.Serialize(ref NumMips);
        Ar.Serialize(ref Format);
        Ar.Serialize(ref TexCreateFlags);
        Ar.Serialize(ref ExportIndices);
    }
}

public class TextureAllocations : ISerializable
{
    public TextureType[] TextureTypes = [];

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref TextureTypes);
    }
}

#endregion

public class PackageFileSummary : ISerializable
{
    #region Serialized members

    /// <summary>
    /// Unreal package magic.</summary>
    public uint Tag = Globals.PackageTag;

    /// <summary>
    /// Binary package version this package was saved with.
    /// </summary>
    public short PackageVersion;

    /// <summary>
    /// Licensee version this package was saved with.
    /// </summary>
    public short LicenseeVersion;

    /// <summary>
    /// Total size in bytes of this package summary header.
    /// </summary>
    public int TotalHeaderSize;

    /// <summary>
    /// Unreal Editor asset browser folder this package resides in.
    /// </summary>
    public string FolderName = "None";

    /// <summary>
    /// Attribute flags describing this package.
    /// </summary>
    public PackageFlags PackageFlags;

    /// <summary>
    /// Number of names stored in this package's name table.
    /// </summary>
    public int NameCount;

    /// <summary>
    /// Offset into this file where this package's name table starts.
    /// </summary>
    public int NameOffset;

    /// <summary>
    /// Number of exports stored in this package's export table.
    /// </summary>
    public int ExportCount;

    /// <summary>
    /// Offset into this file where this package's export table starts.
    /// </summary>
    public int ExportOffset;

    /// <summary>
    /// Number of imports stored in this package's import table.
    /// </summary>
    public int ImportCount;

    /// <summary>
    /// Offset into this file where this package's import table starts.
    /// </summary>
    public int ImportOffset;

    /// <summary>
    /// Offset into this file where this package's depends map starts.
    /// </summary>
    public int DependsOffset;

    /// <summary>
    /// Offset into this file where the package's ImportGuids/ExportGuids data starts.
    /// </summary>
    public int ImportExportGuidsOffset;

    /// <summary>
    /// Number of ImportGuid entries in this package.
    /// </summary>
    public int ImportGuidsCount;

    /// <summary>
    /// Number of ExportGuid entries in this package.
    /// </summary>
    public int ExportGuidsCount;

    /// <summary>
    /// Offset into this file where the thumbnail table starts.
    /// </summary>
    public uint ThumbnailTableOffset;

    /// <summary>
    /// Unique identifier for this package.
    /// </summary>
    public Guid Guid;

    /// <summary>
    /// Data regarding previous versions of this package.
    /// </summary>
    public GenerationInfo[] Generations = [];

    /// <summary>
    /// Engine version this package was saved with.
    /// </summary>
    public int EngineVersion;

    /// <summary>
    /// Cooker version this package was saved with, if cooked.
    /// </summary>
    public int CookerVersion;

    /// <summary>
    /// Attribute flags describing the compression used in this package.
    /// </summary>
    public CompressionFlags CompressionFlags;

    /// <summary>
    /// Array of compressed chunks if this package was stored compressed.
    /// </summary>
    public List<CompressedChunk> CompressedChunks = [];

    /// <summary>
    /// Value determining whether this package was saved by Epic/Licensee or a 3rd party.
    /// </summary>
    public uint PackageSource;

    /// <summary>
    /// List of additional packages that need to be cooked for this package (i.e. streaming levels).
    /// </summary>
    public string[] AdditionalPackagesToCook = [];

    /// <summary>
    /// Information about textures stored within this package.
    /// </summary>
    public TextureAllocations TextureAllocations = new();

    #endregion

    #region Accessors

    // Check if any bits other than options flags are set, to catch any future or unknown compression types.
    public bool IsCompressed() => (CompressionFlags & ~CompressionFlags.OptionsMask) != CompressionFlags.None;

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Tag);

        ArgumentOutOfRangeException.ThrowIfNotEqual(Tag, Globals.PackageTag);

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
    }
}
