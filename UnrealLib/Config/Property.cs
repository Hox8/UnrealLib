using System;
using UnrealLib.Interfaces;

namespace UnrealLib.Config;

// Change to struct?
public class Property : ISerializable
{
    public string Key = string.Empty;
    public string Value = string.Empty;

    /// <summary>
    /// Parameterless constructor. Used internally by UnrealStream serializer.
    /// </summary>
    public Property() { }

    /// <summary>
    /// Creates a new <see cref="Property"/> instance from existing key and value strings.
    /// </summary>
    public Property(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="Property"/> instance from an Ini file line.
    /// </summary>
    public Property(string line)
    {
        // If this line is commented, starve the Value field
        if (IsCommented(line))
        {
            Key = line;
            return;
        }

        // Try and split Line into two strings from the first '=' character
        string[] sub = line.Split('=', 2, StringSplitOptions.TrimEntries);

        Key = sub[0];
        Value = sub.Length == 2 ? sub[1] : string.Empty;
    }

    public void Serialize(UnrealStream stream)
    {
        stream.Serialize(ref Key);
        stream.Serialize(ref Value);
    }

    private static bool IsCommented(string line) => string.IsNullOrEmpty(line) || line[0] == ';' || line[0] == '#';

    public override string ToString() => IsCommented(Key) ? Key : $"{Key}={Value}";
}