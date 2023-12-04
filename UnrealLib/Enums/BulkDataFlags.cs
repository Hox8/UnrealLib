using System;

namespace UnrealLib.Enums;

/// <summary>
/// Flags serialized with the bulk data.
/// </summary>
[Flags]
public enum BulkDataFlags : uint
{
    /// <summary>Empty flag set.</summary>
    None = 0,
    /// <summary>If set, payload is [going to be] stored in separate file.</summary>
    StoreInSeparateFile = 1 << 0,
    /// <summary>If set, payload should be [un]compressed using ZLIB during serialization.</summary>
    SerializeCompressedZLIB = 1 << 1,
    /// <summary>Force usage of SerializeElement over bulk serialization.</summary>
    ForceSingleElementSerialization = 1 << 2,
    /// <summary>Bulk data is only used once at runtime in the game.</summary>
    SingleUse = 1 << 3,
    /// <summary>If set, payload should be [un]compressed using LZO during serialization.</summary>
    SerializeCompressedLZO = 1 << 4,
    /// <summary>Bulk data won't be used and doesn't need to be loaded.</summary>
    Unused = 1 << 5,
    /// <summary>If specified, only payload data will be written to archive.</summary>
    StoreOnlyPayload = 1 << 6,
    /// <summary>If set, payload should be [un]compressed using LZX during serialization.</summary>
    SerializeCompressedLZX = 1 << 7,
    /// <summary>Flag to check if either compression mode is specified.</summary>
    SerializeCompressed = (SerializeCompressedZLIB | SerializeCompressedLZO | SerializeCompressedLZX),
};
