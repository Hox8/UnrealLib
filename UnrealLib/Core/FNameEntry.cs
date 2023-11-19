using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FNameEntry : ISerializable
{
    // Serialized
    internal string Name;
    internal ObjectFlags NameFlags;

    // Transient
    public int SerializedIndex;
    public bool HasPositiveMinInstance;

    /// <summary>
    /// The import object referencing this name, if any.
    /// </summary>
    /// <remarks>
    /// In-memory only.
    /// </remarks>
    internal List<FObjectImport> Imports { get; private set; } = new();

    /// <summary>
    /// A list of export objects referencing this name.
    /// </summary>
    /// <remarks>
    /// In-memory only.
    /// </remarks>
    internal List<FObjectExport> Exports { get; private set; } = new();

    // @TODO: Track index serialized to support moving names around
    public void Serialize(UnrealStream stream)
    {
        stream.Serialize(ref Name);
        stream.Serialize(ref NameFlags);
    }

    public void Register(FObjectExport export)
    {
        Exports.Add(export);
        if (export.ObjectName.Instance > 0) HasPositiveMinInstance = true;
    }

    public void Register(FObjectImport import) => Imports.Add(import);

    /// <summary>
    /// Performs a case-insensitive comparison and returns the result.
    /// </summary>
    /// <remarks>
    /// FNames should NEVER contain Unicode characters. Optimizations have been done with this in mind.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string b)
    {
        Debug.Assert(Name is not null, "Cannot get name of null Name reference!");
        Debug.Assert(Ascii.IsValid(Name) && Ascii.IsValid(b), "FName or compare string contain utf16 characters!");
        
        return Ascii.EqualsIgnoreCase(Name, b);
    }

    public static bool operator ==(FNameEntry a, FNameEntry b) => a.Name.Equals(b.Name) && a.NameFlags == b.NameFlags;
    public static bool operator !=(FNameEntry a, FNameEntry b) => !(a == b);
    public static bool operator ==(FNameEntry a, string b) => a.Equals(b);
    public static bool operator !=(FNameEntry a, string b) => !(a == b);
    
    public override string ToString() => Name;
}