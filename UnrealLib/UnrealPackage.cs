﻿using UnLib.Core;
using UnLib.Interfaces;

namespace UnLib;

public enum LinkerLevel
{
    /// The minimum amount of linkage. Incurs minimal performance penalty.
    /// Spends extra processing time linking UObjects together.
    Normal = 0,

    /// Stub. Will influence whether UObject data is copied and linked. Very expensive!
    High = 1
}

public class UnrealPackage : IDisposable, IUnrealStreamable
{
    internal List<FObjectExport> Exports;
    internal List<FObjectImport> Imports;
    internal List<FNameEntry> Names;
    // protected List<List<int>> DependsMap;

    internal FPackageFileSummary Summary;
    internal UnrealStream UStream;

    public UnrealPackage(MemoryStream memStream)
    {
        UStream = new UnrealStream(memStream, this);
        // Init();
    }

    public UnrealPackage(string filePath)
    {
        FilePath = filePath;

        // Delay initializing UnrealStream
    }

    public int Version => Summary.EngineVersion;

    public int Length => UStream.Length;
    public bool InitFailed { get; set; }
    public bool Modified { get; set; } = false;

    public void Write(byte[] value)
    {
        UStream.Write(value);
    }

    // CONFIG
    // public LinkerLevel LinkerLevel = LinkerLevel.Normal;

    public string FilePath { get; set; } = string.Empty;
    public Stream BaseStream => UStream.BaseStream;

    public void Init()
    {
        // If using a filestream, open a stream now.
        if (FilePath.Length > 0) UStream = new UnrealStream(FilePath);
        UStream.IsLoading = true;

        try
        {
            UStream.Position = 0;
            UStream.Serialize(ref Summary);

            // Read names
            Names = new List<FNameEntry>(Summary.NameCount);
            for (var i = 0; i < Summary.NameCount; i++)
            {
                var name = new FNameEntry();

                name.Serialize(UStream);
                name.SerializedIndex = i;

                Names.Add(name);
            }

            // Read and link Imports
            Imports = new List<FObjectImport>(Summary.ImportCount);
            for (var i = 0; i < Summary.ImportCount; i++)
            {
                var import = new FObjectImport();

                import.Serialize(UStream);
                import.SerializedIndex = i;
                import.SerializedOffset = i;

                // Link names
                import.ObjectName._name = Names[import.ObjectName.Index];
                import.ClassPackage._name = Names[import.ClassPackage.Index];
                import.ClassName._name = Names[import.ClassName.Index];

                // Add import as a direct referencer to its object name
                import.ObjectName._name.Imports.Add(import);

                Imports.Add(import);
            }

            // Read and link Exports
            Exports = new List<FObjectExport>(Summary.ExportCount);
            for (var i = 0; i < Summary.ExportCount; i++)
            {
                var export = new FObjectExport();

                export.Serialize(UStream);
                export.SerializedIndex = i;
                export.SerializedOffset = i;

                // Link names
                export.ObjectName._name = Names[export.ObjectName.Index];

                // Add export as a direct referencer to its object name
                export.ObjectName._name.Exports.Add(export);

                Exports.Add(export);
            }

            // UStream.Serialize(ref DependsMap);   // DependsMaps is always empty for cooked console packages

            // Link Imports
            for (var i = 0; i < Summary.ImportCount; i++) Imports[i].Link(this);

            // Link Exports
            for (var i = 0; i < Summary.ExportCount; i++) Exports[i].Link(this);
        }
        // If any errors occurred during serializations, catch them here and set a flag.
        catch (Exception)
        {
            InitFailed = true;
        }

        InitFailed = false;
        UStream.IsLoading = false;
    }

    public void Save()
    {
        BaseStream.Position = 0;
    }

    #region UPK Helpers

    private FName? SearchNames(string searchTerm, int instance)
    {
        for (var i = 0; i < Names.Count; i++)
            if (string.Equals(Names[i].Name, searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                if (Names[i].HasPositiveMinInstance()) instance++;

                return new FName
                {
                    Index = i,
                    Instance = instance,
                    _name = Names[i]
                };
            }

        return null;
    }

    #endregion

    #region UPK Methods

    // This method is not infallible, as a very small percentage of names are not returned.
    // It is however still an improvement over what the previous version's implementation.
    public FName? FindName(string searchTerm)
    {
        // Because a small percentage of names have instances 'baked' into the name string itself,
        // We need to iterate the name table twice in order to correctly pick them up.

        var result = SearchNames(searchTerm, 0);

        // Is the first pass yielded no results, perform a second pass if the searchTerm contains an instance
        if (result is null)
        {
            var instanceIdx = searchTerm.LastIndexOf('_');
            if (instanceIdx != -1)
            {
                var instanceStr = searchTerm[(instanceIdx + 1)..];
                if (int.TryParse(instanceStr, out var parsed)) result = SearchNames(searchTerm[..instanceIdx], parsed);
            }
        }

        return result;
    }

    /// <summary>
    ///     @TODO: Does not support objects where instance is higher than what UEE displays.
    ///     @TODO: Allow for "imprecise" object searching. Currently object path must be exact match.
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <returns></returns>
    public FObjectResource? FindObject(string searchTerm)
    {
        // Convert leaf name into FName.
        // If name does not exist in the name table (null FName), return null.
        var delimIdx = searchTerm.LastIndexOf('.');
        string? outerNameString = null;
        string leafNameString;

        if (delimIdx != -1)
        {
            outerNameString = searchTerm[..delimIdx];
            leafNameString = searchTerm[(delimIdx + 1)..];
        }
        else
        {
            leafNameString = searchTerm;
        }

        var leaf = FindName(leafNameString);
        if (leaf is null) return null;

        // Iterate leaf name entry's import 'referencees' for a match
        foreach (var import in leaf._name.Imports)
        {
            if (import.ObjectName != leaf) continue;

            if (string.Equals(import._outer?.ToString(), outerNameString, StringComparison.OrdinalIgnoreCase))
                return import;
        }

        // Iterate leaf name entry's export 'referencees' for a match
        foreach (var export in leaf._name.Exports)
        {
            if (export.ObjectName != leaf) continue;

            if (string.Equals(export._outer?.ToString(), outerNameString, StringComparison.OrdinalIgnoreCase))
                return export;
        }

        return null;
    }

    public void ReplaceExportData(FObjectExport export, byte[] newData)
    {
        // @TODO: This method should script calculate length! (not memory size though)

        export.SerialOffset = UStream.Length;
        export.SerialSize = newData.Length;

        // Update serialized size and offset in-place since we aren't reconstructing the whole package
        UStream.Position = export.SerializedOffset + 32;
        UStream.Serialize(ref export.SerialSize);
        UStream.Serialize(ref export.SerialOffset);

        // Paste replacement UObject data at EOF
        UStream.Position = UStream.Length;
        UStream.Write(newData);
    }

    public FObjectExport? GetObjectAtOffset(int offset)
    {
        for (var i = 0; i < Exports.Count; i++)
            if (Exports[i].SerialOffset + Exports[i].SerialSize >= offset)
                return Exports[i];

        return null;
    }

    #endregion

    public void Dispose()
    {
        UStream.Dispose();
        GC.SuppressFinalize(this);
    }
}