using System;
using System.Buffers;
using System.Diagnostics;
using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FUntypedBulkData : ISerializable, IDisposable
{
    #region Serialized members

    /// <summary>
    /// Flags for bulk data.
    /// </summary>
    internal BulkDataFlags BulkDataFlags;
    /// <summary>
    /// Number of elements in array.
    /// </summary>
    internal int ElementCount;
    /// <summary>
    /// Size of bulk data on disk, in bytes. Affected by compression.
    /// </summary>
    internal int BulkDataSizeOnDisk;
    /// <summary>
    /// Bulk data offset in file.
    /// </summary>
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

    public int Offset { get => BulkDataOffsetInFile; set => BulkDataOffsetInFile = value; }
    public int Size => BulkDataSizeOnDisk;

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        if (!Ar.IsLoading && !IsStoredInSeparateFile)
        {
            BulkDataOffsetInFile = (int)Ar.Position + (sizeof(int) * 4);
        }

        Ar.Serialize(ref BulkDataFlags);
        Ar.Serialize(ref ElementCount);
        Ar.Serialize(ref BulkDataSizeOnDisk);
        Ar.Serialize(ref BulkDataOffsetInFile);

        if (!IsStoredInSeparateFile)
        {
            // Check offset is valid
            ArgumentOutOfRangeException.ThrowIfNotEqual(Ar.Position, BulkDataOffsetInFile);

            // Read in inlined data
            if (ContainsData)
            {
                if (!Ar.IsLoading)
                {
                    Ar.Write(BulkData, 0, BulkDataSizeOnDisk);
                }
                else
                {
                    BulkData = ArrayPool<byte>.Shared.Rent(BulkDataSizeOnDisk);
                    Ar.ReadExactly(BulkData, 0, BulkDataSizeOnDisk);
                }

                // Ensure we've written / read the right amount of data
                Debug.Assert(Ar.Position == BulkDataOffsetInFile + BulkDataSizeOnDisk);
            }
        }
    }

    public void ReadData(UnrealArchive Ar)
    {
        if (BulkData is not null || !ContainsData) return;

        Ar.Position = BulkDataOffsetInFile;

        if (ZLibCompressed)
        {
            BulkData = ArrayPool<byte>.Shared.Rent(ElementCount);
            Compression.CompressionManager.Decompress(Ar._buffer, BulkData);
        }
        else
        {
            BulkData = ArrayPool<byte>.Shared.Rent(BulkDataSizeOnDisk);
            Ar.ReadExactly(BulkData);
        }             
    }

    public void Dispose()
    {
        if (BulkData is not null)
        {
            ArrayPool<byte>.Shared.Return(BulkData);
        }

        GC.SuppressFinalize(this);
    }
}
