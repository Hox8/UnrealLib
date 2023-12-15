using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UnrealLib.Experimental.Textures.TGA;

public enum ImageType : byte // Not supporting compressed (RLE) at this time.
{
    None = 0,

    UncompressedIndexed = 1,
    UncompressedTrueColor = 2,
    UncompressedGrayscale = 3,

    CompressedIndexed = 9,
    CompressedTrueColor = 10,
    CompressedGrayscale = 11
}

public enum ImageDescriptor : byte
{
    FlipHorizontal = 16,
    FlipVertical = 32,

    // Not sure about the others. Photoshop uses byte 8, but it doesn't seem to do anything...
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Header
{
    public byte IDLength;
    public byte ColorMapType;
    public ImageType ImageType;

    public short ColorMapOffset;
    public short ColorMapCount;
    public byte ColorMapBpp;

    public short XOrigin;
    public short YOrigin;
    public short Width;
    public short Height;
    public byte Bpp;    // Supported bit depths: 8, 24, 32.
    public ImageDescriptor ImageDescriptor;
}

/// <summary>
/// Quick TGA class I've strung together to test the PVR decompressor.
/// PNG is better than TGA in every aspect (except maybe alpha channels).
/// </summary>
public static class TGA
{
    public static readonly byte[] Footer = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x54, 0x52, 0x55, 0x45, 0x56, 0x49, 0x53, 0x49, 0x4F, 0x4E, 0x2D, 0x58, 0x46, 0x49, 0x4C, 0x45, 0x2E, 0x00 };

    // Indexed color is not supported!
    // RLE is not supported!

    public static void Save(Span<byte> data, int width, int height, UTexture2D texture, string filePath)
    {
        ImageUtils.PrepareUE3TextureForExport(ref data, width, height, texture);

        // var tga = new TGA(data, width, height, format is PixelFormat.G8 ? (byte)8 : (byte)32);
        int bpp = data.Length / width / height * 8;
        var header = GetHeader(width, height, (byte)bpp);

        WriteImageFile(data, header, filePath);
    }

    public static void WriteImageFile(Span<byte> data, Header header, string filePath)
    {
        UnrealArchive Ar = new(filePath, FileMode.Create, FileAccess.Write);
        Ar.StartSaving();

        Ar.Serialize(ref header);
        Ar.Write(data);
        Ar.Write(Footer);
    }

    private static Header GetHeader(int width, int height, byte bpp) => new()
    {
        Width = (short)width,
        Height = (short)height,
        Bpp = bpp,

        ImageType = bpp switch
        {
            8 => ImageType.UncompressedGrayscale,
            24 or 32 => ImageType.UncompressedTrueColor,
            _ => throw new Exception("Unsupported bit depth")
        },

        // Textures coming from UE3 need to have this bit set
        ImageDescriptor = ImageDescriptor.FlipVertical
    };
}
