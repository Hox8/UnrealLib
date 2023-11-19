using System;

namespace UnrealLib.Enums;

/// <summary>
/// Flags describing a UClass object.
/// </summary>
[Flags]
public enum ClassFlags : uint
{
    None = 1U << 0,
    /// <summary>Class is abstract and can't be instantiated directly.</summary>
    Abstract = 1U << 1,
    /// <summary>Script has been compiled successfully.</summary>
    Compiled = 1U << 2,
    /// <summary>Load object configuration at construction time.</summary>
    Config = 1U << 3,
    /// <summary>This object type can't be saved; null it out at save time.</summary>
    Transient = 1U << 4,
    /// <summary>Successfully parsed.</summary>
    Parsed = 1U << 5,
    /// <summary>Class contains localized text.</summary>
    Localized = 1U << 6,
    /// <summary>Objects of this class can be safely replaced with default or NULL.</summary>
    SafeReplace = 1U << 7,
    /// <summary>Class is a native class - native interfaces will have Native set, but not RF_Native.</summary>
    Native = 1U << 8,
    /// <summary>Don't export to C++ header.</summary>
    NoExport = 1U << 9,
    /// <summary>Allow users to create in the editor.</summary>
    Placeable = 1U << 10,
    /// <summary>Handle object configuration on a per-object basis, rather than per-class.</summary>
    PerObjectConfig = 1U << 11,
    /// <summary>Replication handled in C++.</summary>
    NativeReplication = 1U << 12,
    /// <summary>Class can be constructed from editinline New button.</summary>
    EditInlineNew = 1U << 13,
    /// <summary>Display properties in the editor without using categories.</summary>
    CollapseCategories = 1U << 14,
    /// <summary>Class is an interface.</summary>
    Interface = 1U << 15,

    #region Deprecated

    // Deprecated - these values now match the values of the ClassCastFlags enum
    /// <summary>IsA UProperty.</summary>
    IsAUProperty = 1U << 16,
    /// <summary>IsA UObjectProperty.</summary>
    IsAUObjectProperty = 1U << 17,
    /// <summary>IsA UBoolProperty.</summary>
    IsAUBoolProperty = 1U << 18,
    /// <summary>IsA UState.</summary>
    IsAUState = 1U << 19,
    /// <summary>IsA UFunction.</summary>
    IsAUFunction = 1U << 20,
    /// <summary>IsA UStructProperty.</summary>
    IsAUStructProperty = 1U << 21,

    #endregion

    /// <summary>
    /// Indicates that this class contains object properties which are marked 'instanced' (or editinline export). Set by the script compiler after all properties in
    /// class have been parsed.Used by the loading code as an optimization to attempt to instance newly added properties only for relevant classes.
    /// </summary>
    HasInstancedProps = 1U << 21,
    /// <summary>Class needs its DefaultProperties imported.</summary>
    NeedsDefProps = 1U << 22,
    /// <summary>Class has component properties.</summary>
    HasComponents = 1U << 23,
    /// <summary>Don't show this class in the editor class browser or edit inline new menus.</summary>
    Hidden = 1U << 24,
    /// <summary>Don't save objects of this class when serializing.</summary>
    Deprecated = 1U << 25,
    /// <summary>Class not shown in editor drop down for class selection.</summary>
    HideDropDown = 1U << 26,
    /// <summary>Class has been exported to a header file.</summary>
    Exported = 1U << 27,
    /// <summary>Class has no UnrealScript counter-part.</summary>
    Intrinsic = 1U << 28,
    /// <summary>Properties in this class can only be accessed from native code.</summary>
    NativeOnly = 1U << 29,
    /// <summary>Handle object localization on a per-object basis, rather than per-class.</summary>
    PerObjectLocalized = 1U << 30,
    /// <summary>This class has properties that are marked with CPF_CrossLevel.</summary>
    HasCrossLevelRefs = 1U << 31,

    /// <summary>Flags to inherit from base class.</summary>
    Inherit = Transient | Config | Localized | SafeReplace | PerObjectConfig | PerObjectLocalized | Placeable
                            | IsAUProperty | IsAUObjectProperty | IsAUBoolProperty | IsAUStructProperty | IsAUState | IsAUFunction
                            | HasComponents | Deprecated | Intrinsic | HasInstancedProps | HasCrossLevelRefs,

    /// <summary>These flags will be cleared by the compiler when the class is parsed during script compilation.</summary>
    RecompilerClear = Inherit | Abstract | NoExport | NativeReplication | Native,

    /// <summary>These flags will be inherited from the base class only for non-intrinsic classes.</summary>
    ScriptInherit = Inherit | EditInlineNew | CollapseCategories,

    AllFlags = uint.MaxValue,
};