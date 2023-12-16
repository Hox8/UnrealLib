using System;
using System.IO;
using UnrealLib.Core;
using UnrealLib.Experimental.Sound;
using UnrealLib.Experimental.Textures;
using UnrealLib.Experimental.UnObj;

namespace UnrealLib;

// Commonly-used names should be cached at package load
// This will likely be faster when we're dealing with lots of name comparisons e.g. dealing with UProperties
// A lazy-load approach where it's cached on first access sounds good

public class UnrealPackage : UnrealArchive
{
    internal FPackageFileSummary Summary;
    internal FNameEntry[] Names;
    internal FObjectImport[] Imports;
    public FObjectExport[] Exports;

    #region Accessors

    /// <summary>Returns the engine version this package was saved with, e.g. 864.</summary>
    public int GetEngineVersion() => Summary.EngineVersion;
    /// <summary>Returns the engine build number this package was saved with, e.g. 9714.</summary>
    public int GetEngineBuild() => Summary.EngineBuild;

    #endregion

    public UnrealPackage(string filePath, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite) : base(filePath, mode, access)
    {
        Load();
    }

    public override void Load()
    {
        try
        {
            Serialize(ref Summary);

            if (Summary.IsStoredCompressed)
            {
                Error = ArchiveError.UnsupportedDecompress;
                return;
            }

            Serialize(ref Names, Summary.NameCount);
            Serialize(ref Imports, Summary.ImportCount);
            Serialize(ref Exports, Summary.ExportCount);

            foreach (var import in Imports) import.Link(this);
            foreach (var export in Exports) export.Link(this);

            // Bring down IB2's version. Seems to be incorrect
            Version = Summary.EngineVersion == 864 ? 842 : Summary.EngineVersion;
        }
        catch
        {
            Error = ArchiveError.FailedParse;
        }
    }

    public FObjectResource? GetObject(int objectIndex)
    {
        if (objectIndex > 0 && objectIndex <= Exports.Length)
        {
            return Exports[objectIndex - 1];
        }

        if (objectIndex < 0 && objectIndex <= Imports.Length)
        {
            return Imports[~objectIndex];
        }

        return null;
    }

    public FObjectExport? GetExport(int index) => (FObjectExport?)GetObject(index);
    public FObjectImport? GetImport(int index) => (FObjectImport?)GetObject(index);

    internal FNameEntry? GetNameEntry(int nameIndex) => (nameIndex >= 0 && nameIndex <= Names.Length) ? Names[nameIndex] : null;

    /// <summary>
    /// Locates an <see cref="FObjectResource"/> within the Import and Export tables.
    /// </summary>
    /// <remarks>
    /// - Passing an object's Outer is recommended to narrow down results, but is not required.<br/>
    /// - String comparison are case-insensitive.
    /// </remarks>
    /// <param name="searchTerm">The string name to search for. Can include the object's Outer. Case-insensitive.</param>
    /// <returns>The located <see cref="FObjectExport"/> or <see cref="FObjectImport"/> instance, or null if nothing was found.</returns>
    public FObjectResource? FindObject(string searchTerm)
    {
        string objectStr = searchTerm;
        string outerStr = "";

        // Handle any outer names
        int periodIndex = objectStr.LastIndexOf('.');
        if (periodIndex != -1)
        {
            objectStr = searchTerm[(periodIndex + 1)..];

            // If there are multiple outers, get the last one
            // Only care about the last outer; everything else is ignored
            int objIndex = searchTerm.AsSpan(0, periodIndex).LastIndexOf('.') + 1;
            outerStr = searchTerm[objIndex..periodIndex];
        }

        // Search for outer and leaf names
        // Conflicts can theoretically happen if two objects share the same leaf FName AND immediate parent FName
        // (but different outer somewhere down the line). This will produce accurate results 99.999% of the time, which is fine for now.
        FName? outerName = GetName(outerStr);
        FName? leafName = GetName(objectStr);

        // Make sure the names we're using are not null
        if ((outerStr != "" && outerName is null) || leafName is null)
            return null;

        foreach (var user in leafName.NameEntry.Users)
        {
            if (user.ObjectName == leafName)
            {
                // If we didn't specify an outer, use the first one we find
                if (outerStr == "") return user;

                // Otherwise if the two outers match, return
                if (user.Outer is not null && user.Outer.ObjectName == outerStr) return user;
            }
        }

        return null;
    }

    /// <summary>
    /// Looks for the corresponding <see cref="FNameEntry"/> within the names table.
    /// </summary>
    /// <remarks>
    /// - Automatically converts valid numbered-names into the appropriate format.<br/>
    /// - Case-insensitive.
    /// </remarks>
    /// <param name="searchTerm"></param>
    /// <returns></returns>
    private FName? GetName(string searchTerm)
    {
        // Don't process empty names
        if (string.IsNullOrEmpty(searchTerm)) return null;

        // Convert name in case it ends with a number suffix (e.g. Name_14)
        FName.SplitName(searchTerm, out searchTerm, out int number);

        for (int i = 0; i < Names.Length; i++)
        {
            if (Names[i].Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                // This allows names to stay consistent with UEE
                if (Names[i].bDoOffset) number++;

                return new FName { Index = i, Number = number, NameEntry = Names[i] };
            }
        }

        // Name wasn't found in the names table
        return null;
    }

    /// <summary>
    /// Replaces the passed export's UObject data entirely with that of the passed byte span.
    /// </summary>
    /// <remarks>
    /// Replacement UObject data must be complete-- containing UObject header, footer etc.
    /// </remarks>
    public void ReplaceExportData(FObjectExport export, ReadOnlySpan<byte> data)
    {
        StartSaving();

        // Update export size infos
        export.SerialSize = data.Length;
        export.SerialOffset = (int)Length;

        // Serialize the updated export entry
        Position = export.TableOffset;
        export.Serialize(this);

        // Serialize the export data at the end of the file
        Position = Length;
        Write(data);
    }

    /// <summary>
    /// Attempts to locate an <see cref="FObjectExport"/> at the specified file offset.
    /// </summary>
    public FObjectExport? GetObjectAtOffset(long offset)
    {
        foreach (var export in Exports)
        {
            if (export.SerialOffset + export.SerialSize >= offset)
                return export;
        }

        return null;
    }

    public static UObject GetUObject(FObjectExport export)
    {
        UObject uobject = export.Class?.GetName() switch
        {
            "Field"             => new UField(export),
            "Function"          => new UFunction(export),
            "State"             => new UState(export),
            "Struct"            => new UStruct(export),
            "Texture2D"         => new UTexture2D(export),
            "LightMapTexture2D" => new ULightMapTexture2D(export),
            "SoundNodeWave"     => new SoundNodeWave(export),
            null                => new UClass(export),     // UClass if class ref is null
            _ => throw new NotImplementedException($"'{export.Class}' is not implemented")
        };

        uobject.Serialize(export.Package);
        return uobject;
    }
}
