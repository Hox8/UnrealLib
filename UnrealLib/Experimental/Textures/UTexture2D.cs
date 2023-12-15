using System;
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

        // This NEEDS to be source-generated
        public override void SerializeScriptProperties()
        {
            if (Ar.IsLoading)
            {
                DefaultProperties = new();

                while (true)
                {
                    FPropertyTag Tag = new();
                    Tag.Serialize(Ar);

                    if (Tag.Name == "None") return;

                    switch (Tag.Name.ToString())
                    {
                        // BOOL
                        case nameof(SRGB): SRGB = Tag.Value.Bool; break;
                        case nameof(CompressionNoAlpha): CompressionNoAlpha = Tag.Value.Bool; break;
                        case nameof(NeverStream): NeverStream = Tag.Value.Bool; break;
                        case nameof(bForcePVRTC4): bForcePVRTC4 = Tag.Value.Bool; break;
                        case nameof(CompressionNone): CompressionNone = Tag.Value.Bool; break;
                        case nameof(bIsSourceArtUncompressed): bIsSourceArtUncompressed = Tag.Value.Bool; break;
                        case nameof(bIsCompositingSource): bIsCompositingSource = Tag.Value.Bool; break;
                        case nameof(DeferCompression): DeferCompression = Tag.Value.Bool; break;

                        // INT
                        case nameof(SizeX): SizeX = Tag.Value.Int; break;
                        case nameof(SizeY): SizeY = Tag.Value.Int; break;
                        case nameof(OriginalSizeX): OriginalSizeX = Tag.Value.Int; break;
                        case nameof(OriginalSizeY): OriginalSizeY = Tag.Value.Int; break;
                        case nameof(MipTailBaseIdx): MipTailBaseIdx = Tag.Value.Int; break;
                        case nameof(InternalFormatLODBias): InternalFormatLODBias = Tag.Value.Int; break;

                        // FLOAT
                        case nameof(AdjustBrightnessCurve): AdjustBrightness = Tag.Value.Float; break;
                        case nameof(AdjustBrightness): AdjustBrightness = Tag.Value.Float; break;
                        case nameof(AdjustSaturation): AdjustSaturation = Tag.Value.Float; break;
                        case nameof(AdjustRGBCurve): AdjustRGBCurve = Tag.Value.Float; break;

                        // FLOAT[]
                        case nameof(UnpackMin): UnpackMin[Tag.ArrayIndex] = Tag.Value.Float; break;

                        // NAME
                        case nameof(TextureFileCacheName): TextureFileCacheName = Tag.Value.Name; break;

                        // ENUM
                        case nameof(AddressX): AddressX = GetTextureAddress(Ar.GetNameEntry(Tag.Value.Name.Index).Name); break;
                        case nameof(AddressY): AddressY = GetTextureAddress(Ar.GetNameEntry(Tag.Value.Name.Index).Name); break;
                        case nameof(Filter): Filter = GetTextureFilter(Ar.GetNameEntry(Tag.Value.Name.Index).Name); break;
                        case nameof(LODGroup): LODGroup = GetTextureLodGroup(Ar.GetNameEntry(Tag.Value.Name.Index).Name); break;
                        case nameof(Format): Format = GetPixelFormat(Ar.GetNameEntry(Tag.Value.Name.Index).Name); break;
                        case nameof(CompressionSettings): CompressionSettings = GetCompressionSettings(Ar.GetNameEntry(Tag.Value.Name.Index).Name); break;
                        case nameof(MipGenSettings): MipGenSettings = GetMipGenSettings(Ar.GetNameEntry(Tag.Value.Name.Index).Name); break;
                        default:
                            {
                                // Unexpected properties are dumped here
#if DEBUG
                                Console.WriteLine($"Unexpected property in {this} defaults: '{Tag.Name}'");
#endif
                                DefaultProperties.Add(Tag); break;
                            }
                    }
                }
            }
        }

        public override void Serialize()
        {
            base.Serialize();

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

        private static PixelFormat GetPixelFormat(string str) => str switch
        {
            "PF_DXT1" => PixelFormat.DXT1,
            "PF_DXT5" => PixelFormat.DXT5,
            "PF_A8R8G8B8" => PixelFormat.A8R8G8B8,
            "PF_V8U8" => PixelFormat.V8U8,
            "PF_G8" => PixelFormat.G8
        };

        private static TextureCompressionSettings GetCompressionSettings(string str) => str switch
        {
            "TC_Normalmap" => TextureCompressionSettings.Normalmap,
            "TC_NormalmapAlpha" => TextureCompressionSettings.NormalmapAlpha,
            "TC_NormalmapUncompressed" => TextureCompressionSettings.NormalmapUncompressed,
            "TC_Grayscale" => TextureCompressionSettings.Grayscale
        };

        private static TextureMipGenSettings GetMipGenSettings(string str) => str switch
        {
            "TMGS_NoMipmaps" => TextureMipGenSettings.NoMipmaps,
            "TMGS_Sharpen4" => TextureMipGenSettings.Sharpen4
        };

        private static TextureGroup GetTextureLodGroup(string str) => str switch
        {
            "TEXTUREGROUP_World" => TextureGroup.World,
            "TEXTUREGROUP_WorldNormalMap" => TextureGroup.WorldNormalMap,
            "TEXTUREGROUP_WorldSpecular" => TextureGroup.WorldSpecular,
            "TEXTUREGROUP_Character" => TextureGroup.Character,
            "TEXTUREGROUP_CharacterNormalMap" => TextureGroup.CharacterNormalMap,
            "TEXTUREGROUP_CharacterSpecular" => TextureGroup.CharacterSpecular,
            "TEXTUREGROUP_Weapon" => TextureGroup.Weapon,
            "TEXTUREGROUP_WeaponNormalMap" => TextureGroup.WeaponNormalMap,
            "TEXTUREGROUP_WeaponSpecular" => TextureGroup.WeaponSpecular,
            "TEXTUREGROUP_Vehicle" => TextureGroup.Vehicle,
            "TEXTUREGROUP_VehicleNormalMap" => TextureGroup.VehicleNormalMap,
            "TEXTUREGROUP_VehicleSpecular" => TextureGroup.VehicleSpecular,
            "TEXTUREGROUP_Cinematic" => TextureGroup.Cinematic,
            "TEXTUREGROUP_Effects" => TextureGroup.Effects,
            "TEXTUREGROUP_EffectsNotFiltered" => TextureGroup.EffectsNotFiltered,
            "TEXTUREGROUP_Skybox" => TextureGroup.Skybox,
            "TEXTUREGROUP_UI" => TextureGroup.UI,
            "TEXTUREGROUP_Lightmap" => TextureGroup.Lightmap,
            "TEXTUREGROUP_RenderTarget" => TextureGroup.RenderTarget,
            "TEXTUREGROUP_MobileFlattened" => TextureGroup.MobileFlattened,
            "TEXTUREGROUP_ProcBuilding_Face" => TextureGroup.ProcBuilding_Face,
            "TEXTUREGROUP_ProcBuilding_LightMap" => TextureGroup.ProcBuilding_LightMap,
            "TEXTUREGROUP_Shadowmap" => TextureGroup.Shadowmap,
            "TEXTUREGROUP_ColorLookupTable" => TextureGroup.ColorLookupTable,
            "TEXTUREGROUP_Terrain_Heightmap" => TextureGroup.Terrain_Heightmap,
            "TEXTUREGROUP_Terrain_Weightmap" => TextureGroup.Terrain_Weightmap,
            "TEXTUREGROUP_ImageBasedReflection" => TextureGroup.ImageBasedReflection,
            "TEXTUREGROUP_Bokeh" => TextureGroup.Bokeh
        };

        private static TextureFilter GetTextureFilter(string str) => str switch
        {
            "TF_Nearest" => TextureFilter.Nearest,
            "TF_Linear" => TextureFilter.Linear
        };

        private static TextureAddress GetTextureAddress(string str) => str switch
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
