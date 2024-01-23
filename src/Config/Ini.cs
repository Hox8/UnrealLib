using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnrealLib.Config.Coalesced;
using UnrealLib.Interfaces;

namespace UnrealLib.Config;

public enum IniError : byte
{
    None = 0,
    NonExist,
    FailedRead,
    FailedWrite,
    ContainsDuplicateSection
}

public class Ini : ErrorHelper<IniError>, ISerializable
{
    public string Path;
    public Section GlobalSection = new("");
    public List<Section> Sections = [];
    public IniOptions Options = new();

    #region Constructors

    public Ini() { }

    public static Ini FromFile(string path)
    {
        var ini = new Ini { Path = path };

        string[] lines;

        try
        {
            lines = File.ReadAllLines(path);
        }
        catch
        {
            ini.SetError(File.Exists(path) ? IniError.NonExist : IniError.FailedRead, path);
            return ini;
        }

        // Initially set to global section
        var curSection = ini.GlobalSection;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line[0] == '[' && line[^1] == ']')
            {
                // Exit early if we encounter a duplicate section
                if (!ini.TryAddSection(line[1..^1], out curSection))
                {
                    ini.SetError(IniError.ContainsDuplicateSection, curSection.Name);
                    return ini;
                }

                continue;
            }

            curSection.AddProperty(line);
        }

        return ini;
    }

    #endregion

    #region Getters/Setters

    public bool TryGetSection(string sectionName, out Section outSection)
    {
        foreach (var section in Sections)
        {
            if (section.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
            {
                outSection = section;
                return true;
            }
        }

        outSection = null;
        return false;
    }

    // Returns true if section was added. False if it already exists.
    public bool TryAddSection(string sectionName, out Section outSection)
    {
        if (TryGetSection(sectionName, out outSection))
        {
            return false;
        }

        outSection = new Section(sectionName);
        Sections.Add(outSection);
        return true;
    }

    // Returns true if removed. False if not.
    public bool TryRemoveSection(string sectionName)
    {
        if (TryGetSection(sectionName, out var section))
        {
            Sections.Remove(section);
            return true;
        }

        return false;
    }

    #endregion

    #region ErrorHelper

    public override string GetErrorString() => ErrorType switch
    {
        IniError.None => "No error.",
        IniError.NonExist => $"'{ErrorContext}' does not exist.",
        IniError.FailedRead => $"Failed to read from '{ErrorContext}'.",
        IniError.FailedWrite => $"Failed to write to '{ErrorContext}'.",
        IniError.ContainsDuplicateSection => $"Contains duplicate section: '{ErrorContext}'."
    };

    #endregion

    #region Accessors

    public override string ToString() => $"{Path}, {Sections.Count} sections";
    public string FriendlyName => Path.StartsWith("..\\..\\") ? Path[6..] : Path;

    #endregion

    public long SaveToFile(string path)
    {
        if (HasError) throw new NotImplementedException();  // come back to this later

        // StringBuilder or StreamWriter? Probably the latter.
        var sb = new StringBuilder();

        // Save global section
        if (Options.KeepGlobals)
        {
            // Used to print a newline only if at least one global property was serialized
            bool writtenAtLeastOnce = false;

            foreach (var property in GlobalSection.Properties)
            {
                if (!Options.KeepComments && property.IsComment) continue;

                sb.Append($"{property}\n");
                writtenAtLeastOnce = true;
            }

            if (writtenAtLeastOnce) sb.Append('\n');
        }

        // Save regular sections
        for (int i = 0; i < Sections.Count; i++)
        {
            if (!Options.KeepEmptySections && Sections[i].Properties.Count == 0) continue;

            sb.Append($"[{Sections[i].Name}]\n");

            foreach (var property in Sections[i].Properties)
            {
                if (!Options.KeepComments && property.IsComment) continue;

                sb.Append($"{property}\n");
            }

            // If there are more sections to follow, add a newline
            if (i + 1 != Sections.Count)
            {
                sb.Append('\n');
            }
        }

        byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());

        try
        {
            File.WriteAllBytes(path, buffer);
        }
        catch
        {
            SetError(IniError.FailedWrite, path);
            return -1;
        }

        return buffer.LongLength;
    }

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Path);
        Ar.Serialize(ref Sections);
    }
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
    public bool KeepComments { get; set; }
    /// <summary>
    /// Whether to keep empty sections when serializing ini files. If set to false, section headers without any properties will be removed.
    /// </summary>
    public bool KeepEmptySections { get; set; }

    public IniOptions() { }

    public IniOptions(CoalescedOptions options)
    {
        KeepGlobals = options.KeepGlobals;
        KeepComments = options.KeepComments;
        KeepEmptySections = options.KeepEmptySections;
    }
}
