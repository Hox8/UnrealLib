using System;
using UnrealLib.Core;
using UnrealLib.Enums.Textures;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Textures;

public partial class UTexture(FObjectExport? export = null) : USurface(export), IDisposable
{
    #region UProperties

    [UProperty] public bool SRGB = true;
    [UProperty] public bool RGBE;

    [UProperty] public bool bIsSourceArtUncompressed;

    [UProperty] public bool CompressionNoAlpha;
    [UProperty] public bool CompressionNone;
    [UProperty] public bool CompressionNoMipmaps;           // DEPRECATED
    [UProperty] public bool CompressionFullDynamicRange;
    [UProperty] public bool DeferCompression;

    [UProperty] public bool NeverStream;

    /// <summary>
    /// When TRUE, the alpha channel of mip-maps and the base image are dithered for smooth LOD transitions.
    /// </summary>
    [UProperty] public bool bDitherMipMapAlpha;

    /// <summary>If TRUE, the color border pixels are preserved by mipmap generation.</summary>
    /// <remarks>One flag per color channel.</remarks>
    [UProperty] public bool bPreserveBorderR, bPreserveBorderG, bPreserveBorderB, bPreserveBorderA;

    /// <summary>If TRUE, the RHI texture will be created using TexCreate_NoTiling.</summary>
    [UProperty] public bool bNoTiling;

    /// <summary>
    /// For DXT1 textures, setting this will cause the texture to be twice the size, but better looking, on iPhone.
    /// </summary>
    [UProperty] public bool bForcePVRTC4;

    [UProperty] public float[] UnpackMin = new float[4];
    [UProperty] public float[] UnpackMax = [1.0f, 1.0f, 1.0f, 1.0f];

    [UProperty] public TextureCompressionSettings CompressionSettings = TextureCompressionSettings.TC_Default;

    /// <summary>The texture filtering mode to use when sampling this texture.</summary>
    [UProperty] public TextureFilter Filter = TextureFilter.TF_Linear;

    /// <summary>Texture group this texture belongs to for LOD bias.</summary>
    [UProperty] public TextureGroup LODGroup;

    /// <summary>A bias to the index of the top mip level to use.</summary>
    [UProperty] public int LODBias;

    /// <summary>Number of mip-levels to use for cinematic quality.</summary>
    [UProperty] public int NumCinematicMipLevels;

    /// <summary>Path to the resource used to construct this texture.</summary>
    [UProperty] public string SourceFilePath;
    /// <summary>Date/Time-stamp of the file from the last import.</summary>
    [UProperty] public string SourceFileTimestamp;

    /// <summary>Unique ID for this material, used for caching during distributed lighting.</summary>
    [UProperty] public FGuid LightingGuid;

    /// <summary>Static texture brightness adjustment (scales HSV value.)</summary>
    [UProperty] public float AdjustBrightness = 1.0f;
    /// <summary>Static texture curve adjustment (raises HSV value to the specified power.)</summary>
    [UProperty] public float AdjustBrightnessCurve = 1.0f;
    /// <summary>Static texture "vibrance" adjustment (0 - 1) (HSV saturation algorithm adjustment.)</summary>
    [UProperty] public float AdjustVibrance;
    /// <summary>Static texture saturation adjustment (scales HSV saturation.)</summary>
    [UProperty] public float Saturation = 1.0f;
    /// <summary>Static texture RGB curve adjustment (raises linear-space RGB color to the specified power.)</summary>
    [UProperty] public float AdjustRGBCurve = 1.0f;
    /// <summary>Static texture hue adjustment (0 - 360) (offsets HSV hue by value in degrees.)</summary>
    [UProperty] public float AdjustHue;

    /// <summary>
    /// Internal LOD bias already applied by the texture format (eg TC_NormalMapUncompressed).
    /// Used to adjust MinLODMipCount and MaxLODMipCount in CalculateLODBias .
    /// </summary>
    [UProperty] public int InternalFormatLODBias;

    /// <summary>
    /// Per asset specific setting to define the mip-map generation properties like sharpening and kernel size.
    /// </summary>
    [UProperty] public TextureMipGenSettings MipGenSettings;

    #endregion

    public FUntypedBulkData SourceArt;

    /// <summary>The texture's resource.</summary>
    // public FTextureResource Resource;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref SourceArt);
    }

    public virtual void Dispose()
    {
        SourceArt?.Dispose();

        GC.SuppressFinalize(this);
    }

    public static void ParseProperties(UTexture texture, UnrealArchive Ar)
    {

    }
}
