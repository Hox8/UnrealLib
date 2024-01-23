using System.Collections.Generic;
using UnrealLib.Enums;

namespace UnrealLib.Core.Compression;

/// <summary>
/// Helper class to manage chunks when compressing <see cref="UnrealPackage"/> files.
/// </summary>
internal class ChunkHelper
{
    /// <summary>
    /// The maximum number of bytes any given chunk should store.
    /// </summary>
    public const int MaxChunkSize = 1024 * 1024;

    internal readonly List<FCompressedChunk> CompressedChunks = [];
    internal FCompressedChunk CurrentChunk;

    /// <summary>
    /// Calculates the chunks for an <see cref="UnrealPackage"/> and updates its relevant summary fields.
    /// </summary>
    /// <param name="upk">The <see cref="UnrealPackage"/> to calculate chunks for.</param>
    internal void CalculateChunks(UnrealPackage upk)
    {
        // Skip over package summary--it is never compressed
        upk.Position = upk.Summary.OffsetEnd;

        // Force all tables under the same chunk, even if their total size would otherwise exceed the chunk limit.
        // Much simpler to process and they all share a similar byte layout--high compressibility
        CurrentChunk.UncompressedOffset = (int)upk.Position;
        CurrentChunk.UncompressedSize = (int)(upk.Summary.TotalHeaderSize - upk.Position);

        // Start tallying export data
        foreach (var export in upk.Exports)
        {
            UpdateCurrentChunk(export.SerialSize);
        }

        // Add the final chunk to the list if it isn't empty
        if (CurrentChunk.UncompressedSize != 0)
        {
            FinalizeCurrentChunk(0);
        }

        // Set relevant compression stats
        upk.Summary.PackageFlags |= PackageFlags.StoreCompressed;
        upk.Summary.CompressionFlags = CompressionFlags.ZLib;
        upk.Summary.CompressedChunks = CompressedChunks.ToArray();
    }

    internal void UpdateCurrentChunk(int size)
    {
        // If adding Size to the current chunk would exceed its MaxChunkSize threshold:
        if (CurrentChunk.UncompressedSize + size > MaxChunkSize)
        {
            // Finish current chunk and start a new one, initializing it with Size
            FinalizeCurrentChunk(size);
        }
        else
        {
            // No overflows; add to current chunk like normal
            CurrentChunk.UncompressedSize += size;
        }
    }

    internal void FinalizeCurrentChunk(int size)
    {
        // This chunk is finished, so put it aside in the chunk list
        CompressedChunks.Add(CurrentChunk);

        // Start a new chunk, initializing it with Size.
        // Ignore Size exceeding MaxChunkSize with this step

        int offset = CurrentChunk.UncompressedOffset + CurrentChunk.UncompressedSize;
        CurrentChunk = new FCompressedChunk
        {
            UncompressedOffset = offset,
            UncompressedSize = size
        };
    }
}
