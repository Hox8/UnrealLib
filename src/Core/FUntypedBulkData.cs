using Ionic.Zlib;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FUntypedBulkData : ISerializable
{
    #region Serialized members

    /// <summary>Serialized flags for bulk data.</summary>
    internal BulkDataFlags BulkDataFlags = BulkDataFlags.None;
    /// <summary>Uncompressed size?</summary>
    internal int ElementCount;
    /// <summary>Size of bulk data on disk.</summary>
    internal int BulkDataSizeOnDisk;
    /// <summary>Offset of bulk data within file.</summary>
    internal int BulkDataOffsetInFile;

    private byte[] BulkData;

    #endregion

    #region Accessors

    public bool IsUnused => (BulkDataFlags & BulkDataFlags.Unused) != 0;
    public bool IsStoredInSeparateFile => (BulkDataFlags & BulkDataFlags.StoreInSeparateFile) != 0;
    public bool IsStoredCompressed => (BulkDataFlags & BulkDataFlags.SerializeCompressed) != 0;
    public bool ZLibCompressed => (BulkDataFlags & BulkDataFlags.SerializeCompressedZLIB) != 0;
    public bool ContainsData => BulkDataSizeOnDisk > 0;
    public int Count => ElementCount;
    public ReadOnlySpan<byte> DataView => BulkData;
    public Span<byte> Data => BulkData;

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        Debug.Assert(Ar.IsLoading);

        Ar.Serialize(ref BulkDataFlags);
        Ar.Serialize(ref ElementCount);
        Ar.Serialize(ref BulkDataSizeOnDisk);
        Ar.Serialize(ref BulkDataOffsetInFile);

        // Skip over any inlined bulk data.
        if (!IsStoredInSeparateFile)
        {
            Ar.Position += BulkDataSizeOnDisk;
        }
    }

    public void RemoveBulkData()
    {
        BulkData = null;
        ElementCount = 0;
    }

    /// <summary>
    /// Reads the bulk data from disk into memory. Required stream is detailed in its parent UObject default properties,
    /// commonly external texture file caches.
    /// </summary>
    /// <param name="Ar">An UnrealArchive containing the required data.</param>
    public void ReadData(UnrealArchive Ar)
    {
        if (!ContainsData) return;

        Ar.Position = BulkDataOffsetInFile;

        // Allocate buffer for uncompressed data.
        BulkData = GC.AllocateUninitializedArray<byte>(ElementCount);

        if (IsStoredCompressed)
        {
            if (!ZLibCompressed)
            {
                // iOS only supports ZLib. I have no plans to support other forms of compression.
                throw new NotImplementedException("ZLib is the only supported compression method");
            }
            
            DecompressZLib(Ar);
        }
        else
        {
            Ar.ReadExactly(BulkData);
            return;
        }
    }

    public static void DecompressZLibStatic(UnrealArchive Ar)
    {
        var buffer = Ar.GetBufferRaw();

        using var zlibstream = new ZlibStream(Ar, CompressionMode.Decompress, true);

        int bytesRead = zlibstream.Read(buffer);
    }

    // CRUDE ZLib impl.
    // Very inefficient; look into pooling and make this method static so as to not lock behind FUntypedBulkData
    private void DecompressZLib(UnrealArchive Ar)
    {
        int packageTag = 0, chunkSize = 0;
        int compressedSize = 0, uncompressedSize = 0;

        // Read in package tag
        Ar.Serialize(ref packageTag);
        if ((uint)packageTag != Globals.PackageTag)
        {
            throw new Exception("Invalid package tag!");
        }

        // Read in chunk size? Can this change?
        Ar.Serialize(ref chunkSize);
        if (chunkSize != Globals.CompressionChunkSize)
        {
            throw new Exception("Unexpected compression chunk size!");
        }

        // Read in [un]compressed sizes
        Ar.Serialize(ref compressedSize);
        Ar.Serialize(ref uncompressedSize);

        // Determine chunk count based on uncompressed size
        int totalChunkCount = (uncompressedSize + Globals.CompressionChunkSize - 1) / Globals.CompressionChunkSize;

        // Allocate and read totalChunkCount compression chunk infos
        var chunks = new FCompressedChunkInfo[totalChunkCount];
        Ar.ReadExactly(MemoryMarshal.Cast<FCompressedChunkInfo, byte>(chunks));

        // Read in and inflate all chunks
        int bytesRead = 0;
        for (int i = 0; i < chunks.Length; i++)
        {
            // This works, but not how I want it to. @TODO
            using var zlibstream = new ZlibStream(Ar, CompressionMode.Decompress, true);
            zlibstream.BufferSize = chunks[i].CompressedSize;   // Read() reads BufferSize bytes regardless of length passed?

            bytesRead += zlibstream.Read(BulkData.AsSpan(bytesRead, chunks[i].UncompressedSize));
        }
    }
}
