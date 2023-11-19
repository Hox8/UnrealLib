using System;
using System.IO;

namespace UnrealLib;

// This class is pretty ugly. Added for some unreleased version of IBPatcher? Can't remember...
// Would like to remove. See what I should scavenge

public enum UnrealArchiveState : byte
{
    Unloaded = 0,
    // FailedInit,
    Loaded
}

/// <summary>
/// Base class for Unreal Archive types. Implements common functionality.
/// </summary>
public abstract class UnrealArchive : IDisposable
{
    protected bool _disposed = false;
    
    /// <summary>
    /// Archive filename.
    /// </summary>
    public string Filename { get; protected set; }

    /// <summary>
    /// An absolute, platform-specific archive path.
    /// </summary>
    public string QualifiedPath { get; protected set; }

    /// <summary>
    /// An absolute, platform-specific path to this archive's parent directory.
    /// </summary>
    public string QualifiedParentPath { get; protected set; }

    /// <summary>
    /// A string message describing this archive's latest error. If empty, this archive has no errors.
    /// </summary>
    public string ErrorContext { get; protected set; } = string.Empty;

    /// <summary>
    /// Indicates whether the archive has been modified since opening.
    /// </summary>
    public bool Modified { get; set; }

    public UnrealArchiveState State { get; protected set; } = UnrealArchiveState.Unloaded;
    
    public bool HasError => ErrorContext.Length > 0;

    public abstract bool Open(string? path = null);
    public abstract bool Save(string? path = null);
    public abstract bool Init();
    public abstract void Dispose();

    #region Helpers

    protected void InitPathInfo(string filePath)
    {
        Filename = Path.GetFileName(filePath);
        QualifiedPath = Path.GetFullPath(filePath);
        QualifiedParentPath = QualifiedPath[..^Filename.Length];
    }

    // public string GetWindowsPath() => Globals.GetWindowsPath(QualifiedPath);
    // public string GetUnixPath() => Globals.GetUnixPath(QualifiedPath);

    #endregion
}
