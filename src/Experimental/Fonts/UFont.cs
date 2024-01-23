using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.Fonts;

public class UFont(FObjectExport export) : UObject(export)
{
    #region Properties

    public FontCharacter[] Characters;
    // public UTexture2D[] Textures;    @TODO
    public KeyValuePair<short, short>[] CharRemap;
    public int IsRemapped;
    public float EmScale, Ascent, Descent, Leading;
    public int Kerning;
    public FontImportOptions ImportOptions = new();
    public float ScalingFactor = 1.0f;

    #endregion

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref CharRemap);
    }

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            case nameof(Characters): Ar.Serialize(ref Characters, tag.ArraySize); break;
            case nameof(CharRemap): Ar.Serialize(ref CharRemap); break;
            case nameof(IsRemapped): Ar.Serialize(ref IsRemapped); break;
            case nameof(EmScale): Ar.Serialize(ref EmScale); break;
            case nameof(Ascent): Ar.Serialize(ref Ascent); break;
            case nameof(Descent): Ar.Serialize(ref Descent); break;
            case nameof(Leading): Ar.Serialize(ref Leading); break;
            case nameof(Kerning): Ar.Serialize(ref Kerning); break;
            case nameof(ImportOptions): ImportOptions.SerializeProperties(Ar); break;
            case nameof(ScalingFactor): Ar.Serialize(ref ScalingFactor); break;
            default: base.ParseProperty(Ar, tag); break;
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct FontCharacter
{
    public int StartU;
    public int StartV;
    public int USize;
    public int VSize;
    public byte TextureIndex;
    public int VerticalOffset;
}

public class FontImportOptions : UObject
{
    #region Enums

    /// <summary>
    /// Font character set type for importing TrueType fonts.
    /// </summary>
    public enum EFontImportCharacterSet
    {
        FontICS_Default,
        FontICS_Ansi,
        FontICS_Symbol
    };

    #endregion

    #region Properties

    /// <summary>
    /// Name of the typeface for the font to import.
    /// </summary>
    public string FontName;
    /// <summary>
    /// Height of font (point size).
    /// </summary>
    public float Height;
    /// <summary>
    /// Whether the font should be antialiased or not. Usually you should leave this enabled.
    /// </summary>
    public bool bEnableAntialiasing;
    /// <summary>
    /// Whether the font should be generated in bold or not.
    /// </summary>
    public bool bEnableBold;
    /// <summary>
    /// Whether the font should be generated in italics or not.
    /// </summary>
    public bool bEnableItalic;
    /// <summary>
    /// Whether the font should be generated with an underline or not
    /// </summary>
    public bool bEnableUnderline;
    /// <summary>
    /// If TRUE, forces PF_G8 and only maintains Alpha value and discards color.
    /// </summary>
    public bool bAlphaOnly;
    /// <summary>
    /// Character set for this font.
    /// </summary>
    public EFontImportCharacterSet CharacterSet;

    /// <summary>
    /// Explicit list of characters to include in the font.
    /// </summary>
    public string Chars;
    /// <summary>
    /// Range of Unicode character values to include in the font.<br/>
    /// You can specify ranges using hyphens and/or commas (e.g. '400-900').
    /// </summary>
    public string UnicodeRange;
    /// <summary>
    /// Path on disk to a folder where files that contain a list of characters to include in the font.
    /// </summary>
    public string CharsFilePath;
    /// <summary>
    /// File mask wildcard that specifies which files within the CharsFilePath to scan for characters in include in the font.
    /// </summary>
    public string CharsFileWildcard;
    /// <summary>
    /// Skips generation of glyphs for any characters that are not considered 'printable'.
    /// </summary>
    public bool bCreatePrintableOnly;
    /// <summary>
    /// When specifying a range of characters and this is enabled, forces ASCII characters (0 thru 255) to be included as well.
    /// </summary>
    public bool bIncludeASCIIRange;

    /// <summary>
    /// Color of the foreground font pixels.<br/>
    /// Usually you should leave this white and instead use the UI Styles editor to change the color of the font on the fly.
    /// </summary>
    public LinearColor ForegroundColor;
    /// <summary>
    /// Enables a very simple, 1-pixel, black colored drop shadow for the generated font.
    /// </summary>
    public bool bEnableDropShadow;

    /// <summary>
    /// Horizontal size of each texture page for this font in pixels.
    /// </summary>
    public int TexturePageWidth;
    /// <summary>
    /// The maximum vertical size of a texture page for this font in pixels.
    /// The actual height of a texture page may be less than this if the font can fit within a smaller sized texture page.
    /// </summary>
    public int TexturePageMaxHeight;
    /// <summary>
    /// Horizontal padding between each font character on the texture page in pixels.
    /// </summary>
    public int XPadding;
    /// <summary>
    /// Vertical padding between each font character on the texture page in pixels.
    /// </summary>
    public int YPadding;

