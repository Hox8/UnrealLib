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
    /// Attempts to parse a <see cref="string"/> from a <see cref="Property"/>.
    /// </summary>
    /// <returns>True if the <see cref="Property"/> was found, otherwise false.</returns>
    public bool GetString(string keyName, out string result)
    {
        if (TryGetProperty(keyName, out var prop))
        {
            result = prop.Value;
            return true;
        }

        result = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to parse a <see cref="bool"/> from a <see cref="Property"/>.
    /// </summary>
    /// <returns>True if the <see cref="Property"/> was found, otherwise false.</returns>
    public bool GetBool(string keyName, out bool result)
    {
        if (TryGetProperty(keyName, out var prop))
        {
            result = string.Equals(prop.Value, "True", StringComparison.OrdinalIgnoreCase) || prop.Value == "1";
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to parse an <see cref="int"/> from a <see cref="Property"/>.
    /// </summary>
    /// <returns>True if the <see cref="Property"/> was found and parsed successfully, otherwise false.</returns>
    public bool GetInt(string keyName, out int result)
    {
        if (TryGetProperty(keyName, out var prop))
        {
            return int.TryParse(prop.Value, out result);
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to parse a <see cref="float"/> from a <see cref="Property"/>.
    /// </summary>
    /// <returns>True if the <see cref="Property"/> was found and parsed successfully, otherwise false.</returns>
    public bool GetFloat(string keyName, out float result)
    {
        if (TryGetProperty(keyName, out var prop))
        {
            return float.TryParse(prop.Value, out result);
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to parse a single-line array from a <see cref="Property"/>.
    /// </summary>
    /// <returns>True if the <see cref="Property"/> was found and parsed successfully, otherwise false.</returns>
    public bool GetArraySingleLine(string keyName, out string[] result)
    {
        if (TryGetProperty(keyName, out var prop))
        {
            if (string.IsNullOrEmpty(prop.Value) || prop.Value[0] != '(' || prop.Value[^1] != ')')
            {
                result = default;
                return false;
            }

            result = prop.Value[1..^1].Split(',');
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Sets a <see cref="Property"/> value.
    /// </summary>
    /// <remarks>Adds required <see cref="Property"/> if it doesn't exist.</remarks>
    public void SetString(string keyName, string value)
    {
        if (TryGetProperty(keyName, out var prop))
        {
            prop.Value = value;
        }
        else
        {
            prop = new Property(keyName, value);
            Properties.Add(prop);
        }
    }

    /// <summary>
    /// Sets a property value to the passed <see cref="bool"/> value.
    /// </summary>
    public void SetBool(string keyName, bool value) => SetString(keyName, value.ToString());
    /// <summary>
    /// Sets a property value to the passed <see cref="int"/> value.
    /// </summary>
    public void SetInt(string keyName, int value) => SetString(keyName, value.ToString());
    /// <summary>
    /// Sets a property value to the passed <see cref="float"/> value.
    /// </summary>
    public void SetFloat(string keyName, float value) => SetString(keyName, value.ToString());
    /// <summary>
    /// Sets a property value to the passed <see cref="string"/> array value.
    /// </summary>
    public void SetArraySingleLine(string keyName, string[] value) => SetString(keyName, $"({string.Join(',', value)})");

    #region Helpers

    public bool TryGetProperty(string keyName, out Property? result)
    {
        // Iterate Properties in reverse as later keys take precedence
        for (int i = Properties.Count - 1; i >= 0; i--)
        {
            if (Properties[i].Key.Equals(keyName, StringComparison.OrdinalIgnoreCase))
            {
                result = Properties[i];
                return true;
            }
        }

        result = null;
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