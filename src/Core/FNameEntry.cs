using System.Collections.Generic;
using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FNameEntry : ISerializable
{
    #region Serialized members

    internal string Name;
    internal ObjectFlags Flags;

    #endregion

    #region Transient members

    // This NameEntry's index into the package's name table
    internal int Index;

    // List to track FObjectResource usage of this FNameEntry
    internal List<FObjectResource> Users = [];

    // This is used to keep FName numbers consistent with that of UEE.
    // This is a hack; I don't understand how it works.
    internal bool bDoOffset;

    #endregion

    #region Accessors

    public override string ToString() => Name;

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Name);
        Ar.Serialize(ref Flags);
    }
}
