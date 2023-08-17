namespace UnrealLib.Enums;

// This enum is broken?

[Flags]
public enum ObjectFlags : ulong
{
    /// In a singular function.
    InSingularFunc = 1UL << 1,

    /// Object did a state change.
    StateChanged = 1UL << 2,

    /// For debugging PostLoad calls.
    DebugPostLoad = 1UL << 3,

    /// For debugging Serialize calls.
    DebugSerialize = 1UL << 4,

    /// For debugging FinishDestroy calls.
    DebugFinishDestroyed = 1UL << 5,

    /// Object is selected in one of the editors browser windows.
    EdSelected = 1UL << 6,

    /// This component's template was deleted, so should not be used.
    ZombieComponent = 1UL << 7,

    /// Property is protected (may only be accessed from its owner class or subclasses)
    Protected = 1UL << 8,

    /// This object is its class's default object
    ClassDefaultObject = 1UL << 9,

    /// This object is a template for another object - treat like a class default object
    ArchetypeObject = 1UL << 10,

    /// Forces this object to be put into the export table when saving a package regardless of outer
    ForceTagExp = 1UL << 11,

    /// Set if reference token stream has already been assembled
    TokenStreamAssembled = 1UL << 12,

    /// Object's size no longer matches the size of its C++ class (only used during make, for native classes whose properties have changed)
    MisalignedObject = 1UL << 13,

    /// Object will not be garbage collected, even if unreferenced.
    RootSet = 1UL << 14,

    /// BeginDestroy has been called on the object.
    BeginDestroyed = 1UL << 15,

    /// FinishDestroy has been called on the object.
    FinishDestroyed = 1UL << 16,

    /// Whether object is rooted as being part of the root set (garbage collection)
    DebugBeginDestroyed = 1UL << 17,

    /// Marked by content cooker.
    MarkedByCooker = 1UL << 18,

    /// Whether resource object is localized.
    LocalizedResource = 1UL << 19,

    /// whether InitProperties has been called on this object
    InitializedProps = 1UL << 20,

    /// @script patcher: indicates that this struct will receive additional member properties from the script patcher
    PendingFieldPatches = 1UL << 21,

    /// This object has been pointed to by a cross-level reference, and therefore requires additional cleanup upon deletion
    IsCrossLevelReferenced = 1UL << 22,

    /// Object has been saved via SavePackage. Temporary.
    Saved = 1UL << 31,

    /// Object is transactional.
    Transactional = 1UL << 32,

    /// Object is not reachable on the object graph.
    Unreachable = 1UL << 33,

    /// Object is visible outside its package.
    Public = 1UL << 34,

    /// Temporary import tag in load/save.
    TagImp = 1UL << 35,

    /// Temporary export tag in load/save.
    TagExp = 1UL << 36,

    /// Object marked as obsolete and should be replaced.
    Obsolete = 1UL << 37,

    /// Check during garbage collection.
    TagGarbage = 1UL << 38,

    /// Object is being disregard for GC as its static and itself and all references are always loaded.
    DisregardForGC = 1UL << 39,

    /// Object is localized by instance name, not by class.
    PerObjectLocalized = 1UL << 40,

    /// During load, indicates object needs loading.
    NeedLoad = 1UL << 41,

    /// Object is being asynchronously loaded.
    AsyncLoading = 1UL << 42,

    /// During load, indicates that the object still needs to instance subobjects and fixup serialized component references
    NeedPostLoadSubobjects = 1UL << 43,

    /// @warning: Mirrored in UnName.h. Suppressed log name.
    Suppress = 1UL << 44,

    /// Within an EndState call.
    InEndState = 1UL << 45,

    /// Don't save object.
    Transient = 1UL << 46,

    /// Whether the object has already been cooked
    Cooked = 1UL << 47,

    /// In-file load for client.
    LoadForClient = 1UL << 48,

    /// In-file load for client.
    LoadForServer = 1UL << 49,

    /// In-file load for client.
    LoadForEdit = 1UL << 50,

    /// Keep object around for editing even if unreferenced.
    Standalone = 1UL << 51,

    /// Don't load this object for the game client.
    NotForClient = 1UL << 52,

    /// Don't load this object for the game server.
    NotForServer = 1UL << 53,

    /// Don't load this object for the editor.
    NotForEdit = 1UL << 54,

    /// Object needs to be post-loaded.
    NeedPostLoad = 1UL << 56,

    /// Has execution stack.
    HasStack = 1UL << 57,

    /// Native (UClass only).
    Native = 1UL << 58,

    /// Marked (for debugging).
    Marked = 1UL << 59,

    /// ShutdownAfterError called.
    ErrorShutdown = 1UL << 60,

    /// Objects that are pending destruction (invalid for gameplay but valid objects)
    PendingKill = 1UL << 61,

    /// All context flags.
    ContextFlags = NotForClient | NotForServer | NotForEdit,

    /// Flags affecting loading.
    LoadContextFlags = LoadForClient | LoadForServer | LoadForEdit,

    /// Flags to load from Unrealfiles.
    Load = ContextFlags | LoadContextFlags | Public | Standalone | Native | Obsolete |
           Protected | Transactional | HasStack | PerObjectLocalized | ClassDefaultObject |
           ArchetypeObject | LocalizedResource,

    /// Flags to persist across loads.
    Keep = Native | Marked | PerObjectLocalized | MisalignedObject | DisregardForGC | RootSet |
           LocalizedResource,

    /// Script-accessible flags.
    ScriptMask =
        Transactional | Public | Transient | NotForClient | NotForServer | NotForEdit |
        Standalone,

    /// Undo/redo will store/restore these
    UndoRedoMask = PendingKill,

    /// Sub-objects will inherit these flags from their SuperObject.
    PropagateToSubObjects = Public | ArchetypeObject | Transactional,

    AllFlags = ulong.MaxValue
}