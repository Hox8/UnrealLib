using System;
using System.Diagnostics;
using UnrealLib.Core;
using UnrealLib.Core.Interfaces;

namespace UnrealLib.Config;

/// <summary>
/// Represents an Ini file property, which may be a key/value pair or a comment.
/// </summary>
public class IniProperty : ISerializable
{
    private string _key;
    private string? _value;

    #region Constructors

#pragma warning disable 8618

    // Parameterless constructor required for serializer
    public IniProperty() { }

#pragma warning restore

    private IniProperty(string key, string? value)
    {
        _key = key;
        _value = value;
    }

    /// <summary>
    /// Creates a new <see cref="IniProperty"/> from the specified key and value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    /// <returns>The resulting <see cref="IniProperty"/>.</returns>
    public static IniProperty FromParts(string key, string? value = null)
    {
        Debug.Assert(key.Trim() == key, "Key should probably be trimmed...");
        Debug.Assert(value?.Trim() == value, "Value should probably be trimmed...");

        return new IniProperty(key, value);
    }

    /// <summary>
    /// Creates a new <see cref="IniProperty"/> from a line of text.
    /// </summary>
    /// <param name="line">A trimmed, non-empty string representing an Ini property line.</param>
    /// <param name="bTrimSpan">Whether or not the span should be trimmed.</param>
    /// <returns>The resulting <see cref="IniProperty"/>.</returns>
    public static IniProperty FromLine(ReadOnlySpan<char> line, bool bTrimSpan = true)
    {
        Debug.Assert(bTrimSpan || line.Trim().Length == line.Length, "IniProperty 'Line' was not trimmed!");

        int index;

        if (bTrimSpan)
        {
            line = line.Trim();
        }

        if (line.StartsWith(';') || line.StartsWith('#') || (index = line.IndexOf('=')) <= 0)
        {
            // Comment or invalid property:
            // - Line starts with ';' or '#' (comment), OR
            // - Line does not contain '=', or it is at index 0 (invalid)
            //   ^ Last case also catches empty lines

            return new IniProperty(new string(line), null);
        }
        else
        {
            // Valid property

            var key = line.Slice(0, index).Trim();
            var value = line.Slice(index + 1).Trim();

            return new IniProperty(new string(key), new string(value));
        }
    }

    #endregion

    public string GetKey() => _key;
    public string? GetValue() => _value;

    public void SetKey(string key) => _key = key;
    public void SetValue(string? value) => _value = value;

    /// <summary>
    /// Determines whether this property is a key value pair.
    /// </summary>
    /// <returns><c>true</c> if the property is a valid KVP; otherwise <c>false</c>.</returns>
    public bool IsKeyValuePair() => _value is not null;

    /// <summary>
    /// Determines whether this property is a comment.
    /// </summary>
    /// <returns><c>true</c> if the key starts with <c>;</c> or <c>#</c>; otherwise <c>false</c>.</returns>
    public bool IsComment() => _key.Length > 0 && (_key[0] == ';' || _key[0] == '#');

    /// <summary>
    /// Determines whether this property is valid.<br/>
    /// A property is considered valid if it has a non-empty key and either represents a comment or has a non-empty value.
    /// </summary>
    /// <returns><c>true</c> if the property is valid; otherwise <c>false</c>.</returns>
    public bool IsValid() => IsKeyValuePair() || IsComment();

    public override string ToString()
    {
        if (_value is not null)
        {
            return $"{_key}={_value}";
        }

        return _key;
    }

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref _key);
        Ar.Serialize(ref _value);
    }
}
