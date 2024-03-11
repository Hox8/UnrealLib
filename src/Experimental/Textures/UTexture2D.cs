using System;
using System.Diagnostics;
using UnrealLib.Core;
using UnrealLib.Enums.Textures;
using UnrealLib.Interfaces;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Textures;

public record struct FTexture2DMipMap : ISerializable
{
    public FUntypedBulkData Data;
    public int SizeX, SizeY;

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Data);
        Ar.Serialize(ref SizeX);
        Ar.Serialize(ref SizeY);

        Debug.Assert(SizeX > 0 && SizeY > 0);
    }
}

public partial class UTexture2D(FObjectExport? export = null) : UTexture(export), IDisposable
{
    #region UProperties

    /// <summary>The width of the texture.</summary>
    [UProperty] public int SizeX;
    /// <summary>The height of the texture.</summary>
    [UProperty] public int SizeY;

    /// <summary>The original width of the texture source art we imported from.</summary>
    [UProperty] public int OriginalSizeX;
    /// <summary>The original height of the texture source art we imported from.</summary>
    [UProperty] public int OriginalSizeY;

    /// <summary>The format of the texture data.</summary>
    [UProperty] public EPixelFormat Format;

    /// <summary>The addressing mode to use for the X axis.</summary>
    [UProperty] public TextureAddress AddressX;
    /// <summary>The addressing mode to use for the Y axis.</summary>
    [UProperty] public TextureAddress AddressY;

    /// <summary>
    /// Whether this texture editor only and should not be cooked into the final packages.
    /// </summary>
    [UProperty] public bool IsEditorOnly;

    /// <summary>Global/ serialized version of ForceMiplevelsToBeResident.</summary>
    [UProperty] public bool bGlobalForceMipLevelsToBeResident;

    /// <summary>Allows texture to be a source for Texture2DComposite.</summary>
    /// <remarks>Will NOT be available for use in rendering!</remarks>
    [UProperty] public bool bIsCompositingSource;

    /// <summary>Whether the texture has been painted in the editor.</summary>
    [UProperty] public bool bHasBeenPaintedInEditor;

    /// <summary>
    /// Name of texture file cache texture mips are stored in, NAME_None if it is not part of one.
    /// </summary>
    [UProperty] public FName TextureFileCacheName;

    /// <summary>
    /// Number of mips to remove when recompressing (does not work with TC_NormalmapUncompressed).
    /// </summary>
    [UProperty] public int MipsToRemoveOnCompress;

    /// <summary>
    /// Data formatted only for 1 bit textures which are CPU based and never allocate GPU Memory.
    /// </summary>
    [UProperty] private byte[] SystemMemoryData;

    /// <summary>
    /// Keep track of the first mip level stored in the packed miptail.
    /// It's set to highest mip level if no there's no packed miptail.
    /// </summary>
    [UProperty] public int MipTailBaseIdx;

    /// <summary>Keep track of first mip level used for ResourceMem creation.</summary>
    [UProperty] private int FirstResourceMemMip;

    #endregion

    /// <summary>The texture's mip-map data.</summary>
    public FTexture2DMipMap[] Mips;
    /// <summary>Cached PVRTC compressed texture data.</summary>
    public FTexture2DMipMap[] CachedPVRTCMips;
    /// <summary>Cached ATITC compressed texture data.</summary>
    public FTexture2DMipMap[] CachedATITCMips;
    /// <summary>Cached ETC compressed texture data.</summary>
    public FTexture2DMipMap[] CachedETCMips;

    /// <summary>The size that the Flash compressed texture data was cached at.</summary>
    public int CachedFlashMipsMaxResolution;
    /// <summary>Cached Flash compressed texture data.</summary>
    public FUntypedBulkData CachedFlashMips;

    /// <summary>
    /// ID generated whenever the texture is changed so that its bulk data can be updated in the TextureFileCache during cook.
    /// </summary>
    public FGuid TextureFileCacheGuid;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref Mips);
        Ar.Serialize(ref TextureFileCacheGuid);

        Ar.Serialize(ref CachedPVRTCMips);

        if (Ar.Version >= 857)
        {
            Ar.Serialize(ref CachedFlashMipsMaxResolution);

            Ar.Serialize(ref CachedATITCMips);

            Ar.Serialize(ref CachedFlashMips);
        }

        if (Ar.Version >= 864)
        {
            Ar.Serialize(ref CachedETCMips);
        }
    }

    #region Helpers

    /// <summary>
    /// Gets the highest mip containing data.
    /// </summary>
    public bool GetFirstValidPVRTCMip(out FTexture2DMipMap outMip)
    {
        foreach (var mip in Mips)
        {
            if (mip.Data.ContainsData)
            {
                outMip = mip;
                return true;
            }
        }

        outMip = default;
        return false;
    }

    public bool IsCompressed() => Format is (EPixelFormat.PF_DXT1 or EPixelFormat.PF_DXT3 or EPixelFormat.PF_DXT5 or EPixelFormat.PF_BC5);

    #endregion

    public override void Dispose()
    {
        if (Mips is not null)
        {
            foreach (var mip in Mips) mip.Data.Dispose();
        }

        if (CachedPVRTCMips is not null)
        {
            foreach (var mip in CachedPVRTCMips) mip.Data.Dispose();
        }

        if (CachedATITCMips is not null)
        {
            foreach (var mip in CachedATITCMips) mip.Data.Dispose();
        }

        if (CachedETCMips is not null)
        {
            foreach (var mip in CachedETCMips) mip.Data.Dispose();
        }

        CachedFlashMips?.Dispose();

        GC.SuppressFinalize(this);
    }
}