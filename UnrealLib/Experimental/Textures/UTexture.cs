using UnrealLib.Core;

namespace UnrealLib.Experimental.Textures
{
    public class UTexture(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : USurface(stream, pkg, export)
    {
        FUntypedBulkData SourceArt;

        #region Public enums

        public enum TextureCompressionSettings
        {
            Default,
            Normalmap,
            Displacementmap,
            NormalmapAlpha,
            Grayscale,
            HighDynamicRange,
            OneBitAlpha,
            NormalmapUncompressed,
            NormalmapBC5,
            OneBitMonochrome,
            SimpleLightmapModification,
            VectorDisplacementmap
        };

        public enum TextureFilter
        {
            Nearest,
            Linear
        };

        public enum TextureAddress
        {
            Wrap,
            Clamp,
            Mirror
        };

        public enum TextureGroup
        {
            World,
            WorldNormalMap,
            WorldSpecular,
            Character,
            CharacterNormalMap,
            CharacterSpecular,
            Weapon,
            WeaponNormalMap,
            WeaponSpecular,
            Vehicle,
            VehicleNormalMap,
            VehicleSpecular,
            Cinematic,
            Effects,
            EffectsNotFiltered,
            Skybox,
            UI,
            Lightmap,
            RenderTarget,
            MobileFlattened,
            ProcBuilding_Face,
            ProcBuilding_LightMap,
            Shadowmap,
            ColorLookupTable,
            Terrain_Heightmap,
            Terrain_Weightmap,
            ImageBasedReflection,
            Bokeh
        };

        public enum TextureMipGenSettings
        {
            // default for the "texture"
            FromTextureGroup,
            // 2x2 average, default for the "texture group"
            SimpleAverage,
            // 8x8 with sharpening: 0=no sharpening but better quality which is softer, 1..little, 5=medium, 10=extreme
            Sharpen0,
            Sharpen1,
            Sharpen2,
            Sharpen3,
            Sharpen4,
            Sharpen5,
            Sharpen6,
            Sharpen7,
            Sharpen8,
            Sharpen9,
            Sharpen10,
            NoMipmaps,
            // Do not touch existing mip chain as it contains generated data
            LeaveExistingMips,
            // blur further (useful for image based reflections)
            Blur1,
            Blur2,
            Blur3,
            Blur4,
            Blur5
        };

        public enum ETextureMipCount
        {
            ResidentMips,
            AllMips,
            AllMipsBiased,
        };

        #endregion

        #region Properties

        public bool SRGB;
        public bool RGBE;

        public float[] UnpackMin = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };
        public float[] UnpackMax = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };

        // UntypedBulkDaMirror SourceArt

        public bool bIsSourceArtUncompressed;

        public bool CompressionNoAlpha;
        public bool CompressionNone;
        public bool CompressionNoMipmaps;
        public bool CompressionFullDynamicRange;
        public bool DeferCompression;
        public bool NeverStream;

        /** When TRUE, the alpha channel of mip-maps and the base image are dithered for smooth LOD transitions. */
        public bool bDitherMipMapAlpha;

        /** If TRUE, the color border pixels are preserved by mipmap generation.  One flag per color channel. */
        public bool bPreserveBorderR;
        public bool bPreserveBorderG;
        public bool bPreserveBorderB;
        public bool bPreserveBorderA;

        /** If TRUE, the RHI texture will be created using TexCreate_NoTiling */
        public /*const*/ bool bNoTiling;

        /** For DXT1 textures, setting this will cause the texture to be twice the size, but better looking, on iPhone */
        public bool bForcePVRTC4;

        public TextureCompressionSettings CompressionSettings = TextureCompressionSettings.Default;

        /** The texture filtering mode to use when sampling this texture. */
        public TextureFilter Filter = TextureFilter.Nearest;

        /** Texture group this texture belongs to for LOD bias */
        public TextureGroup LODGroup = TextureGroup.World;

        /** A bias to the index of the top mip level to use. */
        public int LODBias = 0;

        /** Number of mip-levels to use for cinematic quality. */
        public int NumCinematicMipLevels = 0;

        /** Path to the resource used to construct this texture */
        public string SourceFilePath;
        /** Date/Time-stamp of the file from the last import */
        public string SourceFileTimestamp;

        /** The texture's resource. */
        // var native const pointer Resource{FTextureResource};

        /** Unique ID for this material, used for caching during distributed lighting */
        private FGuid LightingGuid;

        /** Static texture brightness adjustment (scales HSV value.)  (Non-destructive; Requires texture source art to be available.) */
        public float AdjustBrightness;

        /** Static texture curve adjustment (raises HSV value to the specified power.)  (Non-destructive; Requires texture source art to be available.)  */
        public float AdjustBrightnessCurve;

        /** Static texture "vibrance" adjustment (0 - 1) (HSV saturation algorithm adjustment.)  (Non-destructive; Requires texture source art to be available.)  */
        public float AdjustVibrance;

        /** Static texture saturation adjustment (scales HSV saturation.)  (Non-destructive; Requires texture source art to be available.)  */
        public float AdjustSaturation;

        /** Static texture RGB curve adjustment (raises linear-space RGB color to the specified power.)  (Non-destructive; Requires texture source art to be available.)  */
        public float AdjustRGBCurve;

        /** Static texture hue adjustment (0 - 360) (offsets HSV hue by value in degrees.)  (Non-destructive; Requires texture source art to be available.)  */
        public float AdjustHue;

        /** Internal LOD bias already applied by the texture format (eg NormalMapUncompressed). Used to adjust MinLODMipCount and MaxLODMipCount in CalculateLODBias */
        public int InternalFormatLODBias;

        /** Per asset specific setting to define the mip-map generation properties like sharpening and kernel size. */
        public TextureMipGenSettings MipGenSettings = TextureMipGenSettings.FromTextureGroup;

        #endregion

        public override void Serialize(UnrealStream stream)
        {
            base.Serialize(stream);

            stream.Serialize(ref SourceArt);
        }
    }
}
