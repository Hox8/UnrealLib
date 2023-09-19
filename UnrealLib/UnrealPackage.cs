using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnrealLib.Core;

namespace UnrealLib;

public class UnrealPackage : UnrealArchive
{
    public int EngineVersion => Summary.EngineVersion;
    public int LicenseeVersion => Summary.LicenseeVersion;

    // Private fields
    private UnrealStream Stream;

    internal FPackageFileSummary Summary;
    internal List<FNameEntry> Names;
    internal List<FObjectImport> Imports;
    internal List<FObjectExport> Exports;
    // private List<List<int>> Depends;

    #region Constructors

    /// <summary>
    /// Parameterless constructor. Use for delayed initialization.
    /// </summary>
    public UnrealPackage() { }

    /// <summary>
    /// Constructs an <see cref="UnrealPackage"/> and initializes it from the passed filepath.
    /// </summary>
    public UnrealPackage(string filePath) => Open(filePath);

    #endregion

    #region IO and initialization methods

    public sealed override bool Open(string? path = null)
    {
        // If this UPK has an already-open stream, we should dispose of it before re-assigning it
        Stream.Dispose();

        if (path is not null)
        {
            InitPathInfo(path);
        }

        Stream = new UnrealStream(QualifiedPath);

        return Init();
    }

    public sealed override bool Save(string? path = null)
    {
        if (path is not null) throw new NotImplementedException();

        Stream.StartSaving();

        Stream.Position = 0;
        Stream.Serialize(ref Summary);
        Stream.Serialize(ref Names, Summary.NameCount);
        Stream.Serialize(ref Imports, Summary.ImportCount);
        Stream.Serialize(ref Exports, Summary.ExportCount);

        // Depends map, thumbnail, guids etc.
        // UObject data

        return false;
    }

    public sealed override bool Init()
    {
        Stream.Position = 0;
        Stream.Serialize(ref Summary);
        Stream.Serialize(ref Names, Summary.NameCount);
        Stream.Serialize(ref Imports, Summary.ImportCount);
        Stream.Serialize(ref Exports, Summary.ExportCount);

        // Link imports
        var imports = CollectionsMarshal.AsSpan(Imports);
        for (int i = 0; i < imports.Length; i++)
        {
            imports[i].Link(this, i);
        }

        // Link exports
        var exports = CollectionsMarshal.AsSpan(Exports);
        for (int i = 0; i < exports.Length; i++)
        {
            exports[i].Link(this, i);
        }

        Stream.Dispose();
        return true;
    }

    #endregion

    #region UnrealPackage Methods

    /// <summary>
    /// Attempts to locate an <see cref="FName"/> with a matching string name.
    /// </summary>
    public bool FindName(string searchTerm, out FName? name)
    {
        // Because a small percentage of names have instances 'baked' into the name string itself,
        // We need to iterate the name table twice in order to correctly pick them up.
        name = SearchNames(searchTerm, 0);

        // If the first pass yielded no results, try again by interpreting the name instance
        if (name is null)
        {
            (searchTerm, int instance) = SplitInstance(searchTerm);
            name = SearchNames(searchTerm, instance);
        }

        return name is not null;
    }

    /// <summary>
    /// Attempts to locate an <see cref="FObjectExport"/> with a matching qualified name.
    /// </summary>
    /// <param name="searchTerm">The fully-qualified name to search for.</param>
    public FObjectResource? FindObject(string searchTerm)
    {
        // Split searchTerm into a local 'leaf' name and full parent qualifier
        var (parentString, leafString) = SeparateLeafName(searchTerm);

        // Look for the leaf name in the name table. Return null if not found
        if (!FindName(leafString, out var leaf)) return null;

        // Look for a match in the leaf name's Import referencees
        foreach (var import in leaf.Name.Imports)
        {
            if (import.ObjectName != leaf) continue;

            if (string.Equals(import.Outer?.ToString(), parentString, StringComparison.OrdinalIgnoreCase))
                return import;
        }

        // Look for a match in the leaf name's Export referencees
        foreach (var export in leaf.Name.Exports)
        {
            if (export.ObjectName != leaf) continue;

            if (string.Equals(export.Outer?.ToString(), parentString, StringComparison.OrdinalIgnoreCase))
                return export;
        }

        return null;
    }

    /// <summary>
    /// Attempts to find an <see cref="FObjectExport"/> at the specified file offset.
    /// </summary>
    public FObjectExport? GetObjectAtOffset(int offset)
    {
        var span = CollectionsMarshal.AsSpan(Exports);
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].SerialOffset + span[i].SerialSize >= offset) return span[i];
        }

        return null;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Utility method used by <see cref="FindName"/>. Tries to split a string name from its instance suffix.
    /// </summary>
    private static (string name, int instance) SplitInstance(string searchTerm)
    {
        int instanceIdx = searchTerm.LastIndexOf('_');
        if (instanceIdx != -1)
        {
            if (int.TryParse(searchTerm[(instanceIdx + 1)..], out int instance))
            {
                return (searchTerm[(instanceIdx + 1)..], instance);
            }
        }

        return (searchTerm, 0);
    }

    /// <summary>
    /// Utility method used by <see cref="FindName"/>.
    /// Searches <see cref="Names"/> for the first occurrence of an <see cref="FNameEntry"/> with a matching name.
    /// </summary>
    private FName? SearchNames(string searchTerm, int instance)
    {
        var span = CollectionsMarshal.AsSpan(Names);
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                if (span[i].HasPositiveMinInstance) instance++;

                return new FName
                {
                    Index = i,
                    Instance = instance,
                    Name = span[i]
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Utility method used by <see cref="FindObject"/>. Separates an object name from its fully-qualified name.
    /// </summary>
    private static (string parent, string leaf) SeparateLeafName(string qualifiedObjectName)
    {
        int delimiterIndex = qualifiedObjectName.LastIndexOf('.');
        if (delimiterIndex != -1)
        {
            return (qualifiedObjectName[..delimiterIndex], qualifiedObjectName[(delimiterIndex + 1)..]);
        }

        return (string.Empty, qualifiedObjectName);
    }

    #endregion
}
