using UnrealLib.Core;
using UnrealLib.Experimental.UnObj.DefaultProperties;

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

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            // BOOL
            case nameof(SRGB): Ar.Serialize(ref SRGB); break;
            case nameof(CompressionNoAlpha): Ar.Serialize(ref CompressionNoAlpha); break;
            case nameof(NeverStream): Ar.Serialize(ref NeverStream); break;
            case nameof(bForcePVRTC4): Ar.Serialize(ref bForcePVRTC4); break;
            case nameof(CompressionNone): Ar.Serialize(ref CompressionNone); break;
            case nameof(bIsSourceArtUncompressed): Ar.Serialize(ref bIsSourceArtUncompressed); break;
            case nameof(DeferCompression): Ar.Serialize(ref DeferCompression); break;

            // INT
            case nameof(InternalFormatLODBias): Ar.Serialize(ref InternalFormatLODBias); break;

            // FLOAT
            case nameof(AdjustBrightnessCurve): Ar.Serialize(ref AdjustBrightness); break;
            case nameof(AdjustBrightness): Ar.Serialize(ref AdjustBrightness); break;
            case nameof(AdjustSaturation): Ar.Serialize(ref AdjustSaturation); break;
            case nameof(AdjustRGBCurve): Ar.Serialize(ref AdjustRGBCurve); break;

            // FLOAT[]
            case nameof(UnpackMin): Ar.Serialize(ref UnpackMin[tag.ArrayIndex]); break;

            // ENUM
            case nameof(Filter): Ar.Serialize(ref tag.Value.Name); Filter = GetTextureFilter(tag.Value.Name); break;
            case nameof(LODGroup): Ar.Serialize(ref tag.Value.Name); LODGroup = GetTextureLodGroup(tag.Value.Name); break;
            case nameof(CompressionSettings): Ar.Serialize(ref tag.Value.Name); CompressionSettings = GetCompressionSettings(tag.Value.Name); break;
            case nameof(MipGenSettings): Ar.Serialize(ref tag.Value.Name); MipGenSettings = GetMipGenSettings(tag.Value.Name); break;
            default: base.ParseProperty(Ar, tag); break;
        }
    }

    private static TextureCompressionSettings GetCompressionSettings(FName name) => name.GetString switch
    {
        "TC_Normalmap" => TextureCompressionSettings.Normalmap,
        "TC_NormalmapAlpha" => TextureCompressionSettings.NormalmapAlpha,
        "TC_NormalmapUncompressed" => TextureCompressionSettings.NormalmapUncompressed,
        "TC_Grayscale" => TextureCompressionSettings.Grayscale
    };

    private static TextureMipGenSettings GetMipGenSettings(FName name) => name.GetString switch
    {
        "TMGS_NoMipmaps" => TextureMipGenSettings.NoMipmaps,
        "TMGS_Sharpen4" => TextureMipGenSettings.Sharpen4
    };

    private static TextureGroup GetTextureLodGroup(FName name) => name.GetString switch
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

    private static TextureFilter GetTextureFilter(FName name) => name.GetString switch
    {
        "TF_Nearest" => TextureFilter.Nearest,
        "TF_Linear" => TextureFilter.Linear
    };
}
