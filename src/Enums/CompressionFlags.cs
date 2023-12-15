using System;

namespace UnrealLib.Enums;

/// <summary>
/// Flags controlling [de]compression.
/// </summary>
[Flags]
public enum CompressionFlags : uint
{
    /// <summary>
    /// Indicates no compression.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates ZLib compression.
    /// </summary>
    ZLib = 1 << 0,

    /// <summary>
    /// Indicates LZO compression.
    /// </summary>
    LZO = 1 << 1,

    /// <summary>
    /// Indicates LZX compression.
    /// </summary>
    LZX = 1 << 2,

    /// <summary>
    /// Indicates the compression algorithm should favor smaller output size over speed.
    /// </summary>
    BiasMemory = 1 << 4,

    /// <summary>
    /// Indicates the compression algorithm should favor faster speed in favor of output size.
    /// </summary>
    BiasSpeed = 1 << 5,

    /// <summary>
    /// Indicates decompression should not happen on PS3 SPUs.
    /// </summary>
    ForcePPUDecompressZLib = 1 << 7
}