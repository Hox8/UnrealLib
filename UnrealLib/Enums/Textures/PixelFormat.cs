namespace UnLib.Enums.Textures;

public enum PixelFormat
{
    Unknown,
    A32B32G32R32F,
    A8R8G8B8,
    G8,
    G16,
    DXT1,
    DXT3,
    DXT5,
    UYVY,
    FloatRGB,

    /// An RGB FP format with platform-specific implementation, for use with render targets
    FloatRGBA,

    /// An RGBA FP format with platform-specific implementation, for use with render targets
    DepthStencil,

    /// A depth+stencil format with platform-specific implementation, for use with render targets
    ShadowDepth,

    /// A depth format with platform-specific implementation, for use with render targets
    FilteredShadowDepth,

    /// A depth format with platform-specific implementation, that can be filtered by hardware
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
    A1
}