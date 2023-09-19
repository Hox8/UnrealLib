using System;

namespace UnrealLib.Enums;

/// <summary>
/// Flags describing an <see cref="UnrealPackage"/>.
/// </summary>
[Flags]
public enum PackageFlags : uint
{
    /// <summary>
    /// Allow downloading package.
    /// </summary>
    AllowDownload = 1U << 0,

    /// <summary>
    /// Purely optional for clients.
    /// </summary>
    ClientOptional = 1U << 1,

    /// <summary>
    /// Only needed on the server side.
    /// </summary>
    ServerSideOnly = 1U << 2,

    /// <summary>
    /// Whether this package has been cooked for the target platform.
    /// </summary>
    Cooked = 1U << 3,

    /// <summary>
    /// Not trusted.
    /// </summary>
    Unsecure = 1U << 4,

    /// <summary>
    /// Package was saved with newer version.
    /// </summary>
    SavedWithNewerVersion = 1U << 5,

    /// <summary>
    /// Client needs to download this package.
    /// </summary>
    Need = 1U << 15,

    /// <summary>
    /// Package is currently being compiled.
    /// </summary>
    Compiling = 1U << 16,

    /// <summary>
    /// Set if the package contains a ULevel/ UWorld object.
    /// </summary>
    ContainsMap = 1U << 17,

    /// <summary>
    /// Set if the package was loaded from the trashcan.
    /// </summary>
    Trash = 1U << 18,

    /// <summary>
    /// Set if the archive serializing this package cannot use lazy loading.
    /// </summary>
    DisallowLazyLoading = 1U << 19,

    /// <summary>
    /// Set if the package was created for the purpose of PIE.
    /// </summary>
    PlayInEditor = 1U << 20,

    /// <summary>
    /// Package is allowed to contain UClasses and UnrealScript.
    /// </summary>
    ContainsScript = 1U << 21,

    /// <summary>
    /// Package contains debug info (for UDebugger).
    /// </summary>
    ContainsDebugInfo = 1U << 22,

    /// <summary>
    /// Package requires all its imports to already have been loaded.
    /// </summary>
    RequireImportsAlreadyLoaded = 1U << 23,

    /// <summary>
    /// All lighting in this package should be self-contained.
    /// </summary>
    /// <remarks>
    /// Only applicable to IB1!
    /// </remarks>
    SelfContainedLighting = 1U << 24,

    /// <summary>
    /// Package is being stored compressed, requires archive support for compression.
    /// </summary>
    StoreCompressed = 1U << 25,

    /// <summary>
    /// Package is serialized normally, and then fully compressed after (must be decompressed before LoadPackage is called).
    /// </summary>
    StoreFullyCompressed = 1U << 26,

    /// <summary>
    /// Package was cooked allowing materials to inline their FMaterials (and hence shaders).
    /// </summary>
    /// <remarks>
    /// Only applicable to IB1!
    /// </remarks>
    ContainsInlinedShaders = 1U << 27,

    /// <summary>
    /// Package contains FaceFX assets and/or animsets.
    /// </summary>
    ContainsFaceFXData = 1U << 28,

    /// <summary>
    /// Package was NOT created by a modder. Internal data not for export.
    /// </summary>
    NoExportAllowed = 1U << 29,

    /// <summary>
    /// Source has been removed to compress the package size.
    /// </summary>
    StrippedSource = 1U << 30,

    /// <summary>
    /// Package has editor-only data filtered.
    /// </summary>
    FilterEditorOnly = 1U << 31
}