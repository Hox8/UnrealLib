using UnrealLib.Core;

namespace UnrealLib.Config;

public static class IniSectionExtensions
{
    private enum PropertyOperator
    {
        /// <summary>
        /// Add the property regardless of any duplicates.
        /// </summary>
        Add,

        /// <summary>
        /// Add the property if its key and value don't already exist.
        /// </summary>
        AddUnique,

        /// <summary>
        /// Remove the last instance of the key and value.
        /// </summary>
        Remove,

        /// <summary>
        /// Remove all instances of the specified key.
        /// </summary>
        RemoveAll
    }

    public static void AddSpecial(this IniSection section, string key, string value)
    {
        UDebug.Assert(key.Trim() == key, "Key is not trimmed");
        UDebug.Assert(value.Trim() == value, "Value is not trimmed");

        // Bail out if key is empty or a comment
        if (key.Length == 0 || key[0] == ';' || key[0] == '#')
            return;

        // Figure out which type of operation we need to do
        PropertyOperator operation = key[0] switch
        {
            '!' => PropertyOperator.RemoveAll,
            '-' => PropertyOperator.Remove,
            '+' => PropertyOperator.AddUnique,
            '.' or _ => PropertyOperator.Add,
        };

        // Remove the prefix if we had one
        key = key[0] switch
        {
            '!' or '-' or '+' or '.' => key.Substring(1),
            _ => key
        };

        // Ini keys cannot be zero-length
        if (key.Length == 0)
            return;

        switch (operation)
        {
            case PropertyOperator.Add:
                section.AddProperty(IniProperty.FromParts(key, value));
                break;

            case PropertyOperator.AddUnique:
                section.TryAddProperty(key, value, true, out _);
                break;

            case PropertyOperator.Remove:
                section.RemovePropertyKeyValue(key, value);
                break;

            case PropertyOperator.RemoveAll:
                section.RemovePropertyKey(key, true);
                break;
        }
    }
};
