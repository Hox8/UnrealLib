/*
* Implementation of the Texture Decompression functions.
* PowerVR by Imagination, Developer Technology Team
* Copyright (c) Imagination Technologies Limited.
* 
* Ported from C++ to C#.
* Original GitHub repo: https://github.com/powervr-graphics/Native_SDK
*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UnrealLib.Experimental.Textures.PowerVR;

public static class PVR
{
    #region Structs

    private struct Pixel32(byte r, byte g, byte b, byte a)
    {
        public byte Red = r, Green = g, Blue = b, Alpha = a;
    }

    private struct Pixel128S(int r, int g, int b, int a)
    {
        public int Red = r, Green = g, Blue = b, Alpha = a;
    }

    private struct PVRTCWord(uint modulationData, uint colorData)
    {
        public uint ModulationData = modulationData;
        public uint ColorData = colorData;
    }

    private struct PVRTCWordIndices()
    {
        public int[] P = new int[2], Q = new int[2], R = new int[2], S = new int[2];
    }

    #endregion

    #region Helpers

    private static int WrapWordIndex(int numWords, int word) => (word + numWords) % numWords;

    private static bool IsPowerOf2(int input) => input != 0 && (input & input - 1) == 0;

    private static Pixel32 GetColorA(uint colorData)
    {
        Pixel32 color;

        // Opaque Color Mode - RGB 554
        if ((colorData & 0x8000) != 0)
        {
            color.Red = (byte)((colorData & 0x7c00) >> 10);                     // 5 -> 5 bits
            color.Green = (byte)((colorData & 0x3e0) >> 5);                     // 5 -> 5 bits
            color.Blue = (byte)(colorData & 0x1e | (colorData & 0x1e) >> 4);  // 4 -> 5 bits
            color.Alpha = 0xf;                                                  // 0 bits
        }
        // Transparent Color Mode - ARGB 3443
        else
        {
            color.Red = (byte)((colorData & 0xf00) >> 7 | (colorData & 0xf00) >> 11); // 4 -> 5 bits
            color.Green = (byte)((colorData & 0xf0) >> 3 | (colorData & 0xf0) >> 7);  // 4 -> 5 bits
            color.Blue = (byte)((colorData & 0xe) << 1 | (colorData & 0xe) >> 2);     // 3 -> 5 bits
            color.Alpha = (byte)((colorData & 0x7000) >> 11);                           // 3 -> 4 bits - note 0 at right
        }

        return color;
    }

    private static Pixel32 GetColorB(uint colorData)
    {
        Pixel32 color;

        // Opaque Color Mode - RGB 555
        if ((colorData & 0x80000000) != 0)
        {
            color.Red = (byte)((colorData & 0x7c000000) >> 26);     // 5 -> 5 bits
            color.Green = (byte)((colorData & 0x3e00000) >> 21);    // 5 -> 5 bits
            color.Blue = (byte)((colorData & 0x1f0000) >> 16);      // 5 -> 5 bits
            color.Alpha = 0xf;                                      // 0 bits
        }
        // Transparent Color Mode - ARGB 3444
        else
        {
            color.Red = (byte)((colorData & 0xf000000) >> 23 | (colorData & 0xf000000) >> 27);  // 4 -> 5 bits
            color.Green = (byte)((colorData & 0xf00000) >> 19 | (colorData & 0xf00000) >> 23);  // 4 -> 5 bits
            color.Blue = (byte)((colorData & 0xf0000) >> 15 | (colorData & 0xf0000) >> 19);     // 4 -> 5 bits
            color.Alpha = (byte)((colorData & 0x70000000) >> 27);                                   // 3 -> 4 bits - note 0 at right
        }

        return color;
    }

    private static int TwiddleUV(int xSize, int ySize, int xPos, int yPos)
    {
        // Check the sizes are valid.
        Debug.Assert(yPos < ySize);
        Debug.Assert(xPos < xSize);
        Debug.Assert(IsPowerOf2(ySize));
        Debug.Assert(IsPowerOf2(xSize));

        // Initially assume X is the larger size.
        int minDimension = xSize;
        int maxValue = yPos;
        int twiddled = 0;
        int srcBitPos = 1;
        int shiftCount = 0;

        // If Y is the larger dimension - switch the min/max values.
        if (ySize < xSize)
        {
            minDimension = ySize;
            maxValue = xPos;
        }

        // Step through all the bits in the "minimum" dimension
        while (srcBitPos < minDimension)
        {
            twiddled |= (yPos & srcBitPos) << shiftCount;
            twiddled |= (xPos & srcBitPos) << shiftCount + 1;

            srcBitPos <<= 1;
            shiftCount += 1;
        }

        // Prepend any unused bits
        maxValue >>= shiftCount;
        twiddled |= maxValue << 2 * shiftCount;

        return twiddled;
    }

    #endregion

    #region Private methods

    private static readonly int[] RepVals = [0, 3, 5, 8];

    private static int GetModulationValues(uint[,] modValues, uint[,] modModes, uint xPos, uint yPos, bool do2Bit)
    {
        if (!do2Bit) return (int)modValues[xPos, yPos];

        // extract the modulation value. If a simple encoding
        if (modModes[xPos, yPos] == 0)
        {
            return RepVals[modValues[xPos, yPos]];
        }

        // if this is a stored value
        if (((xPos ^ yPos) & 1) == 0)
        {
            return RepVals[modValues[xPos, yPos]];
        }

        // else average from the neighbours
        // if H&V interpolation...
        if (modModes[xPos, yPos] == 1)
        {
            return (RepVals[modValues[xPos, yPos - 1]] + RepVals[modValues[xPos, yPos + 1]] +
                    RepVals[modValues[xPos - 1, yPos]] + RepVals[modValues[xPos + 1, yPos]] + 2) / 4;
        }
        // else if H-Only
        if (modModes[xPos, yPos] == 2)
        {
            return (RepVals[modValues[xPos - 1, yPos]] + RepVals[modValues[xPos + 1, yPos]] + 1) / 2;
        }

        // else it's V-Only
        return (RepVals[modValues[xPos, yPos - 1]] + RepVals[modValues[xPos, yPos + 1]] + 1) / 2;
    }

    private static void InterpolateColors(Pixel32 p, Pixel32 q, Pixel32 r, Pixel32 s, Span<Pixel128S> pixel, bool do2Bit)
    {
        int wordWidth = do2Bit ? 8 : 4;
        const int wordHeight = 4;

        // Expand pixels to int32
        var hP = new Pixel128S(p.Red, p.Green, p.Blue, p.Alpha);
        var hQ = new Pixel128S(q.Red, q.Green, q.Blue, q.Alpha);
        var hR = new Pixel128S(r.Red, r.Green, r.Blue, r.Alpha);
        var hS = new Pixel128S(s.Red, s.Green, s.Blue, s.Alpha);

        // Get vectors
        var qMinusP = new Pixel128S(hQ.Red - hP.Red, hQ.Green - hP.Green, hQ.Blue - hP.Blue, hQ.Alpha - hP.Alpha);
        var sMinusR = new Pixel128S(hS.Red - hR.Red, hS.Green - hR.Green, hS.Blue - hR.Blue, hS.Alpha - hR.Alpha);

        // Multiply colors
        hP.Red *= wordWidth;
        hP.Green *= wordWidth;
        hP.Blue *= wordWidth;
        hP.Alpha *= wordWidth;
        hR.Red *= wordWidth;
        hR.Green *= wordWidth;
        hR.Blue *= wordWidth;
        hR.Alpha *= wordWidth;

        if (do2Bit)
        {
            // Loop through pixels to achieve results.
            for (int x = 0; x < wordWidth; x++)
            {
                var result = new Pixel128S(4 * hP.Red, 4 * hP.Green, 4 * hP.Blue, 4 * hP.Alpha);
                var dY = new Pixel128S(hR.Red - hP.Red, hR.Green - hP.Green, hR.Blue - hP.Blue, hR.Alpha - hP.Alpha);

                for (int y = 0; y < wordHeight; y++)
                {
                    pixel[y * wordWidth + x].Red = (result.Red >> 7) + (result.Red >> 2);
                    pixel[y * wordWidth + x].Green = (result.Green >> 7) + (result.Green >> 2);
                    pixel[y * wordWidth + x].Blue = (result.Blue >> 7) + (result.Blue >> 2);
                    pixel[y * wordWidth + x].Alpha = (result.Alpha >> 5) + (result.Alpha >> 1);

                    result.Red += dY.Red;
                    result.Green += dY.Green;
                    result.Blue += dY.Blue;
                    result.Alpha += dY.Alpha;
                }

                hP.Red += qMinusP.Red;
                hP.Green += qMinusP.Green;
                hP.Blue += qMinusP.Blue;
                hP.Alpha += qMinusP.Alpha;

                hR.Red += sMinusR.Red;
                hR.Green += sMinusR.Green;
                hR.Blue += sMinusR.Blue;
                hR.Alpha += sMinusR.Alpha;
            }
        }
        else
        {
            // Loop through pixels to achieve results.
            for (int y = 0; y < wordHeight; y++)
            {
                var result = new Pixel128S(4 * hP.Red, 4 * hP.Green, 4 * hP.Blue, 4 * hP.Alpha);
                var dY = new Pixel128S(hR.Red - hP.Red, hR.Green - hP.Green, hR.Blue - hP.Blue, hR.Alpha - hP.Alpha);

                for (int x = 0; x < wordWidth; x++)
                {
                    pixel[y * wordWidth + x].Red = (result.Red >> 6) + (result.Red >> 1);
                    pixel[y * wordWidth + x].Green = (result.Green >> 6) + (result.Green >> 1);
                    pixel[y * wordWidth + x].Blue = (result.Blue >> 6) + (result.Blue >> 1);
                    pixel[y * wordWidth + x].Alpha = (result.Alpha >> 4) + result.Alpha;

                    result.Red += dY.Red;
                    result.Green += dY.Green;
                    result.Blue += dY.Blue;
                    result.Alpha += dY.Alpha;
                }

                hP.Red += qMinusP.Red;
                hP.Green += qMinusP.Green;
                hP.Blue += qMinusP.Blue;
                hP.Alpha += qMinusP.Alpha;

                hR.Red += sMinusR.Red;
                hR.Green += sMinusR.Green;
                hR.Blue += sMinusR.Blue;
                hR.Alpha += sMinusR.Alpha;
            }
        }
    }

    private static void UnpackModulations(ref readonly PVRTCWord word, uint offsetX, uint offsetY, uint[,] modulationValues, uint[,] modulationModes, bool do2Bit)
    {
        uint wordModMode = word.ColorData & 0x1;
        uint modulationBits = word.ModulationData;

        // Unpack differently depending on 2bpp or 4bpp modes.
        if (do2Bit)
        {
            if (wordModMode != 0)
            {
                // determine which of the three modes are in use:

                // If this is the either the H-only or V-only interpolation mode...
                if ((modulationBits & 0x1) != 0)
                {
                    // look at the "LSB" for the "centre" (V=2,H=4) texel. Its LSB is now
                    // actually used to indicate whether it's the H-only mode or the V-only...

                    // The centre texel data is the at (y==2, x==4) and so its LSB is at bit 20.
                    if ((modulationBits & 0x1 << 20) != 0)
                    {
                        // This is the V-only mode
                        wordModMode = 3;
                    }
                    else
                    {
                        // This is the H-only mode
                        wordModMode = 2;
                    }

                    // Create an extra bit for the centre pixel so that it looks like
                    // we have 2 actual bits for this texel. It makes later coding much easier.
                    if ((modulationBits & 0x1 << 21) != 0)
                    {
                        // set it to produce code for 1.0
                        modulationBits |= 0x1 << 20;
                    }
                    else
                    {
                        // clear it to produce 0.0 code
                        modulationBits &= ~(0x1U << 20);
                    }
                } // end if H-Only or V-Only interpolation mode was chosen

                if ((modulationBits & 0x2) != 0)
                {
                    modulationBits |= 0x1; /*set it*/
                }
                else
                {
                    modulationBits &= ~0x1U; /*clear it*/
                }

                // run through all the pixels in the block. Note we can now treat all the
                // "stored" values as if they have 2bits (even when they didn't!)
                for (byte y = 0; y < 4; y++)
                {
                    for (byte x = 0; x < 8; x++)
                    {
                        // modulationModes[x + offsetX)][y + offsetY)] = WordModMode;
                        modulationModes[x + offsetX, y + offsetY] = wordModMode;

                        // if this is a stored value...
                        if (((x ^ y) & 1) == 0)
                        {
                            modulationValues[x + offsetX, y + offsetY] = modulationBits & 3;
                            modulationBits >>= 2;
                        }
                    }
                }
            }
            // Else if direct encoded 2bit mode - i.e. 1 mode bit per pixel
            else
            {
                for (byte y = 0; y < 4; y++)
                {
                    for (byte x = 0; x < 8; x++)
                    {
                        modulationModes[x + offsetX, y + offsetY] = wordModMode;

                        // Double the bits so 0 => 00, and 1 => 11
                        modulationValues[x + offsetX, y + offsetY] = (modulationBits & 1) != 0 ? 0x3U : 0x0U;

                        modulationBits >>= 1;
                    }
                }
            }
        }
        // Else if bpp == 4
        else
        {
            // Much simpler than the 2bpp decompression, only two modes, so the n/8 values are set directly.
            // run through all the pixels in the word.
            if (wordModMode != 0)
            {
                for (byte y = 0; y < 4; y++)
                {
                    for (byte x = 0; x < 4; x++)
                    {
                        uint value = modulationBits & 3;

                        if (value == 1) value = 4;
                        else if (value == 2) value = 14;    // +10 tells the decompressor to punch through alpha.
                        else if (value == 3) value = 8;

                        modulationValues[y + offsetY, x + offsetX] = value;

                        modulationBits >>= 2;
                    }
                }
            }
            else
            {
                for (byte y = 0; y < 4; y++)
                {
                    for (byte x = 0; x < 4; x++)
                    {
                        uint value = (modulationBits & 3) * 3;

                        if (value > 3) value -= 1;
                        modulationValues[y + offsetY, x + offsetX] = value;

                        modulationBits >>= 2;
                    }
                }
            }
        }
    }

    private static void MapDecompressedData(Span<Pixel32> output, int width, Span<Pixel32> word, ref readonly PVRTCWordIndices words, bool do2Bit)
    {
        int wordWidth = do2Bit ? 8 : 4;
        int wordHeight = 4;

        for (int y = 0; y < wordHeight / 2; y++)
        {
            for (int x = 0; x < wordWidth / 2; x++)
            {
                output[(words.P[1] * wordHeight + y + wordHeight / 2) * width + words.P[0] * wordWidth + x + wordWidth / 2] = word[y * wordWidth + x]; // map P

                output[(words.Q[1] * wordHeight + y + wordHeight / 2) * width + words.Q[0] * wordWidth + x] = word[y * wordWidth + x + wordWidth / 2]; // map Q

                output[(words.R[1] * wordHeight + y) * width + words.R[0] * wordWidth + x + wordWidth / 2] = word[(y + wordHeight / 2) * wordWidth + x]; // map R

                output[(words.S[1] * wordHeight + y) * width + words.S[0] * wordWidth + x] = word[(y + wordHeight / 2) * wordWidth + x + wordWidth / 2]; // map S
            }
        }
    }

    // 4bpp only needs 16 values, but 2bpp needs 32, so rather than wasting processor time we just statically allocate 32.
    private static readonly Pixel128S[] upscaledColorA = new Pixel128S[32];
    private static readonly Pixel128S[] upscaledColorB = new Pixel128S[32];

    // 4bpp only needs 8*8 values, but 2bpp needs 16*8, so rather than wasting processor time we just statically allocate 16*8.
    private static readonly uint[,] modulationValues = new uint[16, 8];
    // Only 2bpp needs this.
    private static readonly uint[,] modulationModes = new uint[16, 8];

    private static void GetDecompressedPixels(PVRTCWord P, PVRTCWord Q, PVRTCWord R, PVRTCWord S, Span<Pixel32> colorData, bool do2Bit)
    {
        int wordWidth = do2Bit ? 8 : 4;
        int wordHeight = 4;

        // Get the modulations from each word.
        UnpackModulations(ref P, 0, 0, modulationValues, modulationModes, do2Bit);
        UnpackModulations(ref Q, (uint)wordWidth, 0, modulationValues, modulationModes, do2Bit);
        UnpackModulations(ref R, 0, (uint)wordHeight, modulationValues, modulationModes, do2Bit);
        UnpackModulations(ref S, (uint)wordWidth, (uint)wordHeight, modulationValues, modulationModes, do2Bit);

        // Bilinear upscale image data from 2x2 -> 4x4
        InterpolateColors(GetColorA(P.ColorData), GetColorA(Q.ColorData), GetColorA(R.ColorData), GetColorA(S.ColorData), upscaledColorA, do2Bit);
        InterpolateColors(GetColorB(P.ColorData), GetColorB(Q.ColorData), GetColorB(R.ColorData), GetColorB(S.ColorData), upscaledColorB, do2Bit);

        for (int y = 0; y < wordHeight; y++)
        {
            for (int x = 0; x < wordWidth; x++)
            {
                int mod = GetModulationValues(modulationValues, modulationModes, (uint)(x + wordWidth / 2), (uint)(y + wordHeight / 2), do2Bit);
                bool punchThroughAlpha = false;
                if (mod > 10)
                {
                    punchThroughAlpha = true;
                    mod -= 10;
                }

                int index = do2Bit ? y * wordWidth + x : y + x * wordHeight;
                int index2 = y * wordWidth + x;

                colorData[index].Red = (byte)((upscaledColorA[index2].Red * (8 - mod) + upscaledColorB[index2].Red * mod) / 8);
                colorData[index].Green = (byte)((upscaledColorA[index2].Green * (8 - mod) + upscaledColorB[index2].Green * mod) / 8);
                colorData[index].Blue = (byte)((upscaledColorA[index2].Blue * (8 - mod) + upscaledColorB[index2].Blue * mod) / 8);
                colorData[index].Alpha = punchThroughAlpha ? (byte)0 : (byte)((upscaledColorA[index2].Alpha * (8 - mod) + upscaledColorB[index2].Alpha * mod) / 8);
            }
        }
    }

    private static readonly int[] wordOffsets = new int[4];

    private static unsafe void Decompress(ReadOnlySpan<byte> compressedData, Span<Pixel32> decompressedData, int width, int height, bool do2Bit)
    {
        int wordWidth = do2Bit ? 8 : 4;
        const int wordHeight = 4;

        ReadOnlySpan<uint> wordMembers = MemoryMarshal.Cast<byte, uint>(compressedData);

        // Calculate number of words
        int i32NumXWords = width / wordWidth;
        int i32NumYWords = height / wordHeight;

        // Structs used for decompression
        PVRTCWordIndices indices = new();
        Span<Pixel32> pixels = stackalloc Pixel32[wordWidth * wordHeight * sizeof(Pixel32)];

        // For each row of words
        for (int wordY = -1; wordY < i32NumYWords - 1; wordY++)
        {
            // for each column of words
            for (int wordX = -1; wordX < i32NumXWords - 1; wordX++)
            {
                indices.P[0] = WrapWordIndex(i32NumXWords, wordX);
                indices.P[1] = WrapWordIndex(i32NumYWords, wordY);
                indices.Q[0] = WrapWordIndex(i32NumXWords, wordX + 1);
                indices.Q[1] = WrapWordIndex(i32NumYWords, wordY);
                indices.R[0] = WrapWordIndex(i32NumXWords, wordX);
                indices.R[1] = WrapWordIndex(i32NumYWords, wordY + 1);
                indices.S[0] = WrapWordIndex(i32NumXWords, wordX + 1);
                indices.S[1] = WrapWordIndex(i32NumYWords, wordY + 1);

                // Work out the offsets into the twiddle structs, multiply by two as there are two members per word.
                wordOffsets[0] = TwiddleUV(i32NumXWords, i32NumYWords, indices.P[0], indices.P[1]) * 2;
                wordOffsets[1] = TwiddleUV(i32NumXWords, i32NumYWords, indices.Q[0], indices.Q[1]) * 2;
                wordOffsets[2] = TwiddleUV(i32NumXWords, i32NumYWords, indices.R[0], indices.R[1]) * 2;
                wordOffsets[3] = TwiddleUV(i32NumXWords, i32NumYWords, indices.S[0], indices.S[1]) * 2;

                // Access individual elements to fill out PVRTCWord
                PVRTCWord p, q, r, s;
                p.ColorData = wordMembers[wordOffsets[0] + 1];
                p.ModulationData = wordMembers[wordOffsets[0]];
                q.ColorData = wordMembers[wordOffsets[1] + 1];
                q.ModulationData = wordMembers[wordOffsets[1]];
                r.ColorData = wordMembers[wordOffsets[2] + 1];
                r.ModulationData = wordMembers[wordOffsets[2]];
                s.ColorData = wordMembers[wordOffsets[3] + 1];
                s.ModulationData = wordMembers[wordOffsets[3]];

                // Assemble 4 words into struct to get decompressed pixels from
                GetDecompressedPixels(p, q, r, s, pixels, do2Bit);
                MapDecompressedData(decompressedData, width, pixels, ref indices, do2Bit);
            }
        }
    }

    #endregion

    /// <summary>
    /// Decompresses a headless PVRTC-encoded texture to an RGBA8888 buffer.
    /// </summary>
    /// <param name="compressedData">The PVRTC image data. Must NOT contain a header.</param>
    /// <param name="width">Width of the image, in pixels.</param>
    /// <param name="height">Height of the image, in pixels.</param>
    /// <param name="do2Bit">Whether the PVRTC texture is encoded using 2 bits per pixel (true) or 4 bits per pixel (false).</param>
    /// <returns>A byte span containing the decompressed RGBA8888 image data.</returns>
    public static Span<byte> Decompress(ReadOnlySpan<byte> compressedData, int width, int height, bool do2Bit)
    {
        // Bring dimensions up to the minimum supported if any fall short   - Textures cannot be compressed if below these resolutions?
        // width = Math.Max(width, do2Bit ? 16 : 8);
        // height = Math.Max(height, 8);

        // Allocate buffer for decompressed (RGBA8888) data
        Span<byte> decompressedData = GC.AllocateUninitializedArray<byte>(width * height * 4);

        // Decompress surface
        Decompress(compressedData, MemoryMarshal.Cast<byte, Pixel32>(decompressedData), width, height, do2Bit);

        return decompressedData;
    }
}
