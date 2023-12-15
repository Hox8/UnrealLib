using UnrealLib.Core;

namespace UnrealLib.Experimental.Textures;

    public class UTexture(FObjectExport export) : USurface(export)
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

        /// <summary>When TRUE, the alpha channel of mip-maps and the base image are dithered for smooth LOD transitions.</summary>
        public bool bDitherMipMapAlpha;

        /// <summary>If TRUE, the color border pixels are preserved by mipmap generation. One flag per color channel.</summary>
        public bool bPreserveBorderR;
        public bool bPreserveBorderG;
        public bool bPreserveBorderB;
        public bool bPreserveBorderA;

        /// <summary>If TRUE, the RHI texture will be created using TexCreate_NoTiling.</summary>
        public /*const*/ bool bNoTiling;

        /// <summary>For DXT1 textures, setting this will cause the texture to be twice the size, but better looking, on iPhone.</summary>
        public bool bForcePVRTC4;

        public TextureCompressionSettings CompressionSettings = TextureCompressionSettings.Default;

        /// <summary>The texture filtering mode to use when sampling this texture.</summary>
        public TextureFilter Filter = TextureFilter.Nearest;

        /// <summary>Texture group this texture belongs to for LOD bias.</summary>
        public TextureGroup LODGroup = TextureGroup.World;

        /// <summary>
        /// A bias to the index of the top mip level to use.
        /// </summary>
        public int LODBias = 0;

        /// <summary>
        /// Number of mip-levels to use for cinematic quality.
        /// </summary>
        public int NumCinematicMipLevels = 0;

        ///<summary>Path to the resource used to construct this texture</summary>
        public string SourceFilePath;
        ///<summary>Date/Time-stamp of the file from the last import</summary>
        public string SourceFileTimestamp;

        ///<summary>The texture's resource.</summary>
        // var native const pointer Resource{FTextureResource};

        ///<summary>Unique ID for this material, used for caching during distributed lighting.</summary>
        private FGuid LightingGuid;

        ///<summary>Static texture brightness adjustment (scales HSV value) (Non-destructive; Requires texture source art to be available).</summary>
        public float AdjustBrightness;

        ///<summary>Static texture curve adjustment (raises HSV value to the specified power) (Non-destructive; Requires texture source art to be available).</summary>
        public float AdjustBrightnessCurve;

        ///<summary>Static texture "vibrance" adjustment (0 - 1) (HSV saturation algorithm adjustment) (Non-destructive; Requires texture source art to be available).</summary>
        public float AdjustVibrance;

        ///<summary>Static texture saturation adjustment (scales HSV saturation) (Non-destructive; Requires texture source art to be available).</summary>
        public float AdjustSaturation;

        ///<summary>Static texture RGB curve adjustment (raises linear-space RGB color to the specified power) (Non-destructive; Requires texture source art to be available).</summary>
        public float AdjustRGBCurve;

        ///<summary>Static texture hue adjustment (0 - 360) (offsets HSV hue by value in degrees) (Non-destructive; Requires texture source art to be available).</summary>
        public float AdjustHue;

        ///<summary>Internal LOD bias already applied by the texture format (eg NormalMapUncompressed). Used to adjust MinLODMipCount and MaxLODMipCount in CalculateLODBias.</summary>
        public int InternalFormatLODBias;

        ///<summary>Per asset specific setting to define the mip-map generation properties like sharpening and kernel size.</summary>
        public TextureMipGenSettings MipGenSettings = TextureMipGenSettings.FromTextureGroup;

        #endregion

    public override void Serialize(UnrealArchive Ar)
        {
        base.Serialize(Ar);

            Ar.Serialize(ref SourceArt);
        }

        public override void SerializeScriptProperties()
        {
            base.SerializeScriptProperties();
        }
    }
}
