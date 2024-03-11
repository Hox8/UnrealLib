using NetEscapades.EnumGenerators;

namespace UnrealLib.Enums.Textures;

/// <summary>
/// Flags describing texture format.
/// </summary>
[EnumExtensions]
public enum EPixelFormat : uint
{
    PF_Unknown = 0,

    /// <summary>A 128-bit uncompressed floating-point format that uses 32 bits for each channel (alpha, blue, green, and red).</summary>
    PF_A32B32G32R32F,

    /// <summary>A 32-bit uncompressed ARGB pixel format, with alpha, that uses 8 bits per channel.</summary>
    PF_A8R8G8B8,

    /// <summary>An 8-bit uncompressed grayscale format which uses a single channel.</summary>
    PF_G8,

    /// <summary>A 16-bit uncompressed grayscale format which uses a single channel.</summary>
    PF_G16,

    /// <summary>A 4-bit compressed RGBA pixel format with support for 1-bit alpha textures.</summary>
    PF_DXT1,

    /// <summary>A 4-bit compressed RGBA pixel format with support for 4-bit alpha textures.</summary>
    /// <remarks>Alpha channel is suitable for high-frequency details, such as masks. Unused by UE3.</remarks>
    PF_DXT3,

    /// <summary>A 4-bit compressed RGBA pixel format with support for 4-bit alpha textures.</summary>
    /// <remarks>Alpha channel is suitable for low-frequency details, such as clouds.</remarks>
    PF_DXT5,

    /// <summary>A 16-bit YUV format, using luminance and chrominance over sRGB.</summary>
    /// <remarks>Used for video.</remarks>
    PF_UYVY,


    PF_FloatRGB,
    PF_FloatRGBA,
    PF_DepthStencil,
    PF_ShadowDepth,
    PF_FilteredShadowDepth,
    PF_R32F,
    PF_G16R16,
    PF_G16R16F,
    PF_G16R16F_FILTER,
    PF_G32R32F,
    PF_A2B10G10R10,
    PF_A16B16G16R16,
    PF_D24,
    PF_R16F,
    PF_R16F_FILTER,
    PF_BC5,

    /// <summary>A 16-bit uncompressed bump-map format that uses 8 bits each for U and V data.</summary>
    /// <remarks>Used for uncompressed normal maps within UE3.</remarks>
    PF_V8U8,

    /// <summary>(Probably) A 1-bit texture format used for physical material masks.</summary>
    PF_A1,

    /// <remarks>Not applicable to IB1.</remarks>
    PF_FloatR11G11B10,

    /// <remarks>Not applicable to IB1.</remarks>
    PF_A4R4G4B4
}