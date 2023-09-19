using System.IO;

namespace UnrealLib;

/// <summary>
/// Base class for Unreal Archive types. Implements common functionality.
/// </summary>
public abstract class UnrealArchive
{
    /// <summary>
    /// Archive filename.
    /// </summary>
    public string Filename { get; private set; }

    /// <summary>
    /// An absolute, platform-specific archive path.
    /// </summary>
    public string QualifiedPath { get; private set; }

    /// <summary>
    /// An absolute, platform-specific path to this archive's parent directory.
    /// </summary>
    public string QualifiedParentPath { get; private set; }

    /// <summary>
    /// A string message describing this archive's latest error. If empty, this archive has no errors.
    /// </summary>
    public string ErrorContext { get; internal set; } = string.Empty;

    /// <summary>
    /// Indicates whether the archive has been modified since opening.
    /// </summary>
    public bool Modified { get; private set; }
    
    public bool HasError => ErrorContext.Length > 0;

    public abstract bool Open(string? path = null);
    public abstract bool Save(string? path = null);
    public abstract bool Init();

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
