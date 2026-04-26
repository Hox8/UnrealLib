using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnrealLib.Core;
using UnrealLib.Core.Interfaces;

namespace UnrealLib.Config;

// @TODO: The duplication in the TryFind and TryRemove methods is not ideal.

/// <summary>
/// Represents an Ini file section, which contains <see cref="IniProperty"/> children.
/// </summary>
public class IniSection : ISerializable
{
    private string _name;
    private List<IniProperty> _properties;

    public string GetName() => _name;
    public ReadOnlySpan<IniProperty> GetProperties() => CollectionsMarshal.AsSpan(_properties);
    public int GetNumProperties() => _properties.Count;

    #region Constructors

#pragma warning disable 8618

    // Parameterless constructor required for serializer
    public IniSection() { }

#pragma warning restore

    public IniSection(string name)
    {
        _name = name;
        _properties = [];
    }

    #endregion

    /// <summary>
    /// Adds the passed property to the IniSection's
    /// list of properties, regardless of any duplicates.
    /// </summary>
    /// <param name="property">The property to add.</param>
    public void AddProperty(IniProperty property)
    {
        _properties.Add(property);
    }

    // Sets last instance to value, making if not found.
    public void SetPropertyValue(string key, string? value)
    {
        TryAddProperty(key, null, false, out var property);
        property.SetValue(value);
    }

    #region Value converters

    public bool TryGetPropertyValue(string key, out string value)
    {
        if (TryFindPropertyKey(key, out var property))
        {
            value = property.GetValue() ?? "";
            return true;
        }

        value = "";
        return false;
    }

    public bool TryGetPropertyValue(string key, out bool value)
    {
        if (TryFindPropertyKey(key, out var property))
        {
            value = IsTruthy(property.GetValue());
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetPropertyValue(string key, out byte value)
    {
        if (TryFindPropertyKey(key, out var property))
        {
            return byte.TryParse(property.GetValue(), out value);
        }

        value = default;
        return false;
    }

    public bool TryGetPropertyValue(string key, out int value)
    {
        if (TryFindPropertyKey(key, out var property))
        {
            return int.TryParse(property.GetValue(), out value);
        }

        value = default;
        return false;
    }

    public bool TryGetPropertyValue(string key, out float value)
    {
        if (TryFindPropertyKey(key, out var property))
        {
            return float.TryParse(property.GetValue(), out value);
        }

        value = default;
        return false;
    }

    #endregion

    /// <summary>
    /// Retrieves an existing <see cref="IniProperty"/> by key,
    /// or key and value, and creates and adds a new one if not found.
    /// </summary>
    /// <remarks>
    /// String comparisons are case-insensitive.
    /// </remarks>
    /// <param name="key">The property key to search for, or use if no property is found.</param>
    /// <param name="value">The property value to search for, or use if no property is found.</param>
    /// <param name="bCheckValues">Whether to check for matching values in addition to keys.</param>
    /// <param name="outProperty">The resulting <see cref="IniProperty"/>.</param>
    /// <returns><c>true</c> if a new property was created; otherwise <c>false</c>.</returns>
    public bool TryAddProperty(string key, string? value, bool bCheckValues, out IniProperty outProperty)
    {
        if ((bCheckValues && TryFindPropertyKeyValue(key, value, out outProperty!)) ||
            (!bCheckValues && TryFindPropertyKey(key, out outProperty!)))
        {
            return false;
        }

        outProperty = IniProperty.FromParts(key, value);
        _properties.Add(outProperty);
        return true;
    }

    /// <summary>
    /// Removes the <see cref="IniProperty"/> at the specified index.
    /// </summary>
    /// <param name="index">The index to remove.</param>
    public void RemovePropertyAt(int index) => _properties.RemoveAt(index);

    /// <summary>
    /// Removes one, or all, properties that match the specified key.
    /// </summary>
    /// <param name="key">The property key to search for.</param>
    /// <param name="bRemoveAll">If <c>false</c>, removes only the last instance found; otherwise removes all instances.</param>
    public void RemovePropertyKey(string key, bool bRemoveAll = false)
    {
        ReadOnlySpan<IniProperty> propertySpan = GetProperties();

        // Iterate in reverse, as later properties take precedence
        for (int i = propertySpan.Length - 1; i >= 0; i--)
        {
            if (string.Equals(propertySpan[i].GetKey(), key, StringComparison.OrdinalIgnoreCase))
            {
                _properties.RemoveAt(i);

                if (!bRemoveAll)
                    break;
            }
        }
    }

    /// <summary>
    /// Removes one, or all, properties that match the specified key and value.
    /// </summary>
    /// <param name="key">The property key to search for.</param>
    /// <param name="value">The property value to search for.</param>
    /// <param name="bRemoveAll">If <c>false</c>, removes only the last instance found; otherwise removes all instances.</param>
    public void RemovePropertyKeyValue(string key, string value, bool bRemoveAll = false)
    {
        ReadOnlySpan<IniProperty> propertySpan = GetProperties();

        // Iterate in reverse, as later properties take precedence
        for (int i = propertySpan.Length - 1; i >= 0; i--)
        {
            if (string.Equals(propertySpan[i].GetKey(), key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(propertySpan[i].GetValue(), value, StringComparison.OrdinalIgnoreCase))
            {
                _properties.RemoveAt(i);

                if (!bRemoveAll)
                    break;
            }
        }
    }

    /// <summary>
    /// Searches for an <see cref="IniProperty"/> with a matching key.
    /// </summary>
    /// <remarks>
    /// String comparisons are case-insensitive.
    /// </remarks>
    /// <param name="key">The property key to search for.</param>
    /// <param name="outProperty">The resulting <see cref="IniProperty"/>.</param>
    /// <returns><c>true</c> if a property with a matching key was found; otherwise <c>false</c>.</returns>
    public bool TryFindPropertyKey(string key, [NotNullWhen(true)] out IniProperty? outProperty)
    {
        ReadOnlySpan<IniProperty> propertySpan = GetProperties();

        // Iterate in reverse, as later properties take precedence
        for (int i = propertySpan.Length - 1; i >= 0; i--)
        {
            if (string.Equals(propertySpan[i].GetKey(), key, StringComparison.OrdinalIgnoreCase))
            {
                outProperty = propertySpan[i];
                return true;
            }
        }

        outProperty = default;
        return false;
    }

    /// <summary>
    /// Searches for an <see cref="IniProperty"/> with a matching key and value.
    /// </summary>
    /// <remarks>
    /// String comparisons are case-insensitive.
    /// </remarks>
    /// <param name="key">The property key to search for.</param>
    /// <param name="value">The property value to search for.</param>
    /// <param name="outProperty">The resulting <see cref="IniProperty"/>.</param>
    /// <returns><c>true</c> if a property with a matching key and value was found; otherwise <c>false</c>.</returns>
    public bool TryFindPropertyKeyValue(string key, string? value, [NotNullWhen(true)] out IniProperty? outProperty)
    {
        ReadOnlySpan<IniProperty> propertySpan = GetProperties();

        // Iterate in reverse, as later properties take precedence
        for (int i = propertySpan.Length - 1; i >= 0; i--)
        {
            if (string.Equals(propertySpan[i].GetKey(), key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(propertySpan[i].GetValue(), value, StringComparison.OrdinalIgnoreCase))
            {
                outProperty = propertySpan[i];
                return true;
            }
        }

        outProperty = default;
        return false;
    }

    public override string ToString() => _name;

    /// <summary>
    /// Determines whether the specified string represents a <c>true</c> or <c>false</c> value.
    /// </summary>
    /// <param name="value">The string to check against.</param>
    /// <returns><c>true</c> if the passed string represents a truthy string; otherwise <c>false</c>.</returns>
    public static bool IsTruthy(string? value) =>
        value is null ||
        !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(value, "0");

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref _name);
        Ar.Serialize(ref _properties);
    }
}
