namespace UnrealLib.Core
{
    // @TODO: Do not dump enums here; place them in their relevant files
    // @TODO: All enums should use bitshift expressions over hexadecimal for better readability

    [Flags]
    public enum EObjectFlags : long
    {
        RF_InSingularFunc = 0x0000000000000002,             // In a singular function.
        RF_StateChanged = 0x0000000000000004,               // Object did a state change.
        RF_DebugPostLoad = 0x0000000000000008,              // For debugging PostLoad calls.
        RF_DebugSerialize = 0x0000000000000010,             // For debugging Serialize calls.
        RF_DebugFinishDestroyed = 0x0000000000000020,       // For debugging FinishDestroy calls.
        RF_EdSelected = 0x0000000000000040,                 // Object is selected in one of the editors browser windows.
        RF_ZombieComponent = 0x0000000000000080,            // This component's template was deleted, so should not be used.
        RF_Protected = 0x0000000000000100,                  // Property is protected (may only be accessed from its owner class or subclasses)
        RF_ClassDefaultObject = 0x0000000000000200,         // this object is its class's default object
        RF_ArchetypeObject = 0x0000000000000400,            // this object is a template for another object - treat like a class default object
        RF_ForceTagExp = 0x0000000000000800,                // Forces this object to be put into the export table when saving a package regardless of outer
        RF_TokenStreamAssembled = 0x0000000000001000,       // Set if reference token stream has already been assembled
        RF_MisalignedObject = 0x0000000000002000,           // Object's size no longer matches the size of its C++ class (only used during make, for native classes whose properties have changed)
        RF_RootSet = 0x0000000000004000,                    // Object will not be garbage collected, even if unreferenced.
        RF_BeginDestroyed = 0x0000000000008000,             // BeginDestroy has been called on the object.
        RF_FinishDestroyed = 0x0000000000010000,            // FinishDestroy has been called on the object.
        RF_DebugBeginDestroyed = 0x0000000000020000,        // Whether object is rooted as being part of the root set (garbage collection)
        RF_MarkedByCooker = 0x0000000000040000,             // Marked by content cooker.
        RF_LocalizedResource = 0x0000000000080000,          // Whether resource object is localized.
        RF_InitializedProps = 0x0000000000100000,           // whether InitProperties has been called on this object
        RF_PendingFieldPatches = 0x0000000000200000,        //@script patcher: indicates that this struct will receive additional member properties from the script patcher
        RF_IsCrossLevelReferenced = 0x0000000000400000,     // This object has been pointed to by a cross-level reference, and therefore requires additional cleanup upon deletion
        // unused	0x0000000000800000
        // unused	0x0000000001000000
        // unused	0x0000000002000000
        // unused	0x0000000004000000
        // unused	0x0000000008000000
        // unused	0x0000000010000000
        // unused	0x0000000020000000
        // unused	0x0000000040000000
        RF_Saved = 0x0000000080000000,                      // Object has been saved via SavePackage. Temporary.
        RF_Transactional = 0x0000000100000000,              // Object is transactional.
        RF_Unreachable = 0x0000000200000000,                // Object is not reachable on the object graph.
        RF_Public = 0x0000000400000000,                     // Object is visible outside its package.
        RF_TagImp = 0x0000000800000000,                     // Temporary import tag in load/save.
        RF_TagExp = 0x0000001000000000,                     // Temporary export tag in load/save.
        RF_Obsolete = 0x0000002000000000,                   // Object marked as obsolete and should be replaced.
        RF_TagGarbage = 0x0000004000000000,                 // Check during garbage collection.
        RF_DisregardForGC = 0x0000008000000000,             // Object is being disregard for GC as its static and itself and all references are always loaded.
        RF_PerObjectLocalized = 0x0000010000000000,         // Object is localized by instance name, not by class.
        RF_NeedLoad = 0x0000020000000000,                   // During load, indicates object needs loading.
        RF_AsyncLoading = 0x0000040000000000,               // Object is being asynchronously loaded.
        RF_NeedPostLoadSubobjects = 0x0000080000000000,     // During load, indicates that the object still needs to instance subobjects and fixup serialized component references
        RF_Suppress = 0x0000100000000000,                   // @warning: Mirrored in UnName.h. Suppressed log name.
        RF_InEndState = 0x0000200000000000,                 // Within an EndState call.
        RF_Transient = 0x0000400000000000,                  // Don't save object.
        RF_Cooked = 0x0000800000000000,                     // Whether the object has already been cooked
        RF_LoadForClient = 0x0001000000000000,              // In-file load for client.
        RF_LoadForServer = 0x0002000000000000,              // In-file load for client.
        RF_LoadForEdit = 0x0004000000000000,                // In-file load for client.
        RF_Standalone = 0x0008000000000000,                 // Keep object around for editing even if unreferenced.
        RF_NotForClient = 0x0010000000000000,               // Don't load this object for the game client.
        RF_NotForServer = 0x0020000000000000,               // Don't load this object for the game server.
        RF_NotForEdit = 0x0040000000000000,                 // Don't load this object for the editor.
        // unused	0x0080000000000000
        RF_NeedPostLoad = 0x0100000000000000,               // Object needs to be postloaded.
        RF_HasStack = 0x0200000000000000,                   // Has execution stack.
        RF_Native = 0x0400000000000000,                     // Native (UClass only).
        RF_Marked = 0x0800000000000000,                     // Marked (for debugging).
        RF_ErrorShutdown = 0x1000000000000000,              // ShutdownAfterError called.
        RF_PendingKill = 0x2000000000000000,	            // Objects that are pending destruction (invalid for gameplay but valid objects)
        // unused   0x4000000000000000
    }

    [Flags]
    public enum EPackageFlags : uint
    {
        PKG_AllowDownload = 0x00000001,                 // Allow downloading package.
        PKG_ClientOptional = 0x00000002,                // Purely optional for clients.
        PKG_ServerSideOnly = 0x00000004,                // Only needed on the server side.
        PKG_Cooked = 0x00000008,                        // Whether this package has been cooked for the target platform.
        PKG_Unsecure = 0x00000010,                      // Not trusted.
        PKG_SavedWithNewerVersion = 0x00000020,         // Package was saved with newer version.
        PKG_Need = 0x00008000,                          // Client needs to download this package.
        PKG_Compiling = 0x00010000,                     // package is currently being compiled
        PKG_ContainsMap = 0x00020000,                   // Set if the package contains a ULevel/ UWorld object
        PKG_Trash = 0x00040000,                         // Set if the package was loaded from the trashcan
        PKG_DisallowLazyLoading = 0x00080000,           // Set if the archive serializing this package cannot use lazy loading
        PKG_PlayInEditor = 0x00100000,                  // Set if the package was created for the purpose of PIE
        PKG_ContainsScript = 0x00200000,                // Package is allowed to contain UClasses and unrealscript
        PKG_ContainsDebugInfo = 0x00400000,             // Package contains debug info (for UDebugger)
        PKG_RequireImportsAlreadyLoaded = 0x00800000,   // Package requires all its imports to already have been loaded
        PKG_SelfContainedLighting = 0x01000000,         // All lighting in this package should be self contained
        PKG_StoreCompressed = 0x02000000,               // Package is being stored compressed, requires archive support for compression
        PKG_StoreFullyCompressed = 0x04000000,          // Package is serialized normally, and then fully compressed after (must be decompressed before LoadPackage is called)
        PKG_ContainsInlinedShaders = 0x08000000,        // Package was cooked allowing materials to inline their FMaterials (and hence shaders)
        PKG_ContainsFaceFXData = 0x10000000,            // Package contains FaceFX assets and/or animsets
        PKG_NoExportAllowed = 0x20000000,               // Package was NOT created by a modder.  Internal data not for export
        PKG_StrippedSource = 0x40000000,	            // Source has been removed to compress the package size
    }

    /// <summary>
    /// Bitflags used by entries in the export table. EF_ForcedExport is the only meaningful flag in a cooked package
    /// </summary>
    [Flags]
    public enum EExportFlags : uint
    {
        EF_None	                    = 0,                // No flags set
        EF_ForcedExport	            = 1 << 0,           // Whether the export was forced into the export table via RF_ForceTagExp
        EF_ScriptPatcherExport      = 1 << 1,           // indicates that this export was added by the script patcher, so this object's data will come from memory, not disk
        EF_MemberFieldPatchPending  = 1 << 2,           // indicates that this export is a UStruct which will be patched with additional member fields by the script patcher
        EF_AllFlags                 = uint.MaxValue     // All flags set
    }

    #region Textures

    /// <summary>
    /// Represents a texture format used by a Texture2D
    /// </summary>
    public enum EPixelFormat : int
    {
        PF_Unknown = 0,
        PF_A32B32G32R32F,
        PF_A8R8G8B8,
        PF_G8,
        PF_G16,
        PF_DXT1,
        PF_DXT3,
        PF_DXT5,
        PF_UYVY,
        PF_FloatRGB,
        PF_FloatRGBA,
        PF_DepthStencil,
        PF_ShadowDepth,
        PF_FilteredShadowDepth,
        PF_R32F,
        PF_G16R16,
        PF_G16R16F,
        PF_G16R16F_FILTER,
        PF_G32R32F,
        PF_A2B10G10R10,
        PF_A16B16G16R16,
        PF_D24,
        PF_R16F,
        PF_R16F_FILTER,
        PF_BC5,
        PF_V8U8,
        PF_A1,
        PF_MAX
    };

    /// <summary>
    /// Flags used during texture creation. Has no effect during runtime
    /// </summary>
    [Flags]
    public enum ETextureCreateFlags : int
    {
        // Texture is encoded in sRGB gamma space
        TexCreate_SRGB = 1 << 0,
        // Texture can be used as a resolve target (normally not stored in the texture pool)
        TexCreate_ResolveTargetable = 1 << 1,
        // Texture is a depth stencil format that can be sampled
        TexCreate_DepthStencil = 1 << 2,
        // Texture will be created without a packed miptail
        TexCreate_NoMipTail = 1 << 3,
        // Texture will be created with an un-tiled format
        TexCreate_NoTiling = 1 << 4,
        // Texture that for a resolve target will only be written to/resolved once
        TexCreate_WriteOnce = 1 << 5,
        // Texture that may be updated every frame
        TexCreate_Dynamic = 1 << 6,
        // Texture that didn't go through the offline cooker (normally not stored in the texture pool)
        TexCreate_Uncooked = 1 << 7,
        // Allow silent texture creation failure
        TexCreate_AllowFailure = 1 << 8,
        // Disable automatic defragmentation if the initial texture memory allocation fails.
        TexCreate_DisableAutoDefrag = 1 << 9,
        // Create the texture with automatic -1..1 biasing
        TexCreate_BiasNormalMap = 1 << 10,
    };

    #endregion
}
