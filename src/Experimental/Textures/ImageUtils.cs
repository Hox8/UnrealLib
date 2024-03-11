using System;
using System.Diagnostics;

namespace UnrealLib.Experimental.Textures;

// This class is too allocate-y.
// @TODO look into using array pools.

/// <summary>
/// Provides common operations on various image formats.
/// </summary>
public static class ImageUtils
{
    private const int BitsPerPixelGray = 8;
    private const int BitsPerPixelRGB = 24;
    private const int BitsPerPixelRGBA = 32;

    /// <summary>
    /// Swaps the endianness of the current image data. Little Endian will become Big Endian and vice-versa.
    /// This effectively swaps the Red and Blue channels.
    /// </summary>
    public static void SwapEndianness(Span<byte> data, int channelCount)
    {
        // Swap channels 0 and 2
        for (int i = 0; i < data.Length; i += channelCount)
        {
            (data[i], data[i + 2]) = (data[i + 2], data[i]);
        }
    }

    /// <summary>
    /// Removes the alpha channel from an RGBA image.
    /// </summary>
    /// <param name="data">Span containing the image data.</param>
    /// <param name="width">Width of the image, in pixels.</param>
    /// <param name="height">Height of the image, in pixels.</param>
    /// <returns>True if the alpha channel was successfully removed, false if not.</returns>
    public static bool RemoveAlpha(ref Span<byte> data, int width, int height)
    {
        // Make sure we're dealing with an uncompressed RGBA buffer
        if (GetUncompressedSize(width, height, BitsPerPixelRGBA) != data.Length) return false;

        // Allocate buffer to store new RGB data
        Span<byte> rgb = GC.AllocateUninitializedArray<byte>(GetUncompressedSize(width, height, BitsPerPixelRGB));

        // Copy RGB values from existing RGBA data
        for (int src = 0, dst = 0; src < data.Length;)
        {
            rgb[dst++] = data[src++];
            rgb[dst++] = data[src++];
            rgb[dst++] = data[src++];
            src++;
        }

        data = rgb;
        return true;
    }

    /// <summary>
    /// Removes the alpha channel from an RGBA image if all of its alpha pixels are the same value.
    /// </summary>
    /// <param name="data">Span containing the image data.</param>
    /// <param name="width">Width of the image, in pixels.</param>
    /// <param name="height">Height of the image, in pixels.</param>
    /// <returns>True if the alpha channel was deemed garbage and removed, false if not.</returns>
    public static bool RemoveAlphaIfEmpty(ref Span<byte> data, int width, int height)
    {
        // Make sure we're dealing with an uncompressed RGBA buffer
        if (GetUncompressedSize(width, height, BitsPerPixelRGBA) != data.Length) return false;

        // Sample the value of the 2nd alpha pixel.
        // We avoid the first as some images contain a single black pixel at (0,0)
        byte sample = data[7];

        // Check subsequent alpha pixels for differing values
        for (int i = 11; i < data.Length; i+= 4)
        {
            if (data[i] != sample) return false;
        }

        // We didn't find any differing values; we can remove the alpha channel
        RemoveAlpha(ref data, width, height);
        return true;
    }

    /// <summary>
    /// Converts a V8U8 texture (16-bit, two channels) to an R8G8B8 format (24-bit, three channels).
    /// </summary>
    /// <remarks>
    /// Does not compute the Z channel; instead fills with full white.
    /// </remarks>
    public static void V8U8toR8G8B8(ref Span<byte> data, int width, int height)
    {
        Span<byte> rgb = GC.AllocateUninitializedArray<byte>(GetUncompressedSize(width, height, BitsPerPixelRGB));

        for (int i = 0, src = 0, dst = 0; i < width * height; i++)
        {
            // Offset by 128 to convert from signed to unsigned
            rgb[dst++] = (byte)(data[src++] + 128);
            rgb[dst++] = (byte)(data[src++] + 128);
            rgb[dst++] = byte.MaxValue; // Not computing Z, so fill with white.
        }

        data = rgb;
    }

    public static void PrepareUE3TextureForExport(ref Span<byte> data, int width, int height, UTexture2D texture)
    {
        // Expand V8U8 (uncompressed normal map) to RGB
        if (texture.Format is Enums.Textures.EPixelFormat.PF_V8U8)
        {
            V8U8toR8G8B8(ref data, width, height);
        }

        // PVRTC textures are saved as BGR; swap them back to RGB.
        if (texture.IsCompressed())
        {
            SwapEndianness(data, 4);
        }
        else Console.WriteLine(texture.Format.ToString());

        // This is removing valid alphas!
        //if (texture.CompressionNoAlpha || texture is ULightMapTexture2D)
        //{
        //    RemoveAlpha(ref data, width, height);
        //}
        //else
        //{
        //    RemoveAlphaIfEmpty(ref data, width, height);
        //}
    }

    /// <summary>
    /// Calculates the number of bytes needed to store an uncompressed buffer at the given height, width, and bpp params.
    /// </summary>
    /// <param name="width">Width of the image, in pixels.</param>
    /// <param name="height">Height of the image, in pixels.</param>
    /// <param name="bitsPerPixel">Bits per pixel. Valid values are: 16, 24, and 32.</param>
    public static int GetUncompressedSize(int width, int height, int bitsPerPixel)
    {
        Debug.Assert(bitsPerPixel == 8 || bitsPerPixel == 24 || bitsPerPixel == 32);
        return width * height * bitsPerPixel / 8;
    }
}
