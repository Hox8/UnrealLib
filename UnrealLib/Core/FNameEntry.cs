using System.Collections.Generic;
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

    public override string ToString() => Name;
}