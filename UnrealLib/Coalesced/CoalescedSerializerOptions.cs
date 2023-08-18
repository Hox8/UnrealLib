namespace UnrealLib.Coalesced;

public sealed class CoalescedSerializerOptions
{
    // public CoalescedCommentHandling CommentHandling = CoalescedCommentHandling.PreserveAll;

    /// <summary>
    /// Forces strings to be serialized as Unicode, even if ASCII-compatible.
    /// <remarks>
    /// Strings requiring Unicode will always be Unicode-encoded.
    /// </remarks>
    /// </summary>
    public bool ForceUnicodeEncoding = false;

    /// <summary>
    /// If true, empty sections and Inis will be removed.
    /// <remarks>
    /// Comment handling occurs first, followed by section pruning, and finally Ini pruning.
    /// </remarks>
    /// </summary>
    // public bool PruneEmptyCode = false;

    /// <summary>
    /// If true, encryption will be used when saving Coalesced file.
    /// </summary>
    public bool SaveEncrypted = true;
}

[Flags]
public enum CoalescedCommentHandling : byte
{
    /// <summary>
    /// All comments will be removed during serialization.
    /// </summary>
    RemoveAll = 0,

    /// <summary>
    /// Single-line comments will not be removed during serialization.
    /// </summary>
    PreserveStandalone = 1 << 0,

    /// <summary>
    /// No comments will be removed during serialization.
    /// </summary>
    PreserveAll = PreserveStandalone
}