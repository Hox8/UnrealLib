namespace UnrealLib.Experimental.Infinity_Blade.FLocalFile;

/// <summary>
/// Holds metadata about a given downloadable file.
/// </summary>
public struct LocalEmsFile
{
    /// <summary>
    /// The user that owns the data (not used for local-only files).
    /// </summary>
    public string McpId;
    /// <summary>
    /// Whether the file is for local-use only, or stored on iCloud.
    /// </summary>
    public int bIsLocalOnly;
    /// <summary>
    /// Hash value, if applicable, of the given file contents.
    /// </summary>
    public string Hash;
    /// <summary>
    /// Filename as downloaded.
    /// </summary>
    public string DLName;
    /// <summary>
    /// Logical filename, maps to the downloaded filename.
    /// </summary>
    public string Filename;
    /// <summary>
    /// File size.
    /// </summary>
    public int FileSize;
    /// <summary>
    /// The time the file was last uploaded, if applicable.
    /// </summary>
    public string UploadedDateTime;
}

public struct LocalFileHeaderCache()
{
    public int VersionNumber = 2;
    public string LastMcpId;
    public LocalEmsFile[] CachedRemoteFileList;
    public LocalEmsFile[] LocalFileList;
}

public struct LocalFileCache()
{
    public string EncryptionKey;    // Does not seem to be serialized
    public LocalFileHeaderCache CacheHeader = new();
}