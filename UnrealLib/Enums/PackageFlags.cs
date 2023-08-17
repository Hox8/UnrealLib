namespace UnrealLib.Enums;

// This reflect old flags.

[Flags]
public enum PackageFlags : uint
{
    /// Allow downloading package.
    AllowDownload = 1 << 0,

    /// Purely optional for clients.
    ClientOptional = 1 << 1,

    /// Only needed on the server side.
    ServerSideOnly = 1 << 2,

    /// Whether this package has been cooked for the target platform.
    Cooked = 1 << 3,

    /// Not trusted.
    Unsecure = 1 << 4,

    /// Package was saved with newer version.
    SavedWithNewerVersion = 1 << 5,

    /// Client needs to download this package.
    Need = 1 << 15,

    /// package is currently being compiled
    Compiling = 1 << 16,

    /// Set if the package contains a ULevel/ UWorld object
    ContainsMap = 1 << 17,

    /// Set if the package was loaded from the trashcan
    Trash = 1 << 18,

    /// Set if the archive serializing this package cannot use lazy loading
    DisallowLazyLoading = 1 << 19,

    /// Set if the package was created for the purpose of PIE
    PlayInEditor = 1 << 20,

    /// Package is allowed to contain UClasses and unrealscript
    ContainsScript = 1 << 21,

    /// Package contains debug info (for UDebugger)
    ContainsDebugInfo = 1 << 22,

    /// Package requires all its imports to already have been loaded
    RequireImportsAlreadyLoaded = 1 << 23,

    /// All lighting in this package should be self contained
    SelfContainedLighting = 1 << 24,

    /// Package is being stored compressed, requires archive support for compression
    StoreCompressed = 1 << 25,

    /// Package is serialized normally, and then fully compressed after (must be decompressed before LoadPackage is called)
    StoreFullyCompressed = 1 << 26,

    /// Package was cooked allowing materials to inline their FMaterials (and hence shaders)
    ContainsInlinedShaders = 1 << 27,

    /// Package contains FaceFX assets and/or animsets
    ContainsFaceFXData = 1 << 28,

    /// Package was NOT created by a modder.  Internal data not for export
    NoExportAllowed = 1 << 29,

    /// Source has been removed to compress the package size
    StrippedSource = 1 << 30
}