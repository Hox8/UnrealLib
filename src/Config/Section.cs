using System;
using System.Collections.Generic;
using UnrealLib.Interfaces;

namespace UnrealLib.Config;

public class Section : ISerializable
{
    public string Name;
    public List<Property> Properties = [];

    #region Constructors

    public Section() { }

    public Section(string name)
    {
        Name = name;
    }

    #endregion

    #region Getters and Setters

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

    public bool TryAddProperty(string key, out Property outProperty)
    {
        if (!TryGetProperty(key, out outProperty))
        {
            outProperty = new Property { Key = key };
            Properties.Add(outProperty);
            return true;
        }

        return false;
    }

    // Adds a property to Properties unconditionally.
    public void AddProperty(string iniLine) => Properties.Add(new Property(iniLine));

    #endregion

    #region Accessors

    public override string ToString() => $"{Name}, {Properties.Count} properties";

    #endregion

    private enum ArrayOperator
    {
        Clear,      // Clear out all array entries
        Remove,
        AddConditional,
        AddUnconditional,
    }

    /// <summary>
    /// Updates this section's Properties in way similar to how ini defaults work. @TODO elaborate
    /// </summary>
    public void ParseProperty(string iniLine)
    {
        var property = new Property(iniLine);

        // Don't process comments or invalid properties
        if (property.IsComment || string.IsNullOrWhiteSpace(property.Key)) return;

        ArrayOperator operation;

        // Get array operator
        switch (property.Key[0])
        {
            // Array operator prefix present. Parse, and remove from Key
            case '!' or '-' or '+' or '.':
                operation = property.Key[0] switch
                {
                    '-' => ArrayOperator.Remove,
                    '+' => ArrayOperator.AddConditional,
                    '.' => ArrayOperator.AddUnconditional,
                    _ => ArrayOperator.Clear
                };
                property.Key = property.Key[1..];
                break;

            // Array operator prefix not present. Default to AddUnconditional
            default:
                operation = ArrayOperator.AddUnconditional;
                break;
        }

        // Parse property according to ArrayOperator type
        switch (operation)
        {
            // Remove all properties with a matching Key
            case ArrayOperator.Clear:
                for (int i = Properties.Count - 1; i >= 0; i--)
                {
                    var curProp = Properties[i];

                    // Extra logic to support both implicit and explicit array indexing, i.e. 'MyArray=', 'MyArray[0]='
                    if (curProp.Key.StartsWith(property.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        // Delete property if its key matches exactly or it has an explicit array indexer
                        if (curProp.Key.Length == property.Key.Length ||
                            curProp.Key[property.Key.Length] == '[' && curProp.Key[^1] == ']')
                        {
                            Properties.RemoveAt(i);
                        }
                    }
                }
                break;

            // Removes the last property with a matching Key and Value
            case ArrayOperator.Remove:
                foreach (var prop in Properties)
                {
                    if (property.EqualsCaseInsensitive(prop))
                    {
                        Properties.Remove(prop);
                    }
                }
                break;

            // Add property if its value is not already present
            case ArrayOperator.AddConditional:
                bool shouldAdd = true;

                foreach (var prop in Properties)
                {
                    if (property.EqualsCaseInsensitive(prop))
                    {
                        shouldAdd = false;
                        break;
                    }
                }

                if (shouldAdd)
                {
                    Properties.Add(property);
                }

                break;

            // Add property unconditionally
            // This is the default behavior if no prefix is passed!
            case ArrayOperator.AddUnconditional:
                Properties.Add(property);
                break;
        }
    }

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Name);
        Ar.Serialize(ref Properties);
    }
}
