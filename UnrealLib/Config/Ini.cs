// @TODO:
// - Add getters and setters for: bool, int, float, (array?).
// - Add documentation
// - Add a serializer option to remove malformed data. Currently malformed data are converted to empty properties

// @Big TODO
// Serializer options only apply when writing text inis. Binary files do NOT take into account any serializer options!

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
    public bool Modified = false;

    public Section Globals = new();

    /// <summary>
    /// Parameterless constructor. Used internally by UnrealStream serializer.
    /// </summary>
    public Ini() { }

    /// <summary>
    /// Constructs a new <see cref="Ini"/> instance and reads a file into it.
    /// </summary>
    public Ini(string filePath)
    {
        Path = filePath;
        Open();
    }

    #region IO Methods

    public void Open(string? filePath = null)
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
                TryAddSection(line[1..^1], out curSection);
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

    /// <summary>
    /// Attempts to parse a <see cref="string"/> from a <see cref="Property"/> within a <see cref="Section"/>.
    /// </summary>
    /// <returns>True if the <see cref="Section"/> and <see cref="Property"/> was found, otherwise false.</returns>
    public bool GetString(string keyName, string sectionName, out string result)
    {
        if (TryGetSection(sectionName, out var section))
        {
            return section.GetString(keyName, out result);
        }

        result = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to parse a <see cref="bool"/> from a <see cref="Property"/> within a <see cref="Section"/>.
    /// </summary>
    /// <returns>True if the <see cref="Section"/> and <see cref="Property"/> was found and parsed successfully, otherwise false.</returns>
    public bool GetBool(string keyName, string sectionName, out bool result)
    {
        if (TryGetSection(sectionName, out var section))
        {
            return section.GetBool(keyName, out result);
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to parse an <see cref="int"/> from a <see cref="Property"/> within a <see cref="Section"/>.
    /// </summary>
    /// <returns>True if the <see cref="Section"/> and <see cref="Property"/> was found and parsed successfully, otherwise false.</returns>
    public bool GetInt(string keyName, string sectionName, out int result)
    {
        if (TryGetSection(sectionName, out var section))
        {
            return section.GetInt(keyName, out result);
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to parse a <see cref="float"/> from a <see cref="Property"/> within a <see cref="Section"/>.
    /// </summary>
    /// <returns>True if the <see cref="Section"/> and <see cref="Property"/> was found and parsed successfully, otherwise false.</returns>
    public bool GetFloat(string keyName, string sectionName, out float result)
    {
        if (TryGetSection(sectionName, out var section))
        {
            return section.GetFloat(keyName, out result);
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to parse a single-line array from a <see cref="Property"/> within a <see cref="Section"/>.
    /// </summary>
    /// <returns>True if the <see cref="Section"/> and <see cref="Property"/> was found and parsed successfully, otherwise false.</returns>
    public bool GetArraySingleLine(string keyName, string sectionName, out string[] result)
    {
        if (TryGetSection(sectionName, out var section))
        {
            return section.GetArraySingleLine(keyName, out result);
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Sets a <see cref="Property"/> value within a <see cref="Section"/>.
    /// </summary>
    /// <remarks>Adds required <see cref="Section"/> and <see cref="Property"/> if either don't exist.</remarks>
    public void SetString(string keyName, string sectionName, string value)
    {
        // Add section if it doesn't exist
        if (!TryGetSection(sectionName, out var section))
        {
            section = new Section(sectionName);
            Sections.Add(section);
        }

        section.SetString(keyName, value);
    }

    /// <summary><inheritdoc cref="SetString"/></summary>
    /// <remarks><inheritdoc cref="SetString"/></remarks>
    public void SetBool(string keyName, string sectionName, bool value) => SetString(keyName, sectionName, value.ToString());
    /// <summary><inheritdoc cref="SetString"/></summary>
    /// <remarks><inheritdoc cref="SetString"/></remarks>
    public void SetInt(string keyName, string sectionName, int value) => SetString(keyName, sectionName, value.ToString());
    /// <summary><inheritdoc cref="SetString"/></summary>
    /// <remarks><inheritdoc cref="SetString"/></remarks>
    public void SetFloat(string keyName, string sectionName, float value) => SetString(keyName, sectionName, value.ToString());
    /// <summary><inheritdoc cref="SetString"/></summary>
    /// <remarks><inheritdoc cref="SetString"/></remarks>
    public void SetArraySingleLine(string keyName, string sectionName, string[] value) => SetString(keyName, sectionName, $"({string.Join(',', value)})");

    #endregion

    #region Helpers

    public bool TryGetSection(string sectionName, out Section? result)
    {
        result = GetSection(sectionName);
        return result is not null;
    }

    public bool TryAddSection(string sectionName, out Section result)
    {
        var section = GetSection(sectionName);
        if (section is not null)
        {
            result = section;
            return false;
        }

        result = new Section(sectionName);
        Sections.Add(result);
        return true;
    }

    private Section? GetSection(string sectionName)
    {
        foreach (var section in Sections)
        {
            if (section.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
            {
                return section;
            }
        }

        return null;
    }

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