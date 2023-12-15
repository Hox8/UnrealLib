namespace UnrealLib.Enums;

/// <summary>Detail mode for primitive component rendering.</summary>
public enum EDetailMode
{
    DM_Low,
    DM_Medium,
    DM_High,
};

/// <summary>
/// A priority for sorting scene elements by depth.
/// Elements with higher priority occlude elements with lower priority, disregarding distance.
/// </summary>
public enum ESceneDepthPriorityGroup
{
    /// <summary>UnrealEd background scene DPG.</summary>
    SDPG_UnrealEdBackground,
    /// <summary>World scene DPG.</summary>
    SDPG_World,
    /// <summary>Foreground scene DPG.</summary>
    SDPG_Foreground,
    /// <summary>UnrealEd scene DPG.</summary>
    SDPG_UnrealEdForeground,
    /// <summary>After all scene rendering.</summary>
    SDPG_PostProcess
};