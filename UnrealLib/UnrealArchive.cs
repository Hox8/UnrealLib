using System;
using System.IO;

namespace UnrealLib;

// Look into genericizing this class! How to handle default values? Assume the 0th element represents a 'None' state
// public abstract class ErrorHelper<T> where T : Enum
public abstract class ErrorHelper
{
    public UnrealArchiveError Error { get; protected set; } = UnrealArchiveError.None;
    public bool HasError => Error is not UnrealArchiveError.None;
    public virtual string ErrorString => GetString(Error);

    public void SetError(UnrealArchiveError error) => Error = error;
    public static string GetString(UnrealArchiveError error) => error switch
    {
        // Generic
        UnrealArchiveError.None => "No error.",
        UnrealArchiveError.PathInvalid => "File path is invalid.",
        UnrealArchiveError.PathNonexistent => "File path does not exist.",
        UnrealArchiveError.PathNotReadable => "File path does not allow reading.",
        UnrealArchiveError.FailedOverwrite => "Failed to overwrite the output path. Is the file or folder contents in use?",
        UnrealArchiveError.ParseFailed => "Failed to parse the file.",

        // Coalesced
        UnrealArchiveError.InvalidFolder => "Not a valid Coalesced folder.",
        UnrealArchiveError.UnexpectedGame => "Coalesced file does not match the requested game.",
        UnrealArchiveError.DecryptionFailed => "Failed to decrypt the Coalesced file.",
    };
}

public abstract class UnrealArchive : ErrorHelper, IDisposable
{
    protected FileInfo? _fileInfo;
    private bool _disposed;

    public bool Modified { get; set; } = false;

    public string Filename => _fileInfo.Name;
    public string QualifiedPath => _fileInfo.FullName;
    public string ParentPath => _fileInfo.DirectoryName;
    public bool PathIsDirectory => (_fileInfo.Attributes & FileAttributes.Directory) != 0;
    
    
    /// <summary>
    /// Parameterless constructor for when not working with filestreams.
    /// </summary>
    public UnrealArchive() { }

    public UnrealArchive(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            SetError(UnrealArchiveError.PathInvalid);
            return;
        }

        _fileInfo = new FileInfo(filePath);

        try
        {
            // -1 indicates no attributes, meaning the path does not exist
            if ((int)_fileInfo.Attributes == -1)
            {
                SetError(UnrealArchiveError.PathNonexistent);
            }
        }
        catch
        {
            // If we've caught an exception, that means that path was malformed
            SetError(UnrealArchiveError.PathInvalid);
        }
    }

    public abstract bool Load();
    public abstract bool Save(string? path = null);
    public void Dispose()
    {
        if (_disposed) return;

        DisposeUnmanagedResources();
        GC.SuppressFinalize(this);

        _disposed = true;
    }

    public virtual void DisposeUnmanagedResources() { }
}

public enum UnrealArchiveError : byte
{
    // Generic
    None,
    PathInvalid,
    PathNonexistent,
    PathNotReadable,

    FailedOverwrite,

    ParseFailed,

    // UPK

    // Coalesced
    InvalidFolder,
    UnexpectedGame,
    DecryptionFailed
}
