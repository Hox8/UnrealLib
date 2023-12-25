using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnrealLib.Enums;

namespace UnrealLib.Core;

/// <summary>
/// A table of contents class representing the expected sizes of all files intended to be loaded by the game during runtime.
/// </summary>
public class FTableOfContents(Game game)
{
    /// <summary>
    /// A list containing all file entries within this TOC.
    /// </summary>
    private List<FTOCEntry> Entries = new();

    /// <summary>
    /// IB1 TOCs serialize an extra field (DVD starting sector; 5 fields instead of 4), which we need to account for.
    /// </summary>
    private readonly int ValidLength = game is Game.IB1 ? 5 : 4;

    /// <summary>
    /// Represents a dummy StartSector string. IB1 did not use DVDs, so this was never non-zero.
    /// </summary>
    private readonly string DummyStartSector = game is Game.IB1 ? "0 " : "";

    /// <summary>
    /// Creates a new TOC instance and reads an existing TOC file into it.
    /// </summary>
    public static FTableOfContents FromExistingTOC(string tocPath, Game game)
    {
        var toc = new FTableOfContents(game);

        foreach (var line in File.ReadLines(tocPath))
        {
            string[] sub = line.Split(' ', System.StringSplitOptions.TrimEntries);

            Debug.Assert((sub.Length == 5 && sub[2] == "0") || sub.Length == toc.ValidLength); // IB1's StartSectors should never be non-zero.
            Debug.Assert(sub[1] == "0"); // Uncompressed size should never be non-zero.

            // Ignore arbitrarily-sized entries.
            if (sub.Length != toc.ValidLength) continue;

            // Add entry if it isn't already present.
            if (!toc.TryGetEntry(sub[^2], out var entry))
            {
                entry = new() { Filepath = sub[^2] };
                int.TryParse(sub[0], out entry.FileSize);
                int.TryParse(sub[1], out entry.UncompressedFileSize);
                toc.Entries.Add(entry);
            }
        }

        return toc;
    }

    /// <summary>
    /// Serializes all TOC entries to a text file at the given path.
    /// </summary>
    /// <param name="outPath">Path to write the TOC file to. Existing files will be overwritten.</param>
    public void Save(string outPath)
    {
        using (var fs = File.Create(outPath))
        {
            foreach (var entry in Entries)
            {
                var line = $"{entry.FileSize} {entry.UncompressedFileSize} {DummyStartSector}{entry.Filepath} 0\r\n";
                fs.Write(Encoding.ASCII.GetBytes(line));
            }
        }
    }

    /// <summary>
    /// Updates an existing entry's sizes. If an entry is not found, one is added.
    /// </summary>
    public void UpdateEntry(string filename, int fileSize, int uncompressedSize)
    {
        if (!TryGetEntry(filename, out var entry))
        {
            entry = new() { Filepath = filename };
            Entries.Add(entry);
        }

        entry.FileSize = fileSize;
        entry.UncompressedFileSize = uncompressedSize;
    }

    /// <summary>
    /// Adds a new FTOCEntry to the table without checking for prior existence.
    /// Can be helpful for scenarios where the table is populated externally.
    /// </summary>
    public void AddEntry(string filename, int fileSize, int uncompressedSize) =>
        Entries.Add(new FTOCEntry() { Filepath = filename, FileSize = fileSize, UncompressedFileSize = uncompressedSize});

    public bool TryGetEntry(string fileName, out FTOCEntry outEntry)
    {
        foreach (var entry in Entries)
        {
            if (entry.Filepath.Equals(fileName, System.StringComparison.OrdinalIgnoreCase))
            {
                outEntry = entry;
                return true;
            }
        }

        outEntry = null;
        return false;
    }
}

public record FTOCEntry
{
    /// <summary>
    /// Path of the file on disk, e.g. '..\SwordGame\CookedIPhone\SwordGame.xxx'
    /// </summary>
    public string Filepath;
    /// <summary>
    /// Size on disk (compressed size if it was compressed).
    /// </summary>
    public int FileSize;
    /// <summary>
    /// Uncompressed size on disk, 0 if not compressed.
    /// </summary>
    public int UncompressedFileSize;
    /// <summary>
    /// Starting DVD sector of file (always 0 for IB1).
    /// </summary>
    /// <remarks>Deprecated; used only by IB1. This is not serialized for other games!</remarks>
    // public int StartSector;      // If this is always 0, we don't really need to have this as a field
}