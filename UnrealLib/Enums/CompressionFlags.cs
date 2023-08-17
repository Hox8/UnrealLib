namespace UnrealLib.Enums;

[Flags]
public enum CompressionFlags : uint
{
    /// No compression
    None = 0,

    /// Indicates ZLib compression
    ZLIB = 1 << 0,

    /// Indicates LZO compression
    LZO = 1 << 1,

    /// Indicates LZX compression
    LZX = 1 << 2,

    /// Indicates the compression algorithm should favor smaller output size over speed
    BiasMemory = 1 << 4,

    /// Indicates the compression algorithm should favor faster speed in favor of output size
    BiasSpeed = 1 << 5,

    /// Indicates decompression should not happen on the PS3's SPUs
    ForcePPUDecompressZLib = 1 << 7
}