using System;

namespace UnrealLib.Enums.Textures;

/// <summary>
/// Bit-field flags that affects storage (e.g. packing, streaming) and other info about a lightmap.
/// </summary>
[Flags]
public enum ELightmapFlags
{
    /// <summary>No flags.</summary>
    LMF_None = 0,
    /// <summary>Lightmap should be placed in a streaming texture.</summary>
    LMF_Streamed = 1 << 0,
    /// <summary>Whether this is a simple lightmap or not (directional coefficient).</summary>
    LMF_SimpleLightmap = 1 << 1
}
