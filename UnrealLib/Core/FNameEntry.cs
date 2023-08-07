using UnLib.Enums;
using UnLib.Interfaces;

namespace UnLib.Core;

public class FNameEntry : ISerializable
{
    internal string Name;
    internal ObjectFlags NameFlags;

    /// <summary>
    /// This object's index in the name table when re-serialized. Used by all FNames during serialization
    /// </summary>
    public int SerializedIndex;

    // In-memory

    /// <summary>
    /// The import object referencing this name, if any.
    /// In-memory only.
    /// </summary>
    internal List<FObjectImport> Imports = new();
    
    /// <summary>
    /// A list of export objects referencing this name.
    /// In-memory only.
    /// </summary>
    internal List<FObjectExport> Exports = new();

    // @TODO: Track index serialized to support moving names around
    public void Serialize(UnrealStream UStream)
    {
        UStream.Serialize(ref Name);
        UStream.Serialize(ref NameFlags);
    }

    public override string ToString() => Name;

    /// <summary>
    /// Returns true if an export referencee has a name instance of more than 0. Used for UEE name consistency.
    /// </summary>
    /// <returns></returns>
    internal bool HasPositiveMinInstance()
    {
        for (int i = 0; i < Exports.Count; i++)
        {
            if (Exports[i].ObjectName.Instance > 0) return true;
        }

        return false;
    }
}