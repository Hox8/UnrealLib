using UnrealLib.Core;
using UnrealLib.Enums;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.Sound;

// ADPCM stereo sounds do not play correctly after IB2 v1.0.0!
// Setting NumChannels in the header from 2 to 1 allows it to play, albeit in mono

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

public class SoundNodeWave(FObjectExport export) : SoundNode(export)
{
    #region Properties

    /// <summary>Platform-agnostic compression quality. 1..100 with 1 being best compression and 100 being best quality.</summary>
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

    /// <summary>Playback volume of sound 0 to 1.</summary>
    public float Volume = 0.75f;
    /// <summary>Playback pitch for sound 0.4 to 2.0.</summary>
    public float Pitch = 1.0f;
    /// <summary>Duration of sound in seconds.</summary>
    public float Duration;
    /// <summary>Number of channels of multichannel data; 1 or 2 for regular mono and stereo files.</summary>
    public int NumChannels;
    /// <summary>Cached sample rate for displaying in the tools.</summary>
    public int SampleRate;

    /// <summary>Offsets into the bulk data for the source WAV data.</summary>
    public int[] ChannelOffsets;
    /// <summary>Sizes of the bulk data for the source WAV data.</summary>
    public int[] ChannelSizes;

    /// <summary>Uncompressed WAV data 16-bit in mono or stereo - stereo not allowed for multichannel data.</summary>
    /// <remarks>Used by Infinity Blade I.</remarks>
    public FUntypedBulkData RawData;
    /// <summary>Cached OGG Vorbis data.</summary>
    public FUntypedBulkData CompressedPCData;
    /// <summary>Cached cooked Xbox 360 data to speed up iteration times.</summary>
    public FUntypedBulkData CompressedXbox360Data;
    /// <summary>Cached cooked PS3 data to speed up iteration times.</summary>
    public FUntypedBulkData CompressedPS3Data;
    /// <summary>Cached cooked WiiU data to speed up iteration times.</summary>
    public FUntypedBulkData CompressedWiiUData;
    /// <summary>Cached cooked IPhone data to speed up iteration times.</summary>
    /// <remarks>Used by Infinity Blade II and newer.</remarks>
    public FUntypedBulkData CompressedIPhoneData;
    /// <summary>Cached cooked Flash data to speed up iteration times.</summary>
    public FUntypedBulkData CompressedFlashData;

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
    /// <summary>TRUE if the subtitles display as a sequence of single lines as opposed to multiline.</summary>
    public bool bSingleLine;
    /// <summary>The array of the subtitles for each language. Generated at cook time.</summary>
    public LocalizedSubtitle[] LocalizedSubtitles;

    /// <summary>If on mobile and the platform's DetailMode is less than this value, the sound will be discarded to conserve memory.</summary>
    public EDetailMode MobileDetailMode;

    /// <summary>Path to the resource used to construct this sound node wave.</summary>
    private string SourceFilePath;
    /// <summary>Date/Time-stamp of the file from the last import.</summary>
    private string SourceFileTimestamp;
    #endregion

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref RawData);
        Ar.Serialize(ref CompressedPCData);
        Ar.Serialize(ref CompressedXbox360Data);
        Ar.Serialize(ref CompressedPS3Data);

        // IB1: No
        // IB2: No
        // IB3: Yes
        if (Ar.Version >= 845)
        {
            Ar.Serialize(ref CompressedWiiUData);
        }

        // IB1: No
        // IB2: Yes
        // IB3: Yes
        if (Ar.Version >= 851 || Ar.Game is Game.IB2)
        {
            Ar.Serialize(ref CompressedIPhoneData);
        }

        // IB1: No
        // IB2: No
        // IB3: Yes
        if (Ar.Version >= 854)
        {
            Ar.Serialize(ref CompressedFlashData);
        }
    }
}
