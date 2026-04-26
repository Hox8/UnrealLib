using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using UnrealLib.Core;
using UnrealLib.Core.Interfaces;

namespace UnrealLib.Config;

[Flags]
public enum IniFilter
{
    None = 0,

    /// <summary>
    /// If set, comments will be filtered out during:            read,   save.
    /// </summary>
    Comments = 1 << 0,

    /// <summary>
    /// If set, invalids will be filtered out during:            read,   save.
    /// </summary>
    Invalids = 1 << 1,

    /// <summary>
    /// If set, empty sections will be filtered out during:              save.
    /// </summary>
    Sections = 1 << 2,

    /// <summary>
    /// If set, global properties will be filtered out during:   read,   save.
    /// </summary>
    Globals = 1 << 3,

    // Combination flags

    /// <summary>
    /// Filter out all non-critical information: comments, invalids, empty sections, and global properties.
    /// </summary>
    All = Comments | Invalids | Sections | Globals
}

/// <summary>
/// Represents an Ini file, which contains <see cref="IniSection"/> children.
/// </summary>
public class IniFile : ISerializable
{
    private string _path;
    private List<IniSection> _sections;
    private IniSection _globalSection = new("");
    private IniFilter _filter;

    public bool bHasBeenModified;

    public ReadOnlySpan<IniSection> GetSections() => CollectionsMarshal.AsSpan(_sections);
    public IniSection GetGlobalSection() => _globalSection;
    public IniFilter GetFilter() => _filter;
    public int GetNumSections() => _sections.Count;

    public string Path
    {
        set => _path = value;
        get => _path;
    }

    public bool IsFilteringComments() => (_filter & IniFilter.Comments) != 0;
    public bool IsFilteringInvalids() => (_filter & IniFilter.Invalids) != 0;
    public bool IsFilteringSections() => (_filter & IniFilter.Sections) != 0;
    public bool IsFilteringGlobals() => (_filter & IniFilter.Globals) != 0;

    #region Constructors

#pragma warning disable 8618

    // Parameterless constructor required for serializer
    public IniFile() { }

#pragma warning restore

    private IniFile(string path, IniFilter filter = IniFilter.None)
    {
        _path = path;
        _sections = [];
        _filter = filter;
    }

    /// <summary>
    /// Constructs an empty <see cref="IniFile"/> instance.
    /// </summary>
    /// <param name="path">The path to set the <see cref="IniFile"/> instance to.</param>
    /// <param name="filter">The <see cref="IniFilter"/> for the <see cref="IniFile"/> instance to use.</param>
    /// <returns>The resulting <see cref="IniFile"/>.</returns>
    public static IniFile Create(string path, IniFilter filter = IniFilter.None)
    {
        return new IniFile(path, filter);
    }

    /// <summary>
    /// Reads an Ini file from disk into a new <see cref="IniFile"/> instance.
    /// </summary>
    /// <param name="path">The path to read the Ini file from, and set the created <see cref="IniFile"/> instance to.</param>
    /// <param name="ini">The resulting <see cref="IniFile"/>.</param>
    /// <param name="filter">The <see cref="IniFilter"/> for the <see cref="IniFile"/> instance to use.</param>
    /// <param name="bAllowDuplicateSections">If <c>false</c>, returns <c>false</c> upon encountering a duplicate section; otherwise silently merges them.</param>
    /// <returns><c>true</c> if the ini file was read successfully; otherwise <c>false</c>.</returns>
    public static bool FromFile(string path, out IniFile ini, IniFilter filter = IniFilter.None, bool bAllowDuplicateSections = false)
    {
        ini = new IniFile(path, filter);
        var currentSection = ini._globalSection;

        using var reader = new StreamReader(path);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var lineSpan = line.AsSpan().Trim();

            // Ignore empty lines
            if (lineSpan.Length == 0)
                continue;

            if (lineSpan.StartsWith('[') && lineSpan.EndsWith(']'))
            {
                // Section

                var name = lineSpan[1..^1];

                if (!ini.TryAddSection(new string(name), out currentSection))
                {
                    if (!bAllowDuplicateSections)
                    {
                        // Failure.
                        return false;
                    }
                }
            }
            else
            {
                // Property

                // Skip if this property is global, and we're filtering against them
                if (ini.IsFilteringGlobals() && currentSection == ini._globalSection)
                    continue;

                var property = IniProperty.FromLine(lineSpan, false);

                if (!ini.FilterProperty(property))
                {
                    currentSection.AddProperty(property);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Reads an Ini file from disk into a new <see cref="IniFile"/> instance.
    /// </summary>
    /// <param name="path">The path to read the Ini file from, and set the created <see cref="IniFile"/> instance to.</param>
    /// <param name="filter">The <see cref="IniFilter"/> for the <see cref="IniFile"/> instance to use.</param>
    /// <returns>The resulting <see cref="IniFile"/>.</returns>
    public static IniFile FromFile(string path, IniFilter filter = IniFilter.None)
    {
        // We must allow for duplicate sections in order to return
        // a valid non-nullable IniFile instance like this
        FromFile(path, out var ini, filter, true);

        return ini;
    }

    #endregion

    #region Saving

    /// <summary>
    /// Writes the <see cref="IniFile"/> to the specified file on disk.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    public void ToFile(string path)
    {
        using var writer = new StreamWriter(path);

        bool bAtStartOfFile = true;

        // Write globals if non-empty, and we aren't filtering them out
        if (!IsFilteringGlobals() && GetGlobalSection().GetNumProperties() > 0)
        {
            WriteSectionProperties(GetGlobalSection(), writer);

            bAtStartOfFile = false;
        }

        foreach (var section in GetSections())
        {
            // Filter out empty sections if we're doing so
            if (IsFilteringSections() && section.GetNumProperties() == 0)
                continue;

            if (bAtStartOfFile)
            {
                bAtStartOfFile = false;
            }
            else
            {
                // Do not write a newline if we haven't written anything yet
                writer.WriteLine();
            }

            // Write section header
            writer.Write('[');
            writer.Write(section.GetName());
            writer.WriteLine(']');

            WriteSectionProperties(section, writer);
        }
    }

    /// <summary>
    /// Writes the <see cref="IniFile"/> to disk using its saved file path.
    /// </summary>
    public void ToFile() => ToFile(_path);

    private void WriteSectionProperties(IniSection section, StreamWriter writer)
    {
        foreach (var property in section.GetProperties())
        {
            if (!FilterProperty(property))
            {
                WritePropertyToStream(property, writer);
            }
        }
    }

    private static void WritePropertyToStream(IniProperty property, StreamWriter writer)
    {
        writer.Write(property.GetKey());

        if (property.IsKeyValuePair())
        {
            writer.Write('=');
            writer.Write(property.GetValue());
        }

        writer.WriteLine();
    }

    #endregion

    /// <summary>
    /// Retrieves an existing <see cref="IniSection"/> by name,
    /// or creates and adds a new one if not found.
    /// </summary>
    /// <param name="name">The name of the section to find or create.</param>
    /// <param name="outSection">The resulting <see cref="IniSection"/>.</param>
    /// <returns><c>true</c> if a new section was created; otherwise <c>false</c>.</returns>
    public bool TryAddSection(string name, out IniSection outSection)
    {
        if (TryFindSection(name, out outSection!))
        {
            return false;
        }

        outSection = new(name);
        _sections.Add(outSection);
        return true;
    }

    /// <summary>
    /// Searches for an <see cref="IniSection"/> with a matching name.
    /// </summary>
    /// <param name="name">The name of the section to search for.</param>
    /// <param name="outSection">The resulting <see cref="IniSection"/>.</param>
    /// <returns><c>true</c> if a section with a matching name was found; otherwise <c>false</c>.</returns>
    public bool TryFindSection(string name, [NotNullWhen(true)] out IniSection? outSection)
    {
        foreach (var section in GetSections())
        {
            if (string.Equals(section.GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                outSection = section;
                return true;
            }
        }

        outSection = default;
        return false;
    }

    /// <summary>
    /// Whether a property should be filtered out or not, depending on the IniFile's current filter settings.
    /// </summary>
    /// <param name="property">The property to check against.</param>
    /// <returns><c>true</c> if the specified property should be filtered out; otherwise <c>false</c>.</returns>
    public bool FilterProperty(IniProperty property)
    {
        // Filter comments if we're doing so
        if (IsFilteringComments() && property.IsComment())
        {
            return true;
        }

        // Filter invalids if we're doing so
        if (IsFilteringInvalids() && !property.IsValid())
        {
            return true;
        }

        return false;
    }

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref _path);
        Ar.Serialize(ref _sections);
    }
}
