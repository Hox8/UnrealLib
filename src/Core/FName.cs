using System;
using System.Diagnostics;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FName : ISerializable
{
    #region Serialized members

    internal int Index;
    internal int Number;

    #endregion

    #region Transient members

    internal FNameEntry NameEntry;

    #endregion

    #region Constructors

    // Used by UnrealArchive serializer. Not to be used publicly!
    public FName() { }

    public FName(FNameEntry entry, int number = 0)
    {
        Debug.Assert(entry is not null);

        Index = entry.Index;
        Number = number;
        NameEntry = entry;
    }

    #endregion

    #region Equality

    public static bool operator ==(FName a, FName b) => ReferenceEquals(a, b) || a is not null && b is not null && a.Index == b.Index && a.Number == b.Number;
    public static bool operator !=(FName a, FName b) => !(a == b);

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Index);
        Ar.Serialize(ref Number);

        if (Ar is UnrealPackage upk)
        {
            NameEntry = upk.GetNameEntry(Index);
            if (Number > 0) NameEntry.bDoOffset = true;
        }
    }

    #region Accessors

    public int GetIndex() => Index;
    public int GetNumber() => Number - (NameEntry.bDoOffset ? 1 : 0);
    public FNameEntry GetNameEntry() => NameEntry;
    /// <summary>Returns this FName's string representation with its formatted number suffix, if applicable.</summary>
    public override string ToString() => GetNumber() > 0 ? $"{NameEntry.Name}_{GetNumber()}" : NameEntry.Name;
    /// <summary>Returns the FName's string representation without any number suffixes.</summary>
    public string ToStringRaw() => NameEntry.Name;

    #endregion

    #region Helpers

    /// <summary>
    /// Attempts to split a numbered string name i.e. "String_3" into an FName (separate Name + Number fields).
    /// </summary>
    /// <param name="input">The string name to be split.</param>
    /// <param name="newName">The resultant string name; populated only if conversion succeeds.</param>
    /// <param name="newNumber">The resultant number. Set to 0 by default.</param>
    /// <returns>True if the conversion succeeded, otherwise false.</returns>
    public static bool SplitName(string input, out string newName, out int newNumber)
    {
        newName = input;
        newNumber = 0;

        // Get index of final underscore
        int index = input.LastIndexOf('_');

        // Exit early (invalid number suffix) if...

        // ...the underscore was the first/final char, or it wasn't found at all
        if (index <= 0 || index == input.Length) return false;

        // ...the number is padded, e.g. '_01' or '_00053'
        if (input[index + 1] == '0' && index + 1 != input.Length - 1) return false;

        // ...the number isn't valid
        if (!int.TryParse(input[(index + 1)..], out newNumber)) return false;

        newName = input[..index];
        return true;
    }

    #endregion
}
