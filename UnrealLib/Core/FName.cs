using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FName : ISerializable
{
    #region Serialized members

    internal int Index;
    internal int Number = 0;

    #endregion

    #region Transient members

    public FNameEntry NameEntry { get; internal set; }

    #endregion

    #region Accessors

    /// <summary>
    /// Returns the string name of this FName, including its number delimiter if applicable.
    /// </summary>
    public string Name => Number < 2 ? NameEntry.Name : $"{NameEntry}_{Number - 1}";
    public override string ToString() => Name;

    #endregion

    public void Serialize(UnrealArchive _)
    {
        var Ar = (UnrealPackage)_;

        Ar.Serialize(ref Index);
        Ar.Serialize(ref Number);

        // Linking here is SAFE as the name table is serialized before anything else.
        // This also safeguards against forgetting to link these anywhere in the codebase
        NameEntry = Ar.GetNameEntry(Index);
        if (Number > 0) NameEntry.bDoOffset = true;
    }

    // @TODO fails on null
    public static bool operator ==(FName a, FName b) => a.Name == b.Name && a.Number == b.Number;
    public static bool operator !=(FName a, FName b) => !(a == b);
    public static bool operator ==(FName a, string b) => a.Name.Equals(b);
    public static bool operator !=(FName a, string b) => !(a == b);

    /// <summary>
    /// Attempts to split a numbered name i.e. 'String_3' into separate Name + Number fields.
    /// </summary>
    /// <param name="input">The old-style name to be split.</param>
    /// <param name="newName">The resultant string. Will equal the input string if conversion failed.</param>
    /// <param name="newNumber">The resultant number. Will equal 0 if conversion failed.</param>
    /// <returns>True if the conversion succeeded, otherwise false.</returns>
    public static bool SplitName(string input, out string newName, out int newNumber)
    {
        newName = input;
        newNumber = 0;

        // Get index of final underscore
        int index = input.LastIndexOf('_');

        // If the underscore was not found, or it's the first/final char, return false
        if (index <= 0 || index == input.Length) return false;

        // If the number is padded i.e. '01' or '00033', return false
        if (input[index + 1] == '0' && index + 1 != input.Length -1) return false;

        // Return false if number was not a valid integer
        if (!int.TryParse(input[(index + 1)..], out newNumber)) return false;

        newName = input[..index];
        return true;
    }
}
