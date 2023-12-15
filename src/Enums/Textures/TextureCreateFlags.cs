using System;

namespace UnrealLib.Enums.Textures;

/// <summary>
/// Flags used for texture creation.
/// </summary>
[Flags]
public enum TextureCreateFlags : uint
{
    /// <summary>
    /// Texture is encoded in sRGB gamma space.
    /// </summary>
    SRGB = 1 << 0,

    /// <summary>
    /// Texture can be used as a resolve target (normally not stored in the texture pool).
    /// </summary>
    ResolveTargetable = 1 << 1,

    /// <summary>
    /// Texture is a depth stencil format that can be sampled.
    /// </summary>
    DepthStencil = 1 << 2,

    /// <summary>
    /// Texture will be created without a packed miptail.
    /// </summary>
    NoMipTail = 1 << 3,

    /// <summary>
    /// Texture will be created with an un-tiled format.
    /// </summary>
    NoTiling = 1 << 4,

    /// <summary>
    /// Texture that for a resolve target will only be written to/resolved once.
    /// </summary>
    WriteOnce = 1 << 5,

    /// <summary>
    /// Texture that may be updated every frame.
    /// </summary>
    Dynamic = 1 << 6,

    /// <summary>
    /// Texture that didn't go through the offline cooker (normally not stored in the texture pool).
    /// </summary>
    Uncooked = 1 << 7,

    /// <summary>
    /// Allow silent texture creation failure.
    /// </summary>
    AllowFailure = 1 << 8,

    /// <summary>
    /// Disable automatic defragmentation if the initial texture memory allocation fails.
    /// </summary>
    DisableAutoDefrag = 1 << 9,

    /// <summary>
    /// Create the texture with automatic -1..1 biasing.
    /// </summary>
    BiasNormalMap = 1 << 10,

    /// <summary>
    /// Create the texture with the flag that allows mip generation later, only applicable to D3D11.
    /// </summary>
    GenerateMipCapable = 1 << 11,

    /// <summary>
    /// A resolve texture that can be presented to screen.
    /// </summary>
    Presentable = 1 << 12,

    /// <summary>
    /// Texture is used as a resolvetarget for a multisampled surface. (Required for multisampled depth textures).
    /// </summary>
    Multisample = 1 << 13,

    /// <summary>
    /// Texture should disable any filtering (NGP only, and is hopefully temporary).
    /// </summary>
    PointFilterNGP = 1 << 14,

    /// <summary>
    /// This is a targetable resolve texture for a TargetSurfCreate_HighPerf, so should be fast to read if possible.
    /// </summary>
    HighPerf = 1 << 15,

    /// <summary>
    /// Texture has been created with an explicit address.
    /// </summary>
    ExplicitAddress = 1 << 16
}