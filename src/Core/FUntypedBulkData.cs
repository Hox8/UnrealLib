using System;
using System.Diagnostics;
using UnrealLib.Core.Compression;
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

    public int Offset => BulkDataOffsetInFile;
    public int Size => BulkDataSizeOnDisk;

    #endregion

    public FUntypedBulkData() { }
    public FUntypedBulkData(BulkDataFlags flags, int uncompressedSize, int sizeInFile, int offsetInFile)
    {
        BulkDataFlags = flags;
        ElementCount = uncompressedSize;
        BulkDataSizeOnDisk = sizeInFile;
        BulkDataOffsetInFile = offsetInFile;
    }

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

    /// <summary>
    /// Reads the bulk data from disk into memory. Required stream is detailed in its parent UObject default properties,
    /// commonly external texture file caches.
    /// </summary>
    /// <param name="Ar">An UnrealArchive containing the required data.</param>
    public void ReadData(UnrealArchive Ar)
    {
        // If stored in separate file, how to write? Open and close? Find a shared stream?
        if (!Ar.IsLoading) throw new NotImplementedException();

        if (!ContainsData) return;

        Ar.Position = BulkDataOffsetInFile;

        if (IsStoredCompressed)
        {
            if (!ZLibCompressed) throw new Exception("ZLib is the only supported compression scheme");

            if (Ar.IsLoading)
            {
                // This is not a good idea. Array should be sized and passed in beforehand
                BulkData = GC.AllocateUninitializedArray<byte>(ElementCount);
                CompressionManager.Decompress(Ar._buffer, BulkData);
            }
            else
            {
            }
        }
        else
        {
            Ar.Serialize(ref BulkData, ElementCount);
        }
    }
}
