using System;
using System.Collections.Generic;
using UnrealLib.Interfaces;

namespace UnrealLib.Config;

public class Section : ISerializable
{
    public string Name;
    public List<Property> Properties = new();

    /// <summary>
    /// Parameterless constructor. Used internally by UnrealStream serializer.
    /// </summary>
    public Section() { }

    /// <summary>
    /// Creates a new blank <see cref="Section"/> using the specified name.
    /// </summary>
    public Section(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 'Updates' this section's list of properties by parsing a line representing a key/value pair while taking into account special operators: '!' '-' '+' '.'
    /// </summary>
    public void UpdateProperty(string line)
    {
        // Do not process empty lines
        if (string.IsNullOrWhiteSpace(line)) return;

        string[] sub = line.Split('=', 2, StringSplitOptions.TrimEntries);

        string key = sub[0];
        string value = sub.Length == 2 ? sub[1] : "";

        // Take a property line i.e. 'Key=Value' and do things with it
        // Use special prefixes to denote action: '!', '+', '.', '-'.
        // See https://docs.unrealengine.com/5.3/en-US/configuration-files-in-unreal-engine/

        // Get a copy of the property without the special character if one was used
        var prop = key[0] switch
        {
            '!' or '-' or '+' or '.' => new Property(key[1..], value),
            _ => new Property(key, value)
        };

        switch (key[0])
        {
            // Remove all instances of the key
            // Suited for emptying arrays / removing all keys of the same name
            case '!':
                Properties.RemoveAll(p => p.Key.Equals(prop.Key, StringComparison.OrdinalIgnoreCase));
                break;

            // Remove matching key/value
            // Suited for removing a specific array entry
            case '-':
                Properties.RemoveAll(p => p.Key.Equals(prop.Key, StringComparison.OrdinalIgnoreCase) && p.Value.Equals(prop.Value, StringComparison.OrdinalIgnoreCase));
                break;

            // Add if not already present
            // Suited for idk. Seems pretty useless to me
            case '+':
                if (!TryGetProperty(prop.Key, out _)) Properties.Add(prop);
                break;

            // Add regardless
            // Suited for arrays where multiple keys with different values are necessary
            case '.':
            default:
                Properties.Add(prop);
                break;
        }
    }

    #region Getters and Setters

    // @TODO: Using generics over explicit method overloads.
    // This allows for nicer design in some areas but sacrificing 'type'.Parse() for Convert.ChangeType().
    // What are the performance implications? BENCHMARK ME.

    public T GetValue<T>(string key)
    {
        if (TryGetProperty(key, out var prop))
        {
            Globals.TryConvert(prop.Value, out T value);
            return value;
        }

        return default;
    }

    public bool GetValue<T>(string key, out T value)
    {
        if (TryGetProperty(key, out var prop))
        {
            return Globals.TryConvert(prop.Value, out value);
        }

        value = default;
        return false;
    }

    public void SetValue<T>(string key, T value)
    {
        if (!TryGetProperty(key, out var prop))
        {
            prop = new Property { Key = key };
            Properties.Add(prop);
        }

        prop.Value = value.ToString();
    }

    /// <summary>
    /// Attempts to parse a single-line array from a <see cref="Property"/>.
    /// </summary>
    /// <returns>True if the <see cref="Property"/> was found and parsed successfully, otherwise false.</returns>
    //public bool GetArraySingleLine(string keyName, out string[] result)
    //{
    //    if (TryGetProperty(keyName, out var prop))
    //    {
    //        if (string.IsNullOrEmpty(prop.Value) || prop.Value[0] != '(' || prop.Value[^1] != ')')
    //        {
    //            result = default;
    //            return false;
    //        }

    //        result = prop.Value[1..^1].Split(',');
    //        return true;
    //    }

    //    result = default;
    //    return false;
    //}

    //public void SetArraySingleLine(string keyName, string[] value) => SetString(keyName, $"({string.Join(',', value)})");

    #endregion

    #region Helpers

    /// <summary>
    /// Searches for the last instance of a property within a section.
    /// </summary>
    /// <param name="key">The key name of the property to search for.</param>
    /// <param name="result">The resulting property. Will be null if the property was not found.</param>
    /// <returns>True if the property was found, false otherwise.</returns>
    public bool TryGetProperty(string key, out Property? result)
    {
        // Iterate Properties in reverse as later keys take precedence
        for (int i = Properties.Count - 1; i >= 0; i--)
        {
            if (Properties[i].Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                result = Properties[i];
                return true;
            }
        }

        result = null;
        return false;
    }

    public bool TryAddProperty(string key, out Property result)
    {
        if (!TryGetProperty(key, out result))
        {
            result = new Property { Key = key };
            Properties.Add(result);
            return true;
        }

        return false;
    }

    #endregion

    public void Serialize(UnrealStream stream)
    {
        stream.Serialize(ref Name);
        stream.Serialize(ref Properties);
    }

    public override string ToString() => $"[{Name}]";
}