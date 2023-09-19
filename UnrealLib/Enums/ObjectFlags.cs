using System;

namespace UnrealLib.Enums;

/// <summary>
/// Flags describing a UObject instance.
/// </summary>
[Flags]
public enum ObjectFlags : ulong
{
    /// <summary>
    /// In a singular function.
    /// </summary>
    InSingularFunc = 1UL << 1,

    /// <summary>
    /// Object did a state change.
    /// </summary>
    StateChanged = 1UL << 2,

    /// <summary>
    /// For debugging PostLoad calls.
    /// </summary>
    DebugPostLoad = 1UL << 3,

    /// <summary>
    /// For debugging Serialize calls.
    /// </summary>
    DebugSerialize = 1UL << 4,

    /// <summary>
    /// For debugging FinishDestroy calls.
    /// </summary>
    DebugFinishDestroyed = 1UL << 5,

    /// <summary>
    /// Object is selected in one of the editors browser windows.
    /// </summary>
    EdSelected = 1UL << 6,

    /// <summary>
    /// This component's template was deleted, so should not be used.
    /// </summary>
    ZombieComponent = 1UL << 7,

    /// <summary>
    /// Property is protected (may only be accessed from its owner class or subclasses).
    /// </summary>
    Protected = 1UL << 8,

    /// <summary>
    /// This object is its class's default object.
    /// </summary>
    ClassDefaultObject = 1UL << 9,

    /// <summary>
    /// This object is a template for another object - treat like a class default object.
    /// </summary>
    ArchetypeObject = 1UL << 10,

    /// <summary>
    /// Forces this object to be put into the export table when saving a package regardless of outer.
    /// </summary>
    ForceTagExp = 1UL << 11,

    /// <summary>
    /// Set if reference token stream has already been assembled.
    /// </summary>
    TokenStreamAssembled = 1UL << 12,

    /// <summary>
    /// Object's size no longer matches the size of its C++ class (only used during make, for native classes whose properties have changed).
    /// </summary>
    MisalignedObject = 1UL << 13,

    /// <summary>
    /// Object will not be garbage collected, even if unreferenced.
    /// </summary>
    RootSet = 1UL << 14,

    /// <summary>
    /// BeginDestroy has been called on the object.
    /// </summary>
    BeginDestroyed = 1UL << 15,

    /// <summary>
    /// FinishDestroy has been called on the object.
    /// </summary>
    FinishDestroyed = 1UL << 16,

    /// <summary>
    /// Whether object is rooted as being part of the root set (garbage collection).
    /// </summary>
    DebugBeginDestroyed = 1UL << 17,

    /// <summary>
    /// Marked by content cooker.
    /// </summary>
    MarkedByCooker = 1UL << 18,

    /// <summary>
    /// Whether resource object is localized.
    /// </summary>
    LocalizedResource = 1UL << 19,

    /// <summary>
    /// Whether InitProperties has been called on this object.
    /// </summary>
    InitializedProps = 1UL << 20,

    /// <summary>
    /// Indicates that this struct will receive additional member properties from the script patcher.
    /// </summary>
    PendingFieldPatches = 1UL << 21,

    /// <summary>
    /// This object has been pointed to by a cross-level reference, and therefore requires additional cleanup upon deletion.
    /// </summary>
    IsCrossLevelReferenced = 1UL << 22,

    /// <summary>
    /// Object has been saved via SavePackage. Temporary.
    /// </summary>
    Saved = 1UL << 31,

    /// <summary>
    /// Object is transactional.
    /// </summary>
    Transactional = 1UL << 32,

    /// <summary>
    /// Object is not reachable on the object graph.
    /// </summary>
    Unreachable = 1UL << 33,

    /// <summary>
    /// Object is visible outside its package.
    /// </summary>
    Public = 1UL << 34,

    /// <summary>
    /// Temporary import tag in load/save.
    /// </summary>
    TagImp = 1UL << 35,

    /// <summary>
    /// Temporary export tag in load/save.
    /// </summary>
    TagExp = 1UL << 36,

    /// <summary>
    /// Object marked as obsolete and should be replaced.
    /// </summary>
    Obsolete = 1UL << 37,

    /// <summary>
    /// Check during garbage collection.
    /// </summary>
    TagGarbage = 1UL << 38,

    /// <summary>
    /// Object is being disregard for GC as its static and itself and all references are always loaded.
    /// </summary>
    DisregardForGC = 1UL << 39,

    /// <summary>
    /// Object is localized by instance name, not by class.
    /// </summary>
    PerObjectLocalized = 1UL << 40,

    /// <summary>
    /// During load, indicates object needs loading.
    /// </summary>
    NeedLoad = 1UL << 41,

    /// <summary>
    /// Object is being asynchronously loaded.
    /// </summary>
    AsyncLoading = 1UL << 42,

    /// <summary>
    /// During load, indicates that the object still needs to instance subobjects and fixup serialized component references.
    /// </summary>
    NeedPostLoadSubobjects = 1UL << 43,

    /// <summary>
    /// Suppressed log name.
    /// </summary>
    Suppress = 1UL << 44,

    /// <summary>
    /// Within an EndState call.
    /// </summary>
    InEndState = 1UL << 45,

    /// <summary>
    /// Don't save object.
    /// </summary>
    Transient = 1UL << 46,

    /// <summary>
    /// Whether the object has already been cooked.
    /// </summary>
    Cooked = 1UL << 47,

    /// <summary>
    /// In-file load for client.
    /// </summary>
    LoadForClient = 1UL << 48,

    /// <summary>
    /// In-file load for server.
    /// </summary>
    LoadForServer = 1UL << 49,

    /// <summary>
    /// In-file load for editor.
    /// </summary>
    LoadForEdit = 1UL << 50,

    /// <summary>
    /// Keep object around for editing even if unreferenced.
    /// </summary>
    Standalone = 1UL << 51,

    /// <summary>
    /// Don't load this object for the game client.
    /// </summary>
    NotForClient = 1UL << 52,

    /// <summary>
    /// Don't load this object for the game server.
    /// </summary>
    NotForServer = 1UL << 53,

    /// <summary>
    /// Don't load this object for the editor.
    /// </summary>
    NotForEdit = 1UL << 54,

    /// <summary>
    /// Object needs to be post-loaded.
    /// </summary>
    NeedPostLoad = 1UL << 56,

    /// <summary>
    /// Has execution stack.
    /// </summary>
    HasStack = 1UL << 57,

    /// <summary>
    /// Native (UClass only).
    /// </summary>
    Native = 1UL << 58,

    /// <summary>
    /// Marked (for debugging).
    /// </summary>
    Marked = 1UL << 59,

    /// <summary>
    /// ShutdownAfterError called.
    /// </summary>
    ErrorShutdown = 1UL << 60,

    /// <summary>
    /// Objects that are pending destruction (invalid for gameplay but valid objects).
    /// </summary>
    PendingKill = 1UL << 61,

    /// <summary>
    /// Temporarily marked by content cooker - should be cleared.
    /// </summary>
    /// <remarks>
    /// Not applicable to IB1!
    /// </remarks>
    MarkedByCookerTemp = 1UL << 62,

    /// <summary>
    /// This object was cooked into a startup package.
    /// </summary>
    /// <remarks>
    /// Not applicable to IB1!
    /// </remarks>
    CookedStartupObject = 1UL << 63,

    /// <summary>
    /// All context flags.
    /// </summary>
    ContextFlags = NotForClient | NotForServer | NotForEdit,

    /// <summary>
    /// Flags affecting loading.
    /// </summary>
    LoadContextFlags = LoadForClient | LoadForServer | LoadForEdit,

    /// <summary>
    /// Flags to load from Unreal files.
    /// </summary>
    Load = ContextFlags | LoadContextFlags | Public | Standalone | Native | Obsolete | Protected | Transactional | HasStack | PerObjectLocalized | ClassDefaultObject | ArchetypeObject | LocalizedResource,

    /// <summary>
    /// Flags to persist across loads.
    /// </summary>
    Keep = Native | Marked | PerObjectLocalized | MisalignedObject | DisregardForGC | RootSet | LocalizedResource,

    /// <summary>
    /// Script-accessible flags.
    /// </summary>
    ScriptMask = Transactional | Public | Transient | NotForClient | NotForServer | NotForEdit | Standalone,

    /// <summary>
    /// Undo/ redo will store/ restore these.
    /// </summary>
    UndoRedoMask = PendingKill,

    /// <summary>
    /// Sub-objects will inherit these flags from their SuperObject.
    /// </summary>
    PropagateToSubObjects = Public | ArchetypeObject | Transactional,

    /// <summary>
    /// All flags.
    /// </summary>
    AllFlags = ulong.MaxValue
}