using System;

namespace UnrealLib.Enums.Textures;

/// <summary>
/// Bit-field flags that affects storage (e.g. packing, streaming) and other info about a lightmap.
/// </summary>
[Flags]
public enum LightmapFlags
{
    /// <summary>No flags.</summary>
    None = 0,
    /// <summary>Lightmap should be placed in a streaming texture.</summary>
    Streamed = 1 << 0,
    /// <summary>Whether this is a simple lightmap or not (directional coefficient).</summary>
    SimpleLightmap = 1 << 1
}
