using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealLib.Enums.Textures;

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
public class TGA
{
    public static readonly byte[] Footer = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x54, 0x52, 0x55, 0x45, 0x56, 0x49, 0x53, 0x49, 0x4F, 0x4E, 0x2D, 0x58, 0x46, 0x49, 0x4C, 0x45, 0x2E, 0x00 };

    // Indexed color is not supported!
    // RLE is not supported!

    Header Head = new();
    byte[] Data;

    public bool ContainsAlpha() => Head.Bpp == 32;

    public TGA() { }

    public TGA(byte[] imageData, int width, int height, byte bpp)
    {
        Head = GetHeader(width, height, bpp);
        Data = imageData;
    }
    /// <summary>
    /// Swaps the endianness of the current image data. Little Endian will become Big Endian and vice-versa.
    /// This effectively swaps the Red and Blue channels.
    /// </summary>
    public void SwapEndianness()
    {
        // This would work for 16 bpp also, but it's out of scope for now
        if (Head.Bpp >= 24)
        {
            int channelCount = Head.Bpp / 8;

            // Swap red and blue channels
            for (int i = 0; i < Data.Length; i += channelCount)
            {
                (Data[i], Data[i + 2]) = (Data[i + 2], Data[i]);
            }
        }
    }

    public void RemoveAlpha()
    {
        if (ContainsAlpha())
        {
            // Allocate buffer to store new RGB data
            byte[] rgb = GC.AllocateUninitializedArray<byte>(CalculateUncompressedSize(Head.Width, Head.Height, 3));

            // Copy RGB values from existing RGBA data
            for (int src = 0, dst = 0; src < Data.Length; src += 4, dst += 3)
            {
                Buffer.BlockCopy(Data, src, rgb, dst, 3);
            }

            Head.Bpp = 24;
            Data = rgb;
        }
    }

    public void ClearEmptyAlpha()
    {
        if (ContainsAlpha())
        {
            int channelCount = Head.Bpp / 8;

            // Sample the value of the first alpha pixel
            byte sample = Data[channelCount - 1];

            // Check subsequent pixels for differing values
            for (int i = channelCount * 2 - 1; i < Data.Length; i += channelCount)
            {
                if (Data[i] != sample) return;
            }

            // All alpha pixels share the same value. We can remove the empty channel
            RemoveAlpha();
        }
    }

    /// <summary>
    /// Splits the current RGBA texture into separate RGB + A TGA images.
    /// </summary>
    public TGA? SplitAlpha()
    {
        if (ContainsAlpha())
        {
            // Allocate buffers
            byte[] grayData = GC.AllocateUninitializedArray<byte>(CalculateUncompressedSize(Head.Width, Head.Height, 1));
            byte[] rgbData = GC.AllocateUninitializedArray<byte>(CalculateUncompressedSize(Head.Width, Head.Height, 3));

            // Copy RGB + A pixels to the new buffers
            for (int rgba = 0, rgb = 0, gray = 0; rgba < Data.Length; rgba += 4, rgb += 3, gray++)
            {
                Buffer.BlockCopy(Data, rgba, rgbData, rgb, 3);
                grayData[gray] = Data[rgba + 3];
            }

            // Update metadata + data of new buffers
            Head.Bpp = 24;
            Data = rgbData;

            var tmp = new TGA
            {
                Head = Head,
                Data = grayData
            };

            tmp.Head.ImageType = ImageType.UncompressedGrayscale;
            tmp.Head.Bpp = 8;

            return tmp;
        }

        return null;
    }

    public void Save(string filePath)
    {
        using (var us = new UnrealStream(filePath, FileMode.Create))
        {
            us.StartSaving();
            us.Serialize(ref Head);
            us.Write(Data);
            us.Write(Footer);
        }
    }

    public static void Save(byte[] data, int width, int height, byte bpp, string filePath)
    {
        var tga = new TGA(data, width, height, bpp);
        tga.SwapEndianness();
        tga.ClearEmptyAlpha();
        tga.Save(filePath);
    }

    public static void Save(byte[] data, int width, int height, PixelFormat format, string filePath)
    {
        var tga = new TGA(data, width, height, format is PixelFormat.G8 ? (byte)8 : (byte)32);

        if (format is PixelFormat.V8U8)
        {
            tga.V8U8toR8G8B8();
        }

        tga.ClearEmptyAlpha();
        tga.SwapEndianness();
        tga.Save(filePath);
    }

    /// <summary>
    /// Converts a V8U8 texture (16-bit, two channels) to an R8G8B8 format (24-bit, three channels).
    /// </summary>
    /// <remarks>
    /// Does not compute the Z channel; instead fills with full white.
    /// </remarks>
    private void V8U8toR8G8B8()
    {
        byte[] rgb = GC.AllocateUninitializedArray<byte>(CalculateUncompressedSize(Head.Width, Head.Height, 3));

        for (int i = 0, src = 0, dst = 0; i < Head.Width * Head.Height; i++)
        {
            // Offset by 128 to convert from signed to unsigned
            rgb[dst++] = (byte)(Data[src++] + 128);
            rgb[dst++] = (byte)(Data[src++] + 128);
            rgb[dst++] = byte.MaxValue; // Not computing Z, so fill with white.
        }

        Head.Bpp = 24;
        Data = rgb;
    }

    /// <summary>
    /// Calculates the number of bytes needed to store an RGBA8888 buffer at the given height, width, and bpp params.
    /// </summary>
    /// <param name="width">Width of the image, in pixels.</param>
    /// <param name="height">Height of the image, in pixels.</param>
    /// <param name="bpp">Bits per pixel. Valid values are: 1, 3, and 4.</param>    // What??
    public static int CalculateUncompressedSize(int width, int height, int bpp)
    {
        Debug.Assert(bpp > 0 && bpp <= 4);
        return width * height * bpp;
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
            // _ => throw new Exception("Unsupported bit depth")
        },

        // ASSUMING data is coming from decompressed PVR, set this bit.
        ImageDescriptor = ImageDescriptor.FlipVertical
    };
}
