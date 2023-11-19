//using UnrealLib.Core;
//using UnrealLib.Enums.Textures;

//namespace UnrealLib.Experimental.Textures
//{
//    public class UTexture2D(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UTexture(stream, pkg, export)
//    {
//        #region Structs

//        public struct Texture2DMipMap
//        {
//            public byte[] Data;
//            public int SizeX;
//            public int SizeY;
//        }

//        #endregion

//        #region Properties

//        /** The texture's mip-map data.												*/
//        public Texture2DMipMap[] Mips;

//        /** Cached PVRTC compressed texture data									*/
//        public Texture2DMipMap[] CachedPVRTCMips;

//        /** Cached ATITC compressed texture data									*/
//        // public Texture2DMipMap[] CachedATITCMips;

//        /** The size that the Flash compressed texture data was cached at 			*/
//        // public int CachedFlashMipsMaxResolution;

//        /** Cached Flash compressed texture data									*/
//        // public TextureMipBulkData[] CachedFlashMips;

//        /** The width of the texture.												*/
//        public int SizeX;

//        /** The height of the texture.												*/
//        public int SizeY;

//        /** The original width of the texture source art we imported from.			*/
//        public int OriginalSizeX;

//        /** The original height of the texture source art we imported from.			*/
//        public int OriginalSizeY;

//        /** The format of the texture data.											*/
//        public PixelFormat Format;

//        /** The addressing mode to use for the X axis.								*/
//        public TextureAddress AddressX;

//        /** The addressing mode to use for the Y axis.								*/
//        public TextureAddress AddressY;

//        /** Global/ serialized version of ForceMiplevelsToBeResident.				*/
//        public bool bGlobalForceMipLevelsToBeResident;

//        /** Allows texture to be a source for Texture2DComposite.  Will NOT be available for use in rendering! */
//        public bool bIsCompositingSource;

//        /** Whether the texture has been painted in the editor.						*/
//        public bool bHasBeenPaintedInEditor;

//        /** Name of texture file cache texture mips are stored in, NAME_None if it is not part of one. */
//        public FName TextureFileCacheName;

//        /** ID generated whenever the texture is changed so that its bulk data can be updated in the TextureFileCache during cook */
//        public FGuid TextureFileCacheGuid;

//        /** Number of mips to remove when recompressing (does not work with TC_NormalmapUncompressed) */
//        public int MipsToRemoveOnCompress;

//        /** 
//        * Keep track of the first mip level stored in the packed miptail.
//        * it's set to highest mip level if no there's no packed miptail 
//        */
//        public int MipTailBaseIdx;

//        #endregion
//    }
//}
