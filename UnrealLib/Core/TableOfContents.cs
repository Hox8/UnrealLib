using System.Collections.Generic;
using System.IO;
using static UnrealLib.Core.Globals;

namespace UnrealLib.Core;

public class TableOfContents(Game game)
{
    private readonly List<TableOfContentsEntry> Entries = [];
    private readonly string Dummy = (game == Game.IB1 ? "0 " : "");

    public void AddEntry(string filePath, int uncompressedSize)
    {
        Entries.Add(new TableOfContentsEntry(filePath, uncompressedSize));
    }

    public void UpdateEntry(string filePath, int uncompressedSize)
    {
        foreach (ref var entry in Entries.AsSpan())
        {
            if (string.Equals(entry.Path, filePath, System.StringComparison.OrdinalIgnoreCase))
            {
                entry.UncompressedSize = uncompressedSize;
                return;
            }
        }

        UDebug.Assert(false);
    }

    public long SaveToFile(string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            foreach (var entry in Entries)
            {
                // Write strings separately to avoid unnecessary intermediary allocations

                writer.Write(entry.UncompressedSize);

                // Compressed size. We don't use this for IB, so hardcode it
                writer.Write(" 0 ");

                writer.Write(Dummy);
                writer.Write(entry.Path);

                writer.WriteLine(" 0");
            }

            return writer.BaseStream.Length;
        }
    }
}

/// <summary>
///
/// </summary>
/// <param name="Path"></param>
/// <param name="UncompressedSize"></param>
/// <param name="CompressedSize">This is always 0 for IB. Therefore this field is removed.</param>
/// <param name="StartSector">IB1-only. Always has a value of 0. Therefore, this field is removed.</param>
// Use struct: class is twice as slow and has a higher memory footprint (benchmarked TOC with 700 entries)
public record struct TableOfContentsEntry(string Path, int UncompressedSize/*, int CompressedSize, int StartSector*/);
