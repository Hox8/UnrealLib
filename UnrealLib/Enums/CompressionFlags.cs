namespace UnLib.Enums;

[Flags]
public enum CompressionFlags : uint
{
    /// No compression
    None = 0,

    /// Compress with ZLib
    ZLIB = 1 << 0,

    /// Compress with LZO
    LZO = 1 << 1,

    /// Compress with LZX
    LZX = 1 << 2,

    /// Prefer compression that compresses smaller (ONLY VALID FOR COMPRESSION)
    BiasMemory = 1 << 4,

    /// Prefer compression that compresses faster (ONLY VALID FOR COMPRESSION)
    BiasSpeed = 1 << 5,

    /// If this flag is present, decompression will not happen on the SPUs.
    ForcePPUDecompressZLib = 1 << 7
}