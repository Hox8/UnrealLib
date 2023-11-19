//using UnrealLib.Core;

//namespace UnrealLib.Experimental.Textures
//{
//    public class UTexture(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : USurface(stream, pkg, export)
//    {
//        #region public enums

//        public enum TextureCompressionSettings
//        {
//            TC_Default,
//            TC_Normalmap,
//            TC_Displacementmap,
//            TC_NormalmapAlpha,
//            TC_Grayscale,
//            TC_HighDynamicRange,
//            TC_OneBitAlpha,
//            TC_NormalmapUncompressed,
//            TC_NormalmapBC5,
//            TC_OneBitMonochrome,
//            TC_SimpleLightmapModification,
//            TC_VectorDisplacementmap
//        };

//        public enum TextureFilter
//        {
//            TF_Nearest,
//            TF_Linear
//        };

//        public enum TextureAddress
//        {
//            TA_Wrap,
//            TA_Clamp,
//            TA_Mirror
//        };

//        public enum TextureGroup
//        {
//            TEXTUREGROUP_World,
//            TEXTUREGROUP_WorldNormalMap,
//            TEXTUREGROUP_WorldSpecular,
//            TEXTUREGROUP_Character,
//            TEXTUREGROUP_CharacterNormalMap,
//            TEXTUREGROUP_CharacterSpecular,
//            TEXTUREGROUP_Weapon,
//            TEXTUREGROUP_WeaponNormalMap,
//            TEXTUREGROUP_WeaponSpecular,
//            TEXTUREGROUP_Vehicle,
//            TEXTUREGROUP_VehicleNormalMap,
//            TEXTUREGROUP_VehicleSpecular,
//            TEXTUREGROUP_Cinematic,
//            TEXTUREGROUP_Effects,
//            TEXTUREGROUP_EffectsNotFiltered,
//            TEXTUREGROUP_Skybox,
//            TEXTUREGROUP_UI,
//            TEXTUREGROUP_Lightmap,
//            TEXTUREGROUP_RenderTarget,
//            TEXTUREGROUP_MobileFlattened,
//            TEXTUREGROUP_ProcBuilding_Face,
//            TEXTUREGROUP_ProcBuilding_LightMap,
//            TEXTUREGROUP_Shadowmap,
//            TEXTUREGROUP_ColorLookupTable,
//            TEXTUREGROUP_Terrain_Heightmap,
//            TEXTUREGROUP_Terrain_Weightmap,
//            TEXTUREGROUP_ImageBasedReflection,
//            TEXTUREGROUP_Bokeh
//        };

//        public enum TextureMipGenSettings
//        {
//            // default for the "texture"
//            TMGS_FromTextureGroup,
//            // 2x2 average, default for the "texture group"
//            TMGS_SimpleAverage,
//            // 8x8 with sharpening: 0=no sharpening but better quality which is softer, 1..little, 5=medium, 10=extreme
//            TMGS_Sharpen0,
//            TMGS_Sharpen1,
//            TMGS_Sharpen2,
//            TMGS_Sharpen3,
//            TMGS_Sharpen4,
//            TMGS_Sharpen5,
//            TMGS_Sharpen6,
//            TMGS_Sharpen7,
//            TMGS_Sharpen8,
//            TMGS_Sharpen9,
//            TMGS_Sharpen10,
//            TMGS_NoMipmaps,
//            // Do not touch existing mip chain as it contains generated data
//            TMGS_LeaveExistingMips,
//            // blur further (useful for image based reflections)
//            TMGS_Blur1,
//            TMGS_Blur2,
//            TMGS_Blur3,
//            TMGS_Blur4,
//            TMGS_Blur5
//        };

//        public enum ETextureMipCount
//        {
//            TMC_ResidentMips,
//            TMC_AllMips,
//            TMC_AllMipsBiased,
//        };

//        #endregion

//        #region Properties

//        public bool SRGB;
//        public bool RGBE;

//        public float[] UnpackMin, UnpackMax;

//        // UntypedBulkData_Mirror SourceArt

//        public bool bIsSourceArtUncompressed;

//        public bool CompressionNoAlpha;
//        public bool CompressionNone;
//        public bool CompressionNoMipmaps;
//        public bool CompressionFullDynamicRange;
//        public bool DeferCompression;
//        public bool NeverStream;

//        /** When TRUE, the alpha channel of mip-maps and the base image are dithered for smooth LOD transitions. */
//        public bool bDitherMipMapAlpha;

//        /** If TRUE, the color border pixels are preserved by mipmap generation.  One flag per color channel. */
//        public bool bPreserveBorderR;
//        public bool bPreserveBorderG;
//        public bool bPreserveBorderB;
//        public bool bPreserveBorderA;

//        /** If TRUE, the RHI texture will be created using TexCreate_NoTiling */
//        public /*const*/ bool bNoTiling;

//        /** For DXT1 textures, setting this will cause the texture to be twice the size, but better looking, on iPhone */
//        public bool bForcePVRTC4;

//        public TextureCompressionSettings CompressionSettings;

//        /** The texture filtering mode to use when sampling this texture. */
//        public TextureFilter Filter;

//        /** Texture group this texture belongs to for LOD bias */
//        public TextureGroup LODGroup;

//        /** A bias to the index of the top mip level to use. */
//        public int LODBias;

//        /** Number of mip-levels to use for cinematic quality. */
//        public int NumCinematicMipLevels;

//        /** Path to the resource used to construct this texture */
//        public string SourceFilePath;
//        /** Date/Time-stamp of the file from the last import */
//        public string SourceFileTimestamp;

//        /** The texture's resource. */
//        // var native const pointer Resource{FTextureResource};

//        /** Unique ID for this material, used for caching during distributed lighting */
//        private FGuid LightingGuid;

//        /** Static texture brightness adjustment (scales HSV value.)  (Non-destructive; Requires texture source art to be available.) */
//        public float AdjustBrightness;

//        /** Static texture curve adjustment (raises HSV value to the specified power.)  (Non-destructive; Requires texture source art to be available.)  */
//        public float AdjustBrightnessCurve;

//        /** Static texture "vibrance" adjustment (0 - 1) (HSV saturation algorithm adjustment.)  (Non-destructive; Requires texture source art to be available.)  */
//        public float AdjustVibrance;

//        /** Static texture saturation adjustment (scales HSV saturation.)  (Non-destructive; Requires texture source art to be available.)  */
//        public float AdjustSaturation;

//        /** Static texture RGB curve adjustment (raises linear-space RGB color to the specified power.)  (Non-destructive; Requires texture source art to be available.)  */
//        public float AdjustRGBCurve;

//        /** Static texture hue adjustment (0 - 360) (offsets HSV hue by value in degrees.)  (Non-destructive; Requires texture source art to be available.)  */
//        public float AdjustHue;

//        /** Internal LOD bias already applied by the texture format (eg TC_NormalMapUncompressed). Used to adjust MinLODMipCount and MaxLODMipCount in CalculateLODBias */
//        public int InternalFormatLODBias;

//        /** Per asset specific setting to define the mip-map generation properties like sharpening and kernel size. */
//        public TextureMipGenSettings MipGenSettings;

//        #endregion
//    }
//}
