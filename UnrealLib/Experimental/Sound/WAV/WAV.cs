//using System;
//using System.IO;
//using System.Runtime.InteropServices;

//namespace UnrealLib.Experimental.Sound.WAV;

//public enum AudioFormat : short
//{
//    Unknown = 0,
//    /// <summary>Linear Pulse-Code Modulated Audio. Uncompressed.</summary>
//    LPCM = 1,
//    /// <summary>Adaptive Differential Pulse-Code Modulated Audio. Compressed.</summary>
//    ADPCM = 2,
//}

//[StructLayout(LayoutKind.Sequential, Pack = 1)]
//struct RIFFHeader
//{
//    /// <summary>"RIFF"</summary>
//    public int RiffID;
//    public int ChunkSize;
//    /// <summary>"WAVE"</summary>
//    public int WavID;
//}

//[StructLayout(LayoutKind.Sequential, Pack = 1)]
//public struct ChunkHeader
//{
//    public int ChunkID;
//    public int ChunkSize;
//}

//[StructLayout(LayoutKind.Sequential, Pack = 1)]
//public struct WaveFormatEx
//{
//    public ChunkHeader Header;

//    /// <summary>Waveform-audio format type.</summary>
//    public AudioFormat FormatTag;
//    /// <summary>Number of channels in the waveform-audio data. Mono == 1, Stereo == 2.</summary>
//    public short NumChannels;
//    /// <summary>Sample rate, in samples per second. Possible UE3 values are: 44100 or 22050 or 11025 Hz.</summary>
//    public int SamplesPerSec;
//    public int AverageBytesPerSec;
//    public short BlockAlign;
//    public short BitsPerSample;
//    public short Size;  // How many bytes of extra data follow this chunk. See
//    // https://github.com/microsoft/DirectXTK/wiki/Wave-Formats#adpcmwaveformat for info on what this data is
//}

//public class WAV
//{
//    public const uint ValidRiffId = 0x52494646;     // "RIFF"
//    public const uint ValidFormat = 0x57415645;     // "WAVE"
//    public const uint ValidWavID = 0x666D7420;      // "fmt "
//    public const uint ValidDataID = 0x64617461;     // "data"

//    RIFFHeader RiffHeader;
//    WaveFormatEx WaveFormatEx;
//    ChunkHeader DataHeader;

//    WavError Error = WavError.None;
//    public bool HasError => Error is not WavError.None;

//    public bool Read(byte[] data)
//    {
//        var stream = new UnrealStream(data, false);

//        stream.Serialize(ref RiffHeader);

//        if(RiffHeader.RiffID != ValidRiffId)
//        {
//            return false;
//        }

//        if (RiffHeader.WavID != ValidWavID)
//        {
//            return false;
//        }

//        stream.Serialize(ref WaveFormatEx);
//        stream.Position += WaveFormatEx.Size;   // SKip over metadata

//        stream.Serialize(ref DataHeader);

//    }
//}

//public enum WavError
//{
//    None
//}