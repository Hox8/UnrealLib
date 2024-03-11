using NetEscapades.EnumGenerators;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnrealLib.Core;
using UnrealLib.Experimental.Textures;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Fonts;

public partial class UFont(FObjectExport export) : UObject(export)
{
    #region Properties

    [UProperty(ArrayProperty = true)] public FontCharacter[] Characters;
    // [UProperty] public UTexture2D[] Textures;
    public KeyValuePair<short, short>[] CharRemap;
    [UProperty] public int IsRemapped;
    [UProperty] public float EmScale, Ascent, Descent, Leading;
    [UProperty] public int Kerning;
    [UProperty] public FontImportOptions ImportOptions = new();
    [UProperty] public float ScalingFactor = 1.0f;

    #endregion

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref CharRemap);
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

public partial class FontImportOptions : UObject
{
    #region Enums

    /// <summary>
    /// Font character set type for importing TrueType fonts.
    /// </summary>
    [EnumExtensions]
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
    [UProperty] public string FontName = "Arial";
    /// <summary>
    /// Height of font (point size).
    /// </summary>
    [UProperty] public float Height = 16.0f;
    /// <summary>
    /// Whether the font should be antialiased or not. Usually you should leave this enabled.
    /// </summary>
    [UProperty] public bool bEnableAntialiasing = true;
    /// <summary>
    /// Whether the font should be generated in bold or not.
    /// </summary>
    [UProperty] public bool bEnableBold;
    /// <summary>
    /// Whether the font should be generated in italics or not.
    /// </summary>
    [UProperty] public bool bEnableItalic;
    /// <summary>
    /// Whether the font should be generated with an underline or not
    /// </summary>
    [UProperty] public bool bEnableUnderline;
    /// <summary>
    /// If TRUE, forces PF_G8 and only maintains Alpha value and discards color.
    /// </summary>
    [UProperty] public bool bAlphaOnly;
    /// <summary>
    /// Character set for this font.
    /// </summary>
    [UProperty] public EFontImportCharacterSet CharacterSet;

    /// <summary>
    /// Explicit list of characters to include in the font.
    /// </summary>
    [UProperty] public string Chars;
    /// <summary>
    /// Range of Unicode character values to include in the font.<br/>
    /// You can specify ranges using hyphens and/or commas (e.g. '400-900').
    /// </summary>
    [UProperty] public string UnicodeRange;
    /// <summary>
    /// Path on disk to a folder where files that contain a list of characters to include in the font.
    /// </summary>
    [UProperty] public string CharsFilePath;
    /// <summary>
    /// File mask wildcard that specifies which files within the CharsFilePath to scan for characters in include in the font.
    /// </summary>
    [UProperty] public string CharsFileWildcard;
    /// <summary>
    /// Skips generation of glyphs for any characters that are not considered 'printable'.
    /// </summary>
    [UProperty] public bool bCreatePrintableOnly;
    /// <summary>
    /// When specifying a range of characters and this is enabled, forces ASCII characters (0 thru 255) to be included as well.
    /// </summary>
    [UProperty] public bool bIncludeASCIIRange = true;

    /// <summary>
    /// Color of the foreground font pixels.<br/>
    /// Usually you should leave this white and instead use the UI Styles editor to change the color of the font on the fly.
    /// </summary>
    [UProperty] public LinearColor ForegroundColor = new(1.0f);
    /// <summary>
    /// Enables a very simple, 1-pixel, black colored drop shadow for the generated font.
    /// </summary>
    [UProperty] public bool bEnableDropShadow;

    /// <summary>
    /// Horizontal size of each texture page for this font in pixels.
    /// </summary>
    [UProperty] public int TexturePageWidth = 256;
    /// <summary>
    /// The maximum vertical size of a texture page for this font in pixels.
    /// The actual height of a texture page may be less than this if the font can fit within a smaller sized texture page.
    /// </summary>
    [UProperty] public int TexturePageMaxHeight = 256;
    /// <summary>
    /// Horizontal padding between each font character on the texture page in pixels.
    /// </summary>
    [UProperty] public int XPadding = 1;
    /// <summary>
    /// Vertical padding between each font character on the texture page in pixels.
    /// </summary>
    [UProperty] public int YPadding = 1;

    /// <summary>
    /// How much to extend the top of the UV coordinate rectangle for each character in pixels.
    /// </summary>
    [UProperty] public int ExtendBoxTop;
    /// <summary>
    /// How much to extend the bottom of the UV coordinate rectangle for each character in pixels.
    /// </summary>
    [UProperty] public int ExtendBoxBottom;
    /// <summary>
    /// How much to extend the right of the UV coordinate rectangle for each character in pixels.
    /// </summary>
    [UProperty] public int ExtendBoxRight;
    /// <summary>
    /// How much to extend the left of the UV coordinate rectangle for each character in pixels.
    /// </summary>
    [UProperty] public int ExtendBoxLeft;

    /// <summary>
    /// Enables legacy font import mode.<br/>
    /// This results in lower quality antialiasing and larger glyph bounds, but may be useful when debugging problems.
    /// </summary>
    [UProperty] public bool bEnableLegacyMode;

    /// <summary>
    /// The initial horizontal spacing adjustment between rendered characters.<br/>
    /// This setting will be copied directly into the generated Font object's properties.
    /// </summary>
    [UProperty] public int Kerning;

    /// <summary>
    /// If TRUE then the alpha channel of the font textures will store a distance field instead of a color mask.
    /// </summary>
    [UProperty] public bool bUseDistanceFieldAlpha;
    /// <summary>
    /// Scale factor determines how big to scale the font bitmap during import when generating distance field values.<br/>
    /// Note that higher values give better quality but importing will take much longer.
    /// </summary>
    [UProperty] public int DistanceFieldScaleFactor = 16;
    /// <summary>
    /// Shrinks or expands the scan radius used to determine the silhouette of the font edges.
    /// </summary>
    // ClampMin=0.0, ClampMax=4.0
    [UProperty] public float DistanceFieldScanRadiusScale = 1.0f;

    #endregion
}