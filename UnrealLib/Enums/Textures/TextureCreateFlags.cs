namespace UnrealLib.Enums.Textures;

public enum TextureCreateFlags
{
    /// Texture is encoded in sRGB gamma space.
    SRGB = 1 << 0,

    /// Texture can be used as a resolve target (normally not stored in the texture pool).
    ResolveTargetable = 1 << 1,

    /// Texture is a depth stencil format that can be sampled.
    DepthStencil = 1 << 2,

    /// Texture will be created without a packed miptail.
    NoMipTail = 1 << 3,

    /// Texture will be created with an un-tiled format.
    NoTiling = 1 << 4,

    /// Texture that for a resolve target will only be written to/resolved once.
    WriteOnce = 1 << 5,

    /// Texture that may be updated every frame.
    Dynamic = 1 << 6,

    /// Texture that didn't go through the offline cooker (normally not stored in the texture pool).
    Uncooked = 1 << 7,

    /// Allow silent texture creation failure.
    AllowFailure = 1 << 8,

    /// Disable automatic defragmentation if the initial texture memory allocation fails.
    DisableAutoDefrag = 1 << 9,

    /// Create the texture with automatic -1..1 biasing.
    BiasNormalMap = 1 << 10
}