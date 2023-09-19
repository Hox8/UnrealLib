namespace UnrealLib.Enums.Textures;

/// <summary>
/// Flags describing texture format.
/// </summary>
public enum PixelFormat : uint
{
    Unknown = 0,
    A32B32G32R32F,
    A8R8G8B8,
    G8,
    G16,
    DXT1,
    DXT3,
    DXT5,
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
    V8U8,
    A1,

    /// <remarks>
    /// Not applicable to IB1.
    /// </remarks>
    FloatR11G11B10,

    /// <remarks>
    /// Not applicable to IB1.
    /// </remarks>
    A4R4G4B4
}