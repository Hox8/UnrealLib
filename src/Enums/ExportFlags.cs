using System;

namespace UnrealLib.Enums;

/// <summary>
/// FObjectExport flags.
/// </summary>
[Flags]
public enum ExportFlags : uint
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,

    /// <summary>
    /// Whether the export was forced into the export table via <see cref="ObjectFlags.ForceTagExp"/>.
    /// </summary>
    ForcedExport = 1 << 0,

    /// <summary>
    /// Indicates that this export was added by the script patcher, so this object's data will come from memory, not disk.
    /// </summary>
    ScriptPatcherExport = 1 << 1,

    /// <summary>
    /// Indicates that this export is a UStruct which will be patched with additional member fields by the script patcher.
    /// </summary>
    MemberFieldPatchPending = 1 << 2,

    /// <summary>
    /// All flags.
    /// </summary>
    AllFlags = uint.MaxValue
}