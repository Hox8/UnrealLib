// @TODO:
// - Add getters and setters for: bool, int, float, (array?).
// - Add documentation
// - Add a serializer option to remove malformed data. Currently malformed data are converted to empty properties

// @Big TODO
// Serializer options only apply when writing text inis. Binary files do NOT take into account any serializer options!

// @TODO: Enforce unique sections should not be optional as inis should adhere to the official spec.

using System;
using System.Collections.Generic;
using System.IO;
using UnrealLib.Config.Coalesced;
using UnrealLib.Interfaces;

namespace UnrealLib.Config;

public class Ini : ISerializable
{
    public string Path;
    public List<Section> Sections = new();
    public IniOptions Options = new();

    public Section Globals = new();
    public bool HasDuplicateSections = false;

    // ERROR
    public string Context;
    public string Description;

    /// <summary>
    /// Parameterless constructor. Used internally by UnrealStream serializer.
    /// </summary>
    public Ini() { }

    /// <summary>
    /// Constructs a new <see cref="Ini"/> instance and reads a file into it.
    /// </summary>
    public Ini(string filePath, bool enforceUniqueSections = false)
    {
        Path = filePath;
        Open(enforceUniqueSections: enforceUniqueSections);
    }

    #region IO Methods

    public void Open(string? filePath = null, bool enforceUniqueSections = true)
    {
        // If a file override wasn't passed, use existing value
        filePath ??= Path;

        var curSection = Globals;
        foreach (string line in File.ReadLines(filePath))
        {
            if (string.IsNullOrEmpty(line)) continue;
            if (line[0] == '[' && line[^1] == ']')
            {
                // Add new section to Sections
                if (!TryAddSection(line[1..^1], out curSection))
                {
                    HasDuplicateSections = true;
                    Context = curSection.Name;
                    
                    if (enforceUniqueSections) return;
                }
                continue;
            }

            // Add line to curSection's properties
            curSection.Properties.Add(new Property(line));
        }
    }

    public void Save(string? filePath = null, IniOptions? options = null)
    {
        // If a file override wasn't passed, use existing value
        filePath ??= Path;

        // If an options override wasn't passed, use existing values
        options ??= Options;

        using var sw = new StreamWriter(filePath);

        if (Options.KeepGlobals && Globals.Properties.Count > 0)
        {
            WriteSectionToDisk(sw, options, Globals);

            // If there are sections to follow, separate with a newline
            if (Sections.Count > 0) sw.WriteLine();
        }

        for (var i = 0; i < Sections.Count; i++)
        {
            WriteSectionToDisk(sw, options, Sections[i]);

            // If there are more sections to follow, separate with a newline
            if (i != Sections.Count - 1) sw.WriteLine();
        }
    }

    #endregion

    #region Getters and Setters

    public bool GetValue<T>(string key, string section, out T value)
    {
        if (TryGetSection(section, out var _section))
        {
            return _section.GetValue(key, out value);
        }

        value = default;
        return false;
    }

    public void SetValue<T>(string key, string section, T value)
    {
        if (!TryGetSection(section, out var _section))
        {
            _section = new Section(key);
            Sections.Add(_section);
        }

        _section.SetValue(key, value);
    }

    #endregion

    #region Helpers

    public bool TryGetSection(string sectionName, out Section? result)
    {
        foreach (var section in Sections)
        {
            if (section.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
            {
                result = section;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool TryRemoveSection(string sectionName)
    {
        if (TryGetSection(sectionName, out var section))
        {
            Sections.Remove(section);
            return true;
        }

        return false;
    }

    public bool TryAddSection(string sectionName, out Section result)
    {
        if (!TryGetSection(sectionName, out result))
        {
            result = new Section(sectionName);
            Sections.Add(result);
            return true;
        }

        return false;
    }

    /*private Section? GetSection(string sectionName)
    {
        foreach (var section in Sections)
        {
            if (section.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
            {
                return section;
            }
        }

        return null;
    }*/

    private static void WriteSectionToDisk(StreamWriter sw, IniOptions options, Section section)
    {
        // If Section is empty, don't serialize its header
        if (!options.KeepEmptySections && section.Properties.Count == 0) return;

        // Write Section header
        sw.WriteLine($"[{section.Name}]");

        // Write Section properties
        foreach (var prop in section.Properties)
        {
            // Filter comments
            if (!options.KeepComments && (prop.Value.StartsWith(';') || prop.Value.EndsWith('#'))) continue;

            // Write property + escape newlines
            sw.WriteLine(prop.ToString().Replace("\n", "\\n"));
        }
    }

    #endregion

    public void Serialize(UnrealStream stream)
    {
        stream.Serialize(ref Path);
        stream.Serialize(ref Sections);
    }

    public override string ToString() => Path;
}

public sealed class IniOptions
{
    /// <summary>
    /// Whether to serialize global (sectionless) Ini properties and comments. If not set, these will be omitted during serialization.
    /// </summary>
    public bool KeepGlobals { get; set; } = true;
    /// <summary>
    /// Whether to serialize Ini comments. If not set, all comments ';' '#' will be omitted during serialization.
    /// </summary>
    public bool KeepComments { get; set; } = false;
    /// <summary>
    /// Whether to keep empty sections when serializing ini files. If set to false, section headers without any properties will be removed.
    /// </summary>
    public bool KeepEmptySections { get; set; } = false;

    public IniOptions() {}
    
    public IniOptions(CoalescedOptions options)
    {
        KeepGlobals = options.KeepGlobals;
        KeepComments = options.KeepComments;
        KeepEmptySections = options.KeepComments;
    }
}