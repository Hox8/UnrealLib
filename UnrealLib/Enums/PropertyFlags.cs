using System;

namespace UnrealLib.Enums;

/// <summary>
/// Flags associated with each property in a class, overriding the property's default behavior.
/// </summary>
[Flags]
public enum PropertyFlags : ulong
{
    /// <summary>
    /// Property is user-settable in the editor.
    /// </summary>
    Edit = 1UL << 0,

    /// <summary>
    /// Actor's property always matches class's default actor property.
    /// </summary>
    Const = 1UL << 1,

    /// <summary>
    /// Variable is writable by the input system.
    /// </summary>
    Input = 1UL << 2,

    /// <summary>
    /// Object can be exported with actor.
    /// </summary>
    ExportObject = 1UL << 3,

    /// <summary>
    /// Optional parameter (if PropertyFlags.Param is set).
    /// </summary>
    OptionalParm = 1UL << 4,

    /// <summary>
    /// Property is relevant to network replication.
    /// </summary>
    Net = 1UL << 5,

    /// <summary>
    /// Indicates that elements of an array can be modified, but its size cannot be changed.
    /// </summary>
    EditFixedSize = 1UL << 6,

    /// <summary>
    /// Function/When call parameter.
    /// </summary>
    Parm = 1UL << 7,

    /// <summary>
    /// Value is copied out after function call.
    /// </summary>
    OutParm = 1UL << 8,

    /// <summary>
    /// Property is a short-circuitable evaluation function parm.
    /// </summary>
    SkipParm = 1UL << 9,

    /// <summary>
    /// Return value.
    /// </summary>
    ReturnParm = 1UL << 10,

    /// <summary>
    /// Coerce args into this function parameter.
    /// </summary>
    CoerceParm = 1UL << 11,

    /// <summary>
    /// Property is native: C++ code is responsible for serializing it.
    /// </summary>
    Native = 1UL << 12,

    /// <summary>
    /// Property is transient: shouldn't be saved, zero-filled at load time.
    /// </summary>
    Transient = 1UL << 13,

    /// <summary>
    /// Property should be loaded/saved as permanent profile.
    /// </summary>
    Config = 1UL << 14,

    /// <summary>
    /// Property should be loaded as localizable text.
    /// </summary>
    Localized = 1UL << 15,

    // UNUSED               = 1UL << 16,

    /// <summary>
    /// Property is uneditable in the editor.
    /// </summary>
    EditConst = 1UL << 17,

    /// <summary>
    /// Load config from base class, not subclass.
    /// </summary>
    GlobalConfig = 1UL << 18,

    /// <summary>
    /// Property contains component references.
    /// </summary>
    Component = 1UL << 19,

    /// <summary>
    /// Property should never be exported as NoInit.
    /// </summary>
    AlwaysInit = 1UL << 20,

    /// <summary>
    /// Property should always be reset to the default value during any type of duplication (copy/paste, binary duplication, etc.)
    /// </summary>
    DuplicateTransient = 1UL << 21,

    /// <summary>
    /// Fields need construction/destruction.
    /// </summary>
    NeedCtorLink = 1UL << 22,

    /// <summary>
    /// Property should not be exported to the native class header file.
    /// </summary>
    NoExport = 1UL << 23,

    /// <summary>
    /// Property should not be imported when creating an object from text (copy/paste)
    /// </summary>
    NoImport = 1UL << 24,

    /// <summary>
    /// Hide clear (and browse) button.
    /// </summary>
    NoClear = 1UL << 25,

    /// <summary>
    /// Edit this object reference inline.
    /// </summary>
    EditInline = 1UL << 26,

    // UNUSED               = 1UL << 27,

    /// <summary>
    /// EditInline with Use button.
    /// </summary>
    EditInlineUse = 1UL << 28,

    /// <summary>
    /// Property is deprecated. Read it from an archive, but don't save it.
    /// </summary>
    Deprecated = 1UL << 29,

    /// <summary>
    /// Indicates that this property should be exposed to data stores
    /// </summary>
    DataBinding = 1UL << 30,

    /// <summary>
    /// Native property should be serialized as text (ImportText, ExportText)
    /// </summary>
    SerializeText = 1UL << 31,

    /// <summary>
    /// Notify actors when a property is replicated.
    /// </summary>
    RepNotify = 1UL << 32,

    /// <summary>
    /// Interpolatable property for use with matinee.
    /// </summary>
    Interp = 1UL << 33,

    /// <summary>
    /// Property isn't transacted.
    /// </summary>
    NonTransactional = 1UL << 34,

    /// <summary>
    /// Property should only be loaded in the editor.
    /// </summary>
    EditorOnly = 1UL << 35,

    /// <summary>
    /// Property should not be loaded on console (or be a console cooker commandlet).
    /// </summary>
    NotForConsole = 1UL << 36,

    /// <summary>
    /// Retry replication of this property if it fails to be fully sent (e.g. object references not yet available to serialize over the network).
    /// </summary>
    RepRetry = 1UL << 37,

    /// <summary>
    /// Property is const outside of the class it was declared in.
    /// </summary>
    PrivateWrite = 1UL << 38,

    /// <summary>
    /// Property is const outside of the class it was declared in and subclasses.
    /// </summary>
    ProtectedWrite = 1UL << 39,

    /// <summary>
    /// Property should be ignored by archives which have ArIgnoreArchetypeRef set.
    /// </summary>
    ArchetypeProperty = 1UL << 40,

    /// <summary>
    /// Property should never be shown in a properties window.
    /// </summary>
    EditHide = 1UL << 41,

    /// <summary>
    /// Property can be edited using a text dialog box.
    /// </summary>
    EditTextBox = 1UL << 42,

    // UNUSED               = 1UL << 43,

    /// <summary>
    /// Property can point across levels, and will be serialized properly, but assumes it's target exists in-game (non-editor).
    /// </summary>
    CrossLevelPassive = 1UL << 44,

    /// <summary>
    /// Property can point across levels, and will be serialized properly, and will be updated when the target is streamed in/out.
    /// </summary>
    CrossLevelActive = 1UL << 45
}