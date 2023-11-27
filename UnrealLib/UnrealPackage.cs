using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;

namespace UnrealLib;

// Commonly-used names should be cached at package load
// This will likely be faster when we're dealing with lots of name comparisons e.g. dealing with UProperties
// A lazy-load approach where it's cached on first access sounds good

public class UnrealPackage : UnrealArchive
{
    public UnrealStream Stream;
    internal FPackageFileSummary Summary;
    private List<FNameEntry> Names;
    private List<FObjectImport> Imports;
    private List<FObjectExport> Exports;

    public int EngineVersion => Summary.EngineVersion;
    public int LicenseeVersion => Summary.LicenseeVersion;
    public int EngineBuild => Summary.EngineBuild;
    public int CookerVersion => Summary.CookerVersion;

    #region Accessors
    
    /// <summary>
    /// Returns the <see cref="FNameEntry?"/> at the specified index.
    /// </summary>
    public FNameEntry? GetName(int index) => index <= Names.Count ? Names[index] : null;
    
    /// <inheritdoc cref="GetObject"/>
    public FObjectExport? GetExport(int index) => (FObjectExport?)GetObject(index);
    
    /// <inheritdoc cref="GetObject"/>
    public FObjectImport? GetImport(int index) => (FObjectImport?)GetObject(index);
    
    /// <summary>
    /// Returns the object at Index.
    /// </summary>
    /// <remarks>
    /// Object indices are negative for <see cref="Imports"/>, and positive (non-zero) for <see cref="Exports"/>.  
    /// </remarks>
    /// <example>
    /// An <see cref="FObjectImport"/> in the imports table at position 0 would have an index of -1.<br/><br/>
    /// An <see cref="FObjectExport"/> in the Exports table at position 0 would have an index of 1.<br/><br/>
    /// </example>
    public FObjectResource? GetObject(int index)
    {
        if (index < 0 && ~index <= Imports.Count)
        {
            return Imports[~index];
        }
        
        if (index > 0 && index < Exports.Count)
        {
            return Exports[index - 1];
        }

        return null;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Parameterless constructor. Use for delayed initialization.
    /// </summary>
    public UnrealPackage(string filePath, bool delayInitialization) : base(filePath)
    {
        if (!delayInitialization)
        {
            Load();
        }
    }

    public UnrealPackage(byte[] buffer, bool resizable = false) : base()
    {
        Stream = new(buffer, resizable);
    }

    #endregion

    #region IO and initialization methods

    public void Write(ReadOnlySpan<byte> value) => Stream.Write(value);
    
    public sealed override bool Load()
    {
        if (HasError) return false;

        Stream = new UnrealStream(QualifiedPath);

        // @TODO: Put this in its own method
        try
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
        }
        catch
        {
            SetError(UnrealArchiveError.ParseFailed);
            return false;
        }

        return true;
    }

    public sealed override long Save(string? path = null)
    {
        Stream.Position = 0;
        return Stream.Length;
    }

    public override void DisposeUnmanagedResources()
    {
        Stream.Dispose();
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
            // Check if the leaf names match and, if so, compare against the full name of its parent
            if (import.ObjectName == leaf &&
                Ascii.EqualsIgnoreCase(import.Outer?.ToString() ?? string.Empty, parentString))
            {
                return import;
            }
        }

        // Look for a match in the leaf name's Export referencees
        foreach (var export in leaf.Name.Exports)
        {
            // Check if the leaf names match and, if so, compare against the full name of its parent
            if (export.ObjectName == leaf &&
                Ascii.EqualsIgnoreCase(export.Outer?.ToString() ?? string.Empty, parentString))
            {
                return export;
            }
        }

        return null;
    }
    
    /// <summary>
    /// Replaces this export's UObject data entirely with that of the passed byte span.
    /// </summary>
    /// <remarks>
    /// Replacement UObject data must be self-contained i.e. containing UObject header, footer etc.
    /// </remarks>
    public void ReplaceExportData(FObjectExport export, ReadOnlySpan<byte> data)
    {
        Stream.StartSaving();

        export.SerialSize = data.Length;
        export.SerialOffset = (int)Stream.Length;

        Stream.Position = export.SerializedOffset;
        export.Serialize(Stream);

        Stream.Position = Stream.Length;
        Stream.Write(data);
        
        // Stream.StartLoading();
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
            if (span[i].Equals(searchTerm))
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

    public UObject GetObjectDEBUG(FObjectExport export)
    {
        Stream.Position = export.SerializedOffset;

        var obj = export.Class?.Name.ToString() switch
        {
            "Field"     => new UField(Stream, this, export),
            "Function"  => new UFunction(Stream, this, export),
            "State"     => new UState(Stream, this, export),
            "Struct"    => new UStruct(Stream, this, export),
            _           => new UClass(Stream, this, export)     // UClass if class ref is null
        };

        obj.Serialize(Stream);
        return obj;
    }
}
