using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using UnrealLib.Core.Compression;

namespace UnrealLib.Core;

#region Enums

/// <summary>
/// Filter used when searching for object resources within an Unreal Package.
/// </summary>
[Flags]
public enum FilterType
{
    None = 0,
    Exports = 1 << 0,
    Imports = 1 << 1,

    Both = Exports | Imports
}

#endregion

public sealed partial class UnrealPackage : UnrealArchive
{
    internal PackageFileSummary Summary = new();
    internal List<FNameEntry> Names = [];
    internal List<FObjectImport> Imports = [];
    internal List<FObjectExport> Exports = [];

    private string Name = string.Empty;

    #region Constructors

    private UnrealPackage(string path, FileMode mode, FileAccess access) : base(path, mode, access) { }
    private UnrealPackage(string path, FileStreamOptions options) : base(path, options) { }
    private UnrealPackage(Stream stream) : base(stream) { }

    public static UnrealPackage FromFile(string path, bool bSerializeObjects = true, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, bool bIsConsolePackage = true)
    {
        var upk = new UnrealPackage(path, mode, access);
        upk.ReadFromFile();
        return upk;
    }

    public static UnrealPackage FromFile(string path, FileStreamOptions options)
    {
        var upk = new UnrealPackage(path, options);
        upk.ReadFromFile();
        return upk;
    }

    private void ReadFromFile()
    {
        Name = Path.GetFileNameWithoutExtension(FileStream.Name);

        Serialize();
    }

    public static PackageFileSummary ReadSummary(string path)
    {
        var Ar = new UnrealArchive(path);

        PackageFileSummary summary = default!;
        Ar.Serialize(ref summary);

        return summary;
    }

#if SLIM_BUILD
    // Simple serialize for slim build.
    private void Serialize()
    {
        Serialize(ref Summary);

        if (IsCompressed())
        {
            if (IsLoading)
            {
                if (!CompressionManager.DecompressPackage(this))
                {
                    // @TODO failed, handle
                }
            }
            else
            {
                if (!CompressionManager.CompressPackage(this))
                {
                    // @TODO failed, handle
                }
            }
        }

        Serialize(ref Names, Summary.NameCount);
        Serialize(ref Imports, Summary.ImportCount);
        Serialize(ref Exports, Summary.ExportCount);

        if (IsLoading)
        {
            for (int i = 0; i < ImportCount; i++) Imports[i].Link(this, i);
            for (int i = 0; i < ExportCount; i++) Exports[i].Link(this, i);
        }
    }

#endif // SLIM_BUILD

    #endregion // Constructors

    #region Public Accessors

    public string PackageName => Name;

    public int NameCount => Names.Count;
    public int ImportCount => Imports.Count;
    public int ExportCount => Exports.Count;

    public int PackageVersion { get => Summary.PackageVersion; /*internal*/ set => Summary.PackageVersion = (short)value; }
    public int LicenseeVersion { get => Summary.LicenseeVersion; /*internal*/ set => Summary.LicenseeVersion = (short)value; }
    public int EngineVersion { get => Summary.EngineVersion; /*internal*/ set => Summary.EngineVersion = value; }


#if DEBUG
    public override string ToString() => PackageName;
#endif

    /// <summary>
    /// Whether we are actively saving the package.
    /// </summary>
    internal bool IsSaving { get; private set; }

    public PackageFlags PackageFlags { get => Summary.PackageFlags; set => Summary.PackageFlags = value; }

    public bool IsMap() => (Summary.PackageFlags & PackageFlags.ContainsMap) != 0;
    public bool ContainsScript() => (Summary.PackageFlags & PackageFlags.ContainsScript) != 0;

    /// <summary>
    /// Creates and returns a <see cref="ReadOnlySpan{T}"/> over the package's name table.
    /// </summary>
    public ReadOnlySpan<FNameEntry> GetNames() => Names.AsSpan();

    /// <summary>
    /// Creates and returns a <see cref="ReadOnlySpan{T}"/> over the package's import table.
    /// </summary>
    public ReadOnlySpan<FObjectImport> GetImports() => Imports.AsSpan();

    /// <summary>
    /// Creates and returns a <see cref="ReadOnlySpan{T}"/> over the package's export table.
    /// </summary>
    public ReadOnlySpan<FObjectExport> GetExports() => Exports.AsSpan();

    public List<FObjectExport> GetExportsUnsafe() => Exports;

    #endregion

    #region Internal Accessors

    internal int NameOffset { get => Summary.NameOffset; }
    internal int ImportOffset { get => Summary.ImportOffset; }
    internal int ExportOffset { get => Summary.ExportOffset; }

    internal int TotalHeaderSize { get => Summary.TotalHeaderSize; }

    internal List<CompressedChunk> CompressedChunks { get => Summary.CompressedChunks; }

    #endregion

    #region Compression

    /// <summary>
    /// Returns whether the package has any kind of compression set.
    /// </summary>
    public bool IsCompressed() => Summary.IsCompressed();

    /// <summary>
    /// Sets whether the package should be saved using compression or not.
    /// </summary>
    /// <param name="bUseCompression">
    /// Whether the package should save compressed (<see langword="true"/>) or uncompressed (<see langword="false"/>).
    /// </param>
    public void SetCompression(CompressionFlags compressionFlags)
    {
        if ((compressionFlags & CompressionFlags.None) != 0)
        {
            Summary.PackageFlags |= PackageFlags.StoreCompressed;
            Summary.CompressionFlags = compressionFlags;
        }
        else
        {
            Summary.PackageFlags &= ~PackageFlags.StoreCompressed;
            Summary.CompressionFlags = CompressionFlags.None;
        }
    }

    /// <summary>
    /// Calculates the CompressedChunks required to compress this UnrealPackage.
    /// </summary>
    internal void CalculateCompressionChunks()
    {
        // Clear out any existing compressed chunks.
        CompressedChunks.Clear();

        // The first chunk should contain the entirety of the name,
        // import, and export tables, regardless of MaxChunkSize.
        var chunk = new CompressedChunk
        {
            UncompressedOffset = NameOffset,
            UncompressedSize = TotalHeaderSize - NameOffset
        };

        // Calculate the chunks for our exports' data.
        foreach (var export in GetExports())
        {
            IncrementChunk(export.SerialSize, ref chunk);
        }

        // Add the final chunk to the list if it isn't empty
        if (chunk.UncompressedSize > 0)
        {
            CompressedChunks.Add(chunk);
        }
    }

    /// <summary>
    /// Attempts to increase the size of the specified chunk without exceeding MaxChunkSize.
    /// </summary>
    /// <param name="newSize">The amount to increase the size of the uncompressed chunk by.</param>
    /// <param name="chunk">The CompressedChunk to increment.</param>
    private void IncrementChunk(int newSize, ref CompressedChunk chunk)
    {
        // This could be an exposable class field...
        const int MaxChunkSize = 1024 * 1024;

        // Try increment chunk if it won't exceed MaxChunkSize
        if (chunk.UncompressedSize > 0 && chunk.UncompressedSize + newSize > MaxChunkSize)
        {
            // We can't increment the given chunk without it exceeding MaxChunkSize.
            // Add it to our list, and start a new chunk.

            CompressedChunks.Add(chunk);

            chunk.UncompressedOffset += chunk.UncompressedSize;
            chunk.UncompressedSize = 0;
        }

        chunk.UncompressedSize += newSize;
    }

    #endregion // Compression

    #region Name API

    /// <summary>
    /// Returns the <see cref="FNameEntry"/> at the specified index from within this package's name table.
    /// </summary>
    /// <param name="index">The int32 index to retrieve the <see cref="FNameEntry"/> from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FNameEntry GetNameEntry(int index)
    {
        UDebug.Assert((uint)index <= NameCount, "Bad name index!");

        return Names[index];
    }

    /// <summary>
    /// Looks for the corresponding <see cref="FNameEntry"/> within the package's names table.
    /// </summary>
    /// <remarks>
    /// - Automatically converts valid numbered-names into the appropriate format.<br/>
    /// - Case-insensitive.
    /// </remarks>
    public bool TryFindName(ReadOnlySpan<char> term, [NotNullWhen(true)] out FName? name, bool trySplitName = true)
    {
        int num = default;

        if (trySplitName)
        {
            FName.SplitName(term, out term, out num);
        }

        // Locate name in the name table
        var nameSpan = Names.AsSpan();
        for (int i = 0; i < nameSpan.Length; i++)
        {
            if (term.Equals(nameSpan[i].Name, StringComparison.OrdinalIgnoreCase))
            {
                name = new FName(i, num, nameSpan[i]);
                return true;
            }
        }

        name = default;
        return false;
    }

    public bool TryFindFName(FName inName, [NotNullWhen(true)] out FName? outName)
    {
        // Locate name in the name table
        var nameSpan = Names.AsSpan();
        for (int i = 0; i < nameSpan.Length; i++)
        {
            if (inName.NameString.Equals(nameSpan[i].Name, StringComparison.OrdinalIgnoreCase))
            {
                outName = new FName(i, inName.Number, nameSpan[i]);
                return true;
            }
        }

        outName = default;
        return false;
    }

    #endregion

    #region Object API

    // DO THIS DOCUMENTATION PROPERLY

    /// <summary>
    /// Returns the object resource at the specified index.
    /// Indices <= -1 will look through imports, wheras indices >= 1 will look through exports.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public FObjectResource? GetObjectAtIndex(int index)
    {
        if (index > 0)
        {
            UDebug.Assert(index <= ExportCount, "Index reached beyond the capacity of the export table");

            return Exports[index - 1];
        }
        else if (index < 0)
        {
            UDebug.Assert(index <= ImportCount, "Index reached beyond the capacity of the import table");

            return Imports[~index];
        }

        // Index of 0 represents null
        return null;
    }

    public bool TryGetObjectAtIndex(int index, [NotNullWhen(true)] out FObjectResource? obj)
    {
        if (index == 0)
        {
            obj = default;
            return false;
        }

        if (index > 0)
        {
            obj = Exports[index - 1];
        }
        else
        {
            obj = Imports[~index];
        }

        return true;
    }

#if !SLIM_BUILD
    // @TODO: Make others use this one as core
    public bool TryFindObject(ReadOnlySpan<FName> names, [NotNullWhen(true)] out FObjectResource? outResource, bool bExactMatch = true, FilterType filter = FilterType.Both)
    {
        var leafName = FindOrAddFName(names[0]);

        foreach (var user in leafName.NameEntry.GetUsers())
        {
            /// Filter out object types we aren't interested in
            if ((filter & FilterType.Imports) == 0 && user is FObjectImport) continue;
            if ((filter & FilterType.Exports) == 0 && user is FObjectExport) continue;

            int hierarchyIndex = 0;
            FObjectResource? cur = user;

            while (cur is not null && hierarchyIndex != names.Length)
            {
                var a = names[hierarchyIndex];
                var b = cur.ObjectName;

                if (a.Number != b.Number || !string.Equals(a.NameString, b.NameString, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                cur = cur.Outer;
                hierarchyIndex++;
            }

            if (hierarchyIndex == names.Length || !bExactMatch)
            {
                outResource = user;
                return true;
            }
        }

        outResource = default;
        return false;
    }

    // Version of TryFindObject optimized for existing FObjectResources (FNames)
    // @TODO: This operates on a List (or better yet, a span) of FNames. Make this the CORE function,
    // and the string overload should simply call it
    // This version will also skip over objects that have different classes.
    public bool TryFindObject(FObjectResource resource, [NotNullWhen(true)] out FObjectResource? outResource, bool bExactMatch = true, FilterType filter = FilterType.Both)
    {
        // Try find outermost name
        if (!TryFindFName(resource.ObjectName, out var leafName))
        {
            outResource = default;
            return false;
        }

        // Construct UObject hierarchy
        List<FObjectResource> hierarchy = [resource];
        for (FObjectResource? cur = resource.Outer; cur is not null; cur = cur.Outer)
        {
            hierarchy.Add(cur);
        }

        foreach (var user in leafName.NameEntry.GetUsers())
        {
            // Ignore if classes don't match
            if (user.New_GetClassName() != resource.New_GetClassName())
                continue;

            // Filter out object types we aren't interested in
            if ((filter & FilterType.Imports) == 0 && user is FObjectImport) continue;
            if ((filter & FilterType.Exports) == 0 && user is FObjectExport) continue;

            int hierarchyIndex = 0;
            FObjectResource? cur = user;

            while (cur is not null && hierarchyIndex != hierarchy.Count)
            {
                var a = hierarchy[hierarchyIndex].ObjectName;
                var b = cur.ObjectName;

#if TRUE
                if (!string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase))
#else
                if (a.Number != b.Number || !string.Equals(a.NameString, b.NameString, StringComparison.OrdinalIgnoreCase))
#endif
                {
                    break;
                }

                cur = cur.Outer;
                hierarchyIndex++;
            }

            if (hierarchyIndex == hierarchy.Count || !bExactMatch)
            {
                outResource = user;
                return true;
            }
        }

        outResource = default;
        return false;
    }
#endif

    public bool TryFindObject(ReadOnlySpan<char> term, [NotNullWhen(true)] out FObjectResource? outResource, bool bExactMatch = true, FilterType filter = FilterType.Both, bool trySplitName = true)
    {
        // Split term into all the various name parts: e.g.: 'A.B.C' => 'A', 'B', 'C'
        Span<Range> slices = stackalloc Range[term.Count('.') + 1];
        term.Split(slices, '.', StringSplitOptions.TrimEntries);

        // Using the object's name, try find an equivalant existing FNameEntry within the package
        if (!TryFindName(term[slices[^1]], out var leafName, trySplitName))
        {
            outResource = default;
            return false;
        }

        // Search through all usages of the found FNameEntry
        foreach (var user in leafName.NameEntry.GetUsers())
        {
            UDebug.Assert(user.Package == this);

            /// Filter out object types we aren't interested in
            if ((filter & FilterType.Imports) == 0 && user is FObjectImport) continue;
            if ((filter & FilterType.Exports) == 0 && user is FObjectExport) continue;

            // Leaf name numbers must match
            // if (user.ObjectName.Number != leafName.Number && (leafName.HasNumber() || user.ObjectName.HasNumber())) continue

            // FNames must match
            if (user.ObjectName.NameEntry != leafName.NameEntry || user.ObjectName.Number != leafName.Number) continue;

            // Check outers until we get a mismatch or we run out of outers to check

            FObjectResource? outer = user.Outer;
            int sliceIdx = slices.Length - 2;

            // Check as many outers as we've specified
            while (sliceIdx != -1 && outer is not null)
            {
                FName.SplitName(term[slices[sliceIdx]], out var curName, out var curNum);

                if ((curNum != outer.ObjectName.Number && (curNum > 1 || outer.ObjectName.Number > 1)) ||
                    !curName.Equals(outer.ObjectName.NameString, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                sliceIdx--;
                outer = outer.Outer;
            }

            // If sliceIdx == -1, we matched all our specified outers and thus have found our object
            if (sliceIdx == -1 || !bExactMatch /*|| outer is null*/)
            {
                outResource = user;
                return true;
            }
        }

        outResource = default;
        return false;
    }

    public bool TryFindImport(ReadOnlySpan<char> term, [NotNullWhen(true)] out FObjectImport? outImport, bool bExactMatch = true, bool trySplitName = true)
    {
        if (TryFindObject(term, out var resource, bExactMatch, FilterType.Imports, trySplitName))
        {
            outImport = (FObjectImport)resource;
            return true;
        }

        outImport = null;
        return false;
    }

    public bool TryFindExport(ReadOnlySpan<char> term, [NotNullWhen(true)] out FObjectExport? outExport, bool bExactMatch = true, bool trySplitName = true)
    {
        if (TryFindObject(term, out var resource, bExactMatch, FilterType.Exports, trySplitName))
        {
            outExport = (FObjectExport)resource;
            return true;
        }

        outExport = null;
        return false;
    }

#endregion

#if SLIM_BUILD
    // Simple save for slim build.
    public void Save()
    {
        // If we have any compression flags, do compression.
        if (IsCompressed())
        {
            CompressionManager.CompressPackage(this);
        }

        base.Close();
    }
#endif
}
