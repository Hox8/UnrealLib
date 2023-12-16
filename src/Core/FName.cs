using System.Diagnostics;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FName : ISerializable
{
    #region Serialized members

    internal int Index = -1;
    internal int Number;

    #endregion

    #region Transient

    public FNameEntry NameEntry { get; internal set; }

    #endregion

    #region Accessors

    /// <summary>Returns a reference to this FName's string.</summary>
    public string GetString => NameEntry.Name;
    public override string ToString() => GetString;

    #endregion

    #region Operators

    public static bool operator ==(FName a, FName b) => a.Number == b.Number && string.Equals(a.GetString, b.GetString, System.StringComparison.OrdinalIgnoreCase);
    public static bool operator !=(FName a, FName b) => !(a == b);
    public static bool operator ==(FName a, string b) => string.Equals(a.GetString, b, System.StringComparison.OrdinalIgnoreCase);
    public static bool operator !=(FName a, string b) => !(a == b);

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        if (Ar.IsLoading)
        {
            if (Ar.SerializeBinaryProperties)
            {
                Ar.Serialize(ref Index);
                Ar.Serialize(ref Number);

                // Linking here is SAFE as the name table is serialized before anything else.
                // Casting is pretty dirty but nothing other than UnrealPackages should be using binary FNames so far
                if (Ar is UnrealPackage pkg)
                {
                    NameEntry = pkg.GetNameEntry(Index);
                    if (Number > 0) NameEntry.bDoOffset = true;
                }
            }
            else
            {
                // Create a new NameEntry to store the string
                NameEntry = new();
                Ar.Serialize(ref NameEntry.Name);
            }
        }
        else
        {
            if (Ar.SerializeBinaryProperties)
            {
                Debug.Assert(Index != -1, "Cannot serialize non-binary FName to binary format");

                Ar.Serialize(ref Index);
                Ar.Serialize(ref Number);
            }
            else
            {
                Ar.Serialize(ref NameEntry.Name);
            }
        }
    }

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
        if (input[index + 1] == '0' && index + 1 != input.Length - 1) return false;

        // Return false if number was not a valid integer
        if (!int.TryParse(input[(index + 1)..], out newNumber)) return false;

        newName = input[..index];
        return true;
    }
}
