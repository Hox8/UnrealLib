using System;
using UnrealLib.Interfaces;

namespace UnrealLib.Config;

public class Property : ISerializable
{
    public string? Key, Value;

    public bool IsComment => Value is null;     // Comments do not set the Value field

    #region Constructors

    public Property() { }

    /// <summary>
    /// Populate a <see cref="Property"/> via a line from within an ini file.
    /// </summary>
    public Property(string iniLine)
    {
        if (string.IsNullOrWhiteSpace(iniLine)) return;

        // Ignore leading whitespace here so we can accurately detect comments
        string trimmedLine = iniLine.TrimStart();

        // If this line is a comment, set its key but not its value
        if (trimmedLine[0] == ';' || trimmedLine[0] == '#')
        {
            Key = trimmedLine;
            return;
        }

        // Split key/value from the first '=' character
        string[] sub = trimmedLine.Split('=', 2, StringSplitOptions.TrimEntries);

        Key = sub[0];

        // Not guaranteed a value was passed, so account for that here.
        Value = sub.Length == 2 ? sub[1] : "";
    }

    #endregion

    #region Accessors

    // @TODO invalid properties will trigger null exception probably
    public override string ToString() => IsComment ? Key : $"{Key}={Value}";

    #endregion

    public bool EqualsCaseInsensitive(Property other) =>
        Key.Equals(other.Key, StringComparison.OrdinalIgnoreCase) &&
        Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Key);
        Ar.Serialize(ref Value);
    }
}