    /// <summary>
    /// How much to extend the top of the UV coordinate rectangle for each character in pixels.
    /// </summary>
    public int ExtendBoxTop;
    /// <summary>
    /// How much to extend the bottom of the UV coordinate rectangle for each character in pixels.
    /// </summary>
    public int ExtendBoxBottom;
    /// <summary>
    /// How much to extend the right of the UV coordinate rectangle for each character in pixels.
    /// </summary>
    public int ExtendBoxRight;
    /// <summary>
    /// How much to extend the left of the UV coordinate rectangle for each character in pixels.
    /// </summary>
    public int ExtendBoxLeft;

    /// <summary>
    /// Enables legacy font import mode.<br/>
    /// This results in lower quality antialiasing and larger glyph bounds, but may be useful when debugging problems.
    /// </summary>
    public bool bEnableLegacyMode;

    /// <summary>
    /// The initial horizontal spacing adjustment between rendered characters.<br/>
    /// This setting will be copied directly into the generated Font object's properties.
    /// </summary>
    public int Kerning;

    /// <summary>
    /// If TRUE then the alpha channel of the font textures will store a distance field instead of a color mask.
    /// </summary>
    public bool bUseDistanceFieldAlpha;
    /// <summary>
    /// Scale factor determines how big to scale the font bitmap during import when generating distance field values.<br/>
    /// Note that higher values give better quality but importing will take much longer.
    /// </summary>
    public int DistanceFieldScaleFactor;
    /// <summary>
    /// Shrinks or expands the scan radius used to determine the silhouette of the font edges.
    /// </summary>
    // ClampMin=0.0, ClampMax=4.0
    public float DistanceFieldScanRadiusScale;

    #endregion

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            case nameof(FontName): Ar.Serialize(ref FontName); break;
            case nameof(Height): Ar.Serialize(ref Height); break;
            case nameof(bEnableAntialiasing): Ar.Serialize(ref bEnableAntialiasing); break;
            case nameof(bEnableBold): Ar.Serialize(ref bEnableBold); break;
            case nameof(bEnableItalic): Ar.Serialize(ref bEnableItalic); break;
            case nameof(bEnableUnderline): Ar.Serialize(ref bEnableUnderline); break;
            case nameof(bAlphaOnly): Ar.Serialize(ref bAlphaOnly); break;
            case nameof(CharacterSet): Ar.Serialize(ref CharacterSet); break;
            case nameof(Chars): Ar.Serialize(ref Chars); break;
            case nameof(UnicodeRange): Ar.Serialize(ref UnicodeRange); break;
            case nameof(CharsFilePath): Ar.Serialize(ref CharsFilePath); break;
            case nameof(CharsFileWildcard): Ar.Serialize(ref CharsFileWildcard); break;
            case nameof(bCreatePrintableOnly): Ar.Serialize(ref bCreatePrintableOnly); break;
            case nameof(bIncludeASCIIRange): Ar.Serialize(ref bIncludeASCIIRange); break;
            case nameof(ForegroundColor): Ar.Serialize(ref ForegroundColor); break;
            case nameof(bEnableDropShadow): Ar.Serialize(ref bEnableDropShadow); break;
            case nameof(TexturePageWidth): Ar.Serialize(ref TexturePageWidth); break;
            case nameof(TexturePageMaxHeight): Ar.Serialize(ref TexturePageMaxHeight); break;
            case nameof(XPadding): Ar.Serialize(ref XPadding); break;
            case nameof(YPadding): Ar.Serialize(ref YPadding); break;
            case nameof(ExtendBoxTop): Ar.Serialize(ref ExtendBoxTop); break;
            case nameof(ExtendBoxBottom): Ar.Serialize(ref ExtendBoxBottom); break;
            case nameof(ExtendBoxRight): Ar.Serialize(ref ExtendBoxRight); break;
            case nameof(ExtendBoxLeft): Ar.Serialize(ref ExtendBoxLeft); break;
            case nameof(bEnableLegacyMode): Ar.Serialize(ref bEnableLegacyMode); break;
            case nameof(Kerning): Ar.Serialize(ref Kerning); break;
            case nameof(bUseDistanceFieldAlpha): Ar.Serialize(ref bUseDistanceFieldAlpha); break;
            case nameof(DistanceFieldScaleFactor): Ar.Serialize(ref DistanceFieldScaleFactor); break;
            case nameof(DistanceFieldScanRadiusScale): Ar.Serialize(ref DistanceFieldScanRadiusScale); break;
            default: base.ParseProperty(Ar, tag); break;
        }
    }
}