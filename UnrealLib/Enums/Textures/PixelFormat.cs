namespace UnrealLib.Enums.Textures;

/// <summary>
/// Flags describing texture format.
/// </summary>
public enum PixelFormat : uint
{
    Unknown = 0,

    /// <summary>A 128-bit uncompressed floating-point format that uses 32 bits for each channel (alpha, blue, green, and red).</summary>
    A32B32G32R32F,

    /// <summary>A 32-bit uncompressed ARGB pixel format, with alpha, that uses 8 bits per channel.</summary>
    A8R8G8B8,

    /// <summary>An 8-bit uncompressed grayscale format which uses a single channel.</summary>
    G8,

    /// <summary>A 16-bit uncompressed grayscale format which uses a single channel.</summary>
    G16,

    /// <summary>A 4-bit compressed RGBA pixel format with support for 1-bit alpha textures.</summary>
    DXT1,

    /// <summary>A 4-bit compressed RGBA pixel format with support for 4-bit alpha textures.</summary>
    /// <remarks>Alpha channel is suitable for high-frequency details, such as masks. Unused by UE3.</remarks>
    DXT3,

    /// <summary>A 4-bit compressed RGBA pixel format with support for 4-bit alpha textures.</summary>
    /// <remarks>Alpha channel is suitable for low-frequency details, such as clouds.</remarks>
    DXT5,

    /// <summary>A 16-bit YUV format, using luminance and chrominance over sRGB.</summary>
    /// <remarks>Used for video.</remarks>
    UYVY,


    FloatRGB,
    FloatRGBA,
    DepthStencil,
    ShadowDepth,
    FilteredShadowDepth,
    R32F,
    G16R16,
    G16R16F,
    G16R16F_FILTER,
    G32R32F,
    A2B10G10R10,
    A16B16G16R16,
    D24,
    R16F,
    R16F_FILTER,
    BC5,

    /// <summary>A 16-bit uncompressed bump-map format that uses 8 bits each for U and V data.</summary>
    /// <remarks>Used for uncompressed normal maps within UE3.</remarks>
    V8U8,

    /// <summary>(Probably) A 1-bit texture format used for physical material masks.</summary>
    A1,

    /// <remarks>Not applicable to IB1.</remarks>
    FloatR11G11B10,

    /// <remarks>Not applicable to IB1.</remarks>
    A4R4G4B4
}