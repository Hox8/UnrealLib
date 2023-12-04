using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnrealLib.Enums;
using UnrealLib.Interfaces;
using Ionic.Zlib;

namespace UnrealLib.Core;

public class FUntypedBulkData : ISerializable
{
    private BulkDataFlags BulkDataFlags;
    // Uncompressed size.
    private int ElementCount;
    // Compressed size.
    private int BulkDataSizeOnDisk;
    private int BulkDataOffsetInFile;

    //private BulkDataFlags SavedBulkDataFlags;
    //private int SavedSavedElementCount;
    //private int SavedBulkDataOffsetInFile;
    //private int SavedBulkDataSizeOnDisk;

    private byte[] BulkData;

    // Use an array pool?

    #region Accessors

    public bool IsStoredInSeparateFile => (BulkDataFlags & BulkDataFlags.StoreInSeparateFile) != 0;
    public bool IsStoredCompressed => (BulkDataFlags & BulkDataFlags.SerializeCompressed) != 0;
    public bool ContainsData => BulkDataSizeOnDisk > 0;
    public int Count => ElementCount;
    public ReadOnlySpan<byte> Data => BulkData;

    #endregion

    public void Serialize(UnrealStream stream)
    {
        Debug.Assert(stream.IsLoading);

        stream.Serialize(ref BulkDataFlags);
        stream.Serialize(ref ElementCount);
        stream.Serialize(ref BulkDataSizeOnDisk);
        stream.Serialize(ref BulkDataOffsetInFile);

        // If we contain inlined data, skip over it. We can read it in later if needed.
        if (ContainsData && !IsStoredInSeparateFile)
        {
            stream.Position += BulkDataSizeOnDisk;
        }
    }

    public void RemoveBulkData()
    {
        BulkData = null;
        ElementCount = 0;
    }

    public void ReadData(UnrealStream stream)
    {
        // 1. Read data / allocate uninitialized array
        // 2. Decompress data if compressed (ZLIB, LZX etc)

        stream.Position = BulkDataOffsetInFile;

        BulkData = GC.AllocateUninitializedArray<byte>(ElementCount);

        if (IsStoredCompressed)
        {
            if ((BulkDataFlags & BulkDataFlags.SerializeCompressedZLIB) != 0)
            {
                DecompressZLib(stream);
            }
            else
            {
                // iOS only supports ZLib. I have no plans to support other forms of compression.
                throw new NotImplementedException("ZLib is the only supported compression method");
            }
        }
        else
        {
            stream.ReadExactly(BulkData);
            return;
        }
    }

    private void DecompressZLib(UnrealStream inputStream)
    {
        int packageTag = 0, chunkSize = 0;
        int compressedSize = 0, uncompressedSize = 0;

        // Read in package tag
        inputStream.Serialize(ref packageTag);
        if ((uint)packageTag != Globals.PackageTag)
        {
            throw new Exception("Invalid package tag!");
        }

        // Read in chunk size? Can this change?
        inputStream.Serialize(ref chunkSize);
        if (chunkSize != Globals.CompressionChunkSize)
        {
            throw new Exception("Unexpected compression chunk size!");
        }

        // Read in [un]compressed sizes
        inputStream.Serialize(ref compressedSize);
        inputStream.Serialize(ref uncompressedSize);

        // Determine chunk count based on uncompressed size
        int totalChunkCount = (uncompressedSize + Globals.CompressionChunkSize - 1) / Globals.CompressionChunkSize;

        // Allocate and read totalChunkCount compression chunk infos
        var chunks = new FCompressedChunkInfo[totalChunkCount];
        inputStream.ReadExactly(MemoryMarshal.Cast<FCompressedChunkInfo, byte>(chunks));

        // Read in and inflate all chunks
        int bytesRead = 0;
        for (int i = 0; i < chunks.Length; i++)
        {
            // This works, but not how I want it to. @TODO
            using var zlibstream = new ZlibStream(inputStream, CompressionMode.Decompress, true);
            zlibstream.BufferSize = chunks[i].CompressedSize;   // Read() reads BufferSize bytes regardless of length passed

            bytesRead += zlibstream.Read(BulkData.AsSpan(bytesRead, chunks[i].UncompressedSize));
        }
    }
}
