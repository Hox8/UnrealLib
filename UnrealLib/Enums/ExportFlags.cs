namespace UnLib.Enums;

[Flags]
public enum ExportFlags : uint
{
    /// No flags.
    None = 0,

    /// Whether the export was forced into the export table via RF_ForceTagExp.
    ForcedExport = 1 << 0,

    /// Indicates that this export was added by the script patcher, so this object's data will come from memory, not disk.
    ScriptPatcherExport = 1 << 1,

    /// Indicates that this export is a UStruct which will be patched with additional member fields by the script patcher.
    MemberFieldPatchPending = 1 << 2,

    /// All flags.
    AllFlags = uint.MaxValue
}