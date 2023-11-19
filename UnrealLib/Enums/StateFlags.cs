using System;

namespace UnrealLib.Enums;

/// <summary>
/// Flags describing a UState object.
/// </summary>
[Flags]
public enum StateFlags : int
{
    /// <summary>
    /// State should be user-selectable in UnrealEd.
    /// </summary>
    Editable = 1 << 0,

    /// <summary>
    /// State is automatic (the default state).
    /// </summary>
    Auto = 1 << 1,

    /// <summary>
    /// State executes on client side.
    /// </summary>
    Simulated = 1 << 2,

    /// <summary>
    /// State has local variables.
    /// </summary>
    HasLocals = 1 << 3,
};