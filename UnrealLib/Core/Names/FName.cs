using System;
using System.Runtime.CompilerServices;
using UnrealLib.Core.Interfaces;

namespace UnrealLib.Core;

/// <summary>
/// Describes a reference to an <see cref="FNameEntry"/> within an Unreal Package.
/// </summary>
public class FName : ISerializable
{
    // Internal FName numbers with this value are considered to have no number component.
    private const int NoNumberInternal = 0;

    /// <summary>
    /// Name has come from static initializer and doesn't yet belong to a package.
    /// </summary>
    internal const int STATIC_NAME = -1;

    #region Serialized Members

    // Index is only used during load and save. During runtime, index is not used at all.
    private int _index = STATIC_NAME;
    private int _number;

    #endregion

    #region Transient members

    private FNameEntry _entry;

    #endregion

    #region Accessors

    internal int Index => _index;
    internal int Number => _number;
    internal FNameEntry NameEntry => _entry;

    /// <summary>
    /// Returns the raw string without any number component. Prefer using <see cref="ToString"/>.
    /// </summary>
    internal string NameString => _entry.Name;

    internal void SetNumber(int newNumber) => _number = newNumber;

    internal bool IsNumberAvailable(int number)
    {
        if (_entry is not null)
        {
            foreach (var user in _entry.GetUsers())
            {
                if (user.ObjectName.Number == number)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the raw string followed by a number component if applicable.
    /// </summary>
    public override string ToString()
    {
#if FALSE
        if (_number != NoNumberInternal)
#else
        if (_number > 1)
#endif
        {
            return $"{_entry.Name}_{NumberInternalToExternal(_number)}";
        }

        return _entry.Name;
    }

#endregion

    #region Equality

    public static bool operator ==(FName? a, FName? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        return a._index == b._index && a._number == b._number;
    }
    public static bool operator !=(FName? a, FName? b) => !(a == b);

    public static bool operator ==(FName a, string b) => string.Equals(a.ToString(), b);
    public static bool operator !=(FName a, string b) => !(a == b);

    public static bool Equals(FName? a, FName? b, bool bDoStringComparison)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        if (bDoStringComparison)
        {
            return string.Equals(a.NameString, b.NameString, StringComparison.OrdinalIgnoreCase);
        }

        return a._index == b._index && a._number == b._number;
    }

    #endregion

    public static implicit operator FName(string text)
    {
        return new FName()
        {
            _index = STATIC_NAME,
            _entry = new FNameEntry(text)
        };
    }

    //public static implicit operator string(FName name)
    //{
    //    return name.ToString();
    //}

    public FName() { }

    public FName(int index, int number, FNameEntry entry)
    {
        _index = index;
        _number = number;
        _entry = entry;
    }

    public FName(string text)
    {
        if (SplitName(text, out var split, out var splitNum))
        {
            _entry = new FNameEntry(split.ToString());
            _number = splitNum;
            return;
        }
    }

    public FName(string text, int number)
    {
        _entry = new FNameEntry(text);
        _number = number;
    }

    public void Serialize(UnrealArchive _)
    {
        var Ar = (UnrealPackage)_;

        Ar.Serialize(ref _index);
        Ar.Serialize(ref _number);

        if (Ar.IsLoading)
        {
            // Store our intended name entry as a field
            _entry = Ar.GetNameEntry(_index);
        }
    }

    // Unreal's FName numbers have two states: internal (binary format) and external (string format)
    //
    // The number of the following raw string 'Rocket_17' is external because it is in a human-readable string format.
    // When reading this string in, it is converted to internal (which becomes 16).
    //
    // When serializing FNames to binary, no conversion happens because the number remains internal.
    // Internal-external conversion only happens when reading/producing human-readable strings.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumberInternalToExternal(int x) => x - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumberExternalToInternal(int x) => x + 1;

    /// <summary>
    /// Attempts to split a numbered string name i.e. "String_3" into an FName (separate Name + Number fields).
    /// </summary>
    /// <param name="term">The name to be split.</param>
    /// <param name="outTerm">The resultant name; populated only if conversion succeeds.</param>
    /// <param name="outNum">The resultant number. Set to 0 by default.</param>
    /// <returns>True if the conversion succeeded; false otherwise.</returns>
    public static bool SplitName(ReadOnlySpan<char> term, out ReadOnlySpan<char> outTerm, out int outNum)
    {
        outTerm = term;
        outNum = 0;

        // Get index of last underscore
        int index = term.LastIndexOf('_');

        // Exit early if...

        // ...the underscore was not found, or it was the first or last character
        if ((uint)index >= term.Length - 1)
            return false;

        // ...the number is padded e.g. '01' or '0053'
        if (term[index + 1] == '0' && index + 1 != term.Length - 1)
            return false;

        // ...the number isn't valid
        if (!int.TryParse(term.Slice(index + 1), out outNum))
            return false;

        outTerm = term.Slice(0, index);
        outNum = NumberExternalToInternal(outNum);

        return true;
    }
}
