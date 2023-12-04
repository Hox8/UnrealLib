using UnrealLib.Core;
using UnrealLib.Enums;

namespace UnrealLib.Experimental.Sound;

/// <summary>A line of subtitle text and the time at which it should be displayed.</summary>
public struct SubtitleCue
{
    /// <summary>The text to appear in the subtitle.</summary>
    public string Text;
    /// <summary>The time at which the subtitle is to be displayed, in seconds relative to the beginning of the line.</summary>
    public float Time;
}

/// <summary>A subtitle localized to a specific language.</summary>
public struct LocalizedSubtitle
{
    /// <summary>The 3-letter language for this subtitle.</summary>
    public string LanguageExt;

    /// <summary>
    /// Subtitle cues. If empty, use SoundNodeWave's SpokenText as the subtitle.
    /// Will often be empty, as the contents of the subtitle is commonly identical to what is spoken.
    /// </summary>
    public SubtitleCue[] Subtitles;

    /// <summary>TRUE if this sound is considered to contain mature content.</summary>
    public bool bMature;

    /// <summary>TRUE if the subtitles have been split manually.</summary>
    public bool bManualWordWrap;

    /// <summary>TRUE if the subtitles should be displayed one line at a time.</summary>
    public bool bSingleLine;
}

public class SoundNodeWave(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : SoundNode(stream, pkg, export)
{
    #region Properties

    /// <summary>Platform agnostic compression quality. 1..100 with 1 being best compression and 100 being best quality.</summary>
    public int CompressionQuality = 40;
    /// <summary>If set, forces wave data to be decompressed during playback instead of upfront on platforms that have a choice.</summary>
    public bool bForceRealTimeDecompression;
    /// <summary>If set, the compressor does everything required to make this a seamlessly looping sound.</summary>
    public bool bLoopingSound = true;

    /// <summary>Whether to free the resource data after it has been uploaded to the hardware.</summary>
    private bool bDynamicResource;

    // public bool bUseTTS;
    // public ETTSSpeaker TTSSpeaker;
    // public string SpokenText;

    /// <summary>Set to true for programmatically-generated, streamed audio. Not used from the editor; you should use SoundNodeWaveStreaming.uc for this.</summary>
    private bool bProcedural;

    /// <summary>Playback volume of sound 0 to 1</summary>
    public float Volume = 0.75f;
    /// <summary>Playback pitch for sound 0.4 to 2.0</summary>
    public float Pitch = 1.0f;
    /// <summary>Duration of sound in seconds.</summary>
    public float Duration;
    /// <summary>Number of channels of multichannel data; 1 or 2 for regular mono and stereo files</summary>
    public int NumChannels;
    /// <summary>Cached sample rate for displaying in the tools.</summary>
    public int SampleRate;

    /// <summary>Offsets into the bulk data for the source WAV data.</summary>
    public int[] ChannelOffsets;
    /// <summary>Sizes of the bulk data for the source WAV data.</summary>
    public int[] ChannelSizes;
    /// <summary>Uncompressed WAV data 16-bit in mono or stereo - stereo not allowed for multichannel data.</summary>
    public FUntypedBulkData RawData;

    /// <summary>Cached OGG Vorbis data.</summary>
    private FUntypedBulkData CompressedPCData;
    /// <summary>Cached cooked Xbox 360 data to speed up iteration times.</summary>
    private FUntypedBulkData CompressedXbox360Data;
    /// <summary>Cached cooked PS3 data to speed up iteration times.</summary>
    private FUntypedBulkData CompressedPS3Data;
    /// <summary>Cached cooked WiiU data to speed up iteration times.</summary>
    private FUntypedBulkData CompressedWiiUData;
    /// <summary>Cached cooked IPhone data to speed up iteration times.</summary>
    private FUntypedBulkData CompressedIPhoneData;
    /// <summary>Cached cooked Flash data to speed up iteration times.</summary>
    private FUntypedBulkData CompressedFlashData;

    /// <summary>
    ///  Subtitle cues. If empty, use SpokenText as the subtitle.
    ///  Will often be empty, as the contents of the subtitle is commonly identical to what is spoken.
    /// </summary>
    public SubtitleCue[] Subtitles;
    /// <summary>TRUE if this sound is considered to contain mature content.</summary>
    public bool bMature;
    /// <summary>Provides contextual information for the sound to the translator.</summary>
    public string Comment;
    /// <summary>TRUE if the subtitles have been split manually.</summary>
    public bool bManualWordWrap;
    /// <summary>TRUE if the subtitles display as a sequence of single lines as opposed to multiline</summary>
    public bool bSingleLine;
    /// <summary>The array of the subtitles for each language. Generated at cook time.</summary>
    public LocalizedSubtitle[] LocalizedSubtitles;

    /// <summary>If on mobile and the platform's DetailMode < this value, the sound will be discarded to conserve memory.</summary>
    public EDetailMode MobileDetailMode;

    /// <summary>Path to the resource used to construct this sound node wave.</summary>
    private string SourceFilePath;
    /// <summary>Date/Time-stamp of the file from the last import.</summary>
    private string SourceFileTimestamp;
    #endregion

    public override void Serialize(UnrealStream stream)
    {
        base.Serialize(stream);

        stream.Serialize(ref RawData);
        stream.Serialize(ref CompressedPCData);
        stream.Serialize(ref CompressedXbox360Data);
        stream.Serialize(ref CompressedPS3Data);

        // @versioning If UPK version >= 845
        if (true)
        {
            stream.Serialize(ref CompressedWiiUData);
        }

        // @versioning If UPK version >= 851
        // IB2 v1.3.5 needs this
        if (true)
        {
            stream.Serialize(ref CompressedIPhoneData);
        }

        // @versioning If UPK version >= 854
        if (false)
        {
            stream.Serialize(ref CompressedFlashData);
        }

        // @versioning If UPK version <= 867
        if (false)
        {
            CompressedIPhoneData.RemoveBulkData();
        }
    }
}
