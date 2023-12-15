using System.Diagnostics;
using UnrealLib.Core;
using UnrealLib.Enums.Textures;
using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.Interfaces;

namespace UnrealLib.Experimental.Textures;

public class UTexture2D(FObjectExport export) : UTexture(export)
{
    #region Structs

    public struct Texture2DMipMap : ISerializable
    {
        public FUntypedBulkData Data;
        public int SizeX;
        public int SizeY;

        public void Serialize(UnrealArchive Ar)
        {
            // Read in data header but not the actual data itself. Skips over it if inlined
            Ar.Serialize(ref Data);

            Ar.Serialize(ref SizeX);
            Ar.Serialize(ref SizeY);
        }
    }

    #endregion

    #region Properties

    /** The texture's mip-map data.												*/
    public Texture2DMipMap[] Mips;

    /** Cached PVRTC compressed texture data									*/
    // public Texture2DMipMap[] CachedPVRTCMips;
    public Texture2DMipMap[] CachedPVRTCMips;

    /** Cached ATITC compressed texture data									*/
    // public Texture2DMipMap[] CachedATITCMips;

    /** The size that the Flash compressed texture data was cached at 			*/
    // public int CachedFlashMipsMaxResolution;

    /** Cached Flash compressed texture data									*/
    // public TextureMipBulkData[] CachedFlashMips;

    /** The width of the texture.												*/
    public int SizeX;

    /** The height of the texture.												*/
    public int SizeY;

    /** The original width of the texture source art we imported from.			*/
    public int OriginalSizeX;

    /** The original height of the texture source art we imported from.			*/
    public int OriginalSizeY;

    /** The format of the texture data.											*/
    public PixelFormat Format = PixelFormat.DXT1;

    /** The addressing mode to use for the X axis.								*/
    public TextureAddress AddressX = TextureAddress.Wrap;

    /** The addressing mode to use for the Y axis.								*/
    public TextureAddress AddressY = TextureAddress.Wrap;

    /** Global/ serialized version of ForceMiplevelsToBeResident.				*/
    public bool bGlobalForceMipLevelsToBeResident;

    /** Allows texture to be a source for Texture2DComposite.  Will NOT be available for use in rendering! */
    public bool bIsCompositingSource;

    /** Whether the texture has been painted in the editor.						*/
    public bool bHasBeenPaintedInEditor;

    /** Name of texture file cache texture mips are stored in, NAME_None if it is not part of one. */
    public FName TextureFileCacheName;

    /** ID generated whenever the texture is changed so that its bulk data can be updated in the TextureFileCache during cook */
    public FGuid TextureFileCacheGuid;

    /** Number of mips to remove when recompressing (does not work with TC_NormalmapUncompressed) */
    public int MipsToRemoveOnCompress;

    /** 
    * Keep track of the first mip level stored in the packed miptail.
    * it's set to highest mip level if no there's no packed miptail 
    */
    public int MipTailBaseIdx;

    #endregion

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        // Skip serializing cached mips if there's no texture data
        if (SizeX <= 0 || SizeY <= 0) return;

        Ar.Serialize(ref CachedPVRTCMips);

#if DEBUG
        foreach (var mip in CachedPVRTCMips)
        {
            Debug.Assert(mip.SizeY > 0 && mip.SizeY <= 4096);
            Debug.Assert(mip.SizeX > 0 && mip.SizeX <= 4096);
        }
#endif

        // @TODO this is a hack. I don't know what to do in this scenario
        // @TODO now that UnrealArchive has merged, look into fixing this here
        if (CachedPVRTCMips.Length > 0 && !CachedPVRTCMips[0].Data.IsStoredInSeparateFile)
        {
            var mip = CachedPVRTCMips[0];
            mip.SizeX = SizeX;
            mip.SizeY = SizeY;
        }
    }

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            // BOOL
            case nameof(bIsCompositingSource): Ar.Serialize(ref bIsCompositingSource); break;

            // INT
            case nameof(SizeX): Ar.Serialize(ref SizeX); break;
            case nameof(SizeY): Ar.Serialize(ref SizeY); break;
            case nameof(OriginalSizeX): Ar.Serialize(ref OriginalSizeX); break;
            case nameof(OriginalSizeY): Ar.Serialize(ref OriginalSizeY); break;
            case nameof(MipTailBaseIdx): Ar.Serialize(ref MipTailBaseIdx); break;

            // NAME
            case nameof(TextureFileCacheName): Ar.Serialize(ref TextureFileCacheName); break;

            // ENUM
            case nameof(AddressX): Ar.Serialize(ref tag.Value.Name); AddressX = GetTextureAddress(tag.Value.Name); break;
            case nameof(AddressY): Ar.Serialize(ref tag.Value.Name); AddressY = GetTextureAddress(tag.Value.Name); break;
            case nameof(Format): Ar.Serialize(ref tag.Value.Name); Format = GetPixelFormat(tag.Value.Name); break;

            default: base.ParseProperty(Ar, tag); break;
        }
    }

    private static PixelFormat GetPixelFormat(FName name) => name.GetString switch
    {
        "PF_DXT1" => PixelFormat.DXT1,
        "PF_DXT5" => PixelFormat.DXT5,
        "PF_A8R8G8B8" => PixelFormat.A8R8G8B8,
        "PF_V8U8" => PixelFormat.V8U8,
        "PF_G8" => PixelFormat.G8
    };

    private static TextureAddress GetTextureAddress(FName name) => name.GetString switch
    {
        "TA_Wrap" => TextureAddress.Wrap,
        "TA_Clamp" => TextureAddress.Clamp,
        "TA_Mirror" => TextureAddress.Mirror
    };

    /// <summary>
    /// Gets the highest mip containing data.
    /// </summary>
    public bool GetFirstValidPVRTCMip(out Texture2DMipMap outMip)
    {
        if (CachedPVRTCMips is not null)
        {
            foreach (var mip in CachedPVRTCMips)
            {
                if (mip.Data.ContainsData)
                {
                    // PVRTC does not support 4096x4096. All mips this size should be cooked out anyway
                    Debug.Assert(mip.SizeX > 0 && mip.SizeX <= 2048);
                    Debug.Assert(mip.SizeY > 0 && mip.SizeY <= 2048);

                    outMip = mip;
                    return true;
                }
            }
        }

        outMip = default;
        return false;
    }

    // Not all-inclusive, but covers everything IB has ever used
    public bool IsCompressed() => Format is not (PixelFormat.A32B32G32R32F or PixelFormat.A8R8G8B8 or PixelFormat.G8 or PixelFormat.G16 or PixelFormat.V8U8);
}
