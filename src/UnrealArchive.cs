using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnrealLib.Core.Compression;
using UnrealLib.Enums;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Interfaces;

namespace UnrealLib;

// @TODO: Investigate the dispose methods of the original UnrealArchive class and port them here
// @TODO: Clean up and organize things

// @TODO All enums project-wide should use that EnumExtensions attribute from Nuget or similar!

public enum ArchiveError : byte
{
    None,

    FailedRead,
    FailedWrite,
    FailedParse,
    FailedDecrypt,
    // FailedDecompress,
    FailedDelete,
    FailedSave,

    RequiresFile,
    RequiresFolder,

    PathIllegal,
    PathNonexistent,

    UnsupportedDecompress,

    UnexpectedGame,
    InvalidCoalescedFolder,
    InvalidCoalescedFile
}

// Some ground rules:
// An instance can open (during init/ctor), but it can never re-open to something else. Streams should be immutable and persist for the duration of the class
// Saves can happen unlimited times. FileStream saving to its own path should not really do anything. Otherwise, File.Create().
public class UnrealArchive : ErrorHelper<ArchiveError>, IDisposable
{
    internal Stream _buffer;
    private bool _disposed;
    protected bool _leaveOpen;

    #region Constructors

    internal UnrealArchive(Stream stream)
    {
        _buffer = stream;

        if (stream is FileStream fs)
        {
            LastSavedFullPath = fs.Name;
        }
    }

    public UnrealArchive(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, bool makeCopy = false)
    {
        FileHelper = new(path);

        // Check path is valid
        if (!FileHelper.IsLegallyFormatted)
        {
            // Use FullName here, since any part of the path could be broken
            SetError(ArchiveError.PathIllegal, path);
            return;
        }

        // Check for read access
        if ((access & FileAccess.Read) != 0 && !FileHelper.IsReadable)
        {
            SetError(ArchiveError.FailedRead, FileHelper.Name);
            return;
        }

        // Check for write access
        if ((access & FileAccess.Write) != 0 && !FileHelper.IsWritable)
        {
            SetError(ArchiveError.FailedWrite, FileHelper.Name);
            return;
        }

        // Copy data into a MemoryStream if requested, otherwise open a FileStream
        if (makeCopy)
        {
            _buffer = new MemoryStream();
            _buffer.Write(File.ReadAllBytes(path));
            Position = 0;
        }
        else
        {
            if (mode is FileMode.Open && !FileHelper.Exists)
            {
                SetError(ArchiveError.PathNonexistent, FileHelper.Name);
                return;
            }

            _buffer = FileHelper.Open(mode, access, FileShare.Read);
        }

        LastSavedFullPath = FullName;
    }

    public static UnrealArchive FromFile(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, bool makeCopy = false) => new(path, mode, access, makeCopy);

    #endregion

    #region Core

    [Flags]
    private enum State : byte
    {
        /// <summary>0 == Saving, 1 == Loading</summary>
        Loading = 0,
        /// <summary>0 == not compressing, 1 == compressing</summary>
        Compressing = 1 << 0
    }

    // private State _state = State.Loading;
    public bool IsLoading = true;
    public bool IsCompressing;

    /// <summary>
    /// Influences Unreal package serialization when set.
    /// </summary>
    public int Version { get; internal set; }

    /// <summary>
    /// Returns the Game this Archive's version corresponds to.
    /// </summary>
    /// <remarks>IB2 and VOTE!!! are synonymous.</remarks>
    public Game Game { get; set; }
    //{
    //    // @TODO This has been commented out as VOTE!!! was impossible to get/set. Required for Coalesced
    //    // Latest version of IB2 is saved with 864, but I believe this is incorrect. Treat IB2 as 842 for now.
    //    get => Version switch { > 864 => Game.IB3, > 788 => Game.IB2, 788 => Game.IB1, _ => Game.Unknown };
    //    protected set => Version = value switch { Game.IB3 => 868, Game.IB2 or Game.Vote => 842, Game.IB1 => 788, _ => 0 };
    //}

    /// <summary>
    /// When true, strings will always be serialized as UTF-16.
    /// </summary>
    public bool ForceUTF16 { get; set; } = false;

    /// <summary>
    /// When true, properties use FName serialization. When false, properties are serialized using strings directly.
    /// Useful for scenarios where there is no name table, such as Infinity Blade saves. 
    /// </summary>
    public bool SerializeBinaryProperties { get; set; } = true;

    public bool StartLoading() => IsLoading = true;
    public bool StartSaving() => IsLoading = false;

    #endregion

    #region Accessors

    public override string ToString() => Name;

    #endregion

    #region IO & utilities

    private readonly FileHelper FileHelper;
    public string Name => FileHelper.Name;
    public string FullName => FileHelper.FullName;
    public string DirectoryName => FileHelper.DirectoryName;
    public long StartingLength => FileHelper.StartingLength;
    public long FinalLength => FileHelper.Length;
    public bool IsFile => FileHelper.IsFile;
    public bool IsDirectory => FileHelper.IsDirectory;

    public string LastSavedFullPath;

    #endregion

    #region ErrorHelper

    public override string GetErrorString() => ErrorType switch
    {
        ArchiveError.None => "No error.",

        ArchiveError.PathNonexistent => $"'{ErrorContext}' does not exist.",
        ArchiveError.PathIllegal => $"'{ErrorContext}' is not a valid path.",
        ArchiveError.FailedRead => $"Failed to read from '{ErrorContext}'.",
        ArchiveError.FailedWrite => $"Failed to write to '{ErrorContext}'.",
        ArchiveError.FailedParse => "Failed to parse file",
        ArchiveError.FailedDecrypt => "Failed to decrypt file",
        // ArchiveError.FailedDecompress => "Failed to decompress file",
        ArchiveError.UnsupportedDecompress => $"Unsupported compression scheme",
        ArchiveError.FailedDelete => "Failed to remove path",
        ArchiveError.FailedSave => "Failed to perform a save",

        ArchiveError.RequiresFile => "Operation requires a file",
        ArchiveError.RequiresFolder => "Operation requires a folder",
    };

    #endregion

    #region Stream API

    public long Length { get => _buffer.Length; set => _buffer.SetLength(value); }
    public long Position { get => _buffer.Position; set => _buffer.Position = value; }

    public void Flush() => _buffer.Flush();

    public void Write(byte[] buffer, int offset, int count) => _buffer.Write(buffer, offset, count);
    public void Write(ReadOnlySpan<byte> buffer) => _buffer.Write(buffer);
    public void WriteByte(byte value) => _buffer.WriteByte(value);

    public void ReadExactly(byte[] buffer, int offset, int count) => _buffer.ReadExactly(buffer, offset, count);
    public void ReadExactly(Span<byte> buffer) => _buffer.ReadExactly(buffer);

    /// <summary>
    /// Copies the contents of the stream into a new byte array.
    /// </summary>
    public byte[] ToArray()
    {
        if (_buffer is MemoryStream memStream)
        {
            return memStream.ToArray();
        }

        byte[] buffer = GC.AllocateUninitializedArray<byte>((int)Length);
        Position = 0;
        ReadExactly(buffer);

        return buffer;
    }

    /// <summary>
    /// Retrieves the underlying buffer of the MemoryStream.
    /// </summary>
    public Span<byte> GetBuffer() => GetBufferRaw().AsSpan(0, (int)Length);

    /// <summary>
    /// Retrieves the underlying buffer of the MemoryStream.
    /// </summary>
    /// <remarks>
    /// May include junk data! Use <see cref="GetBuffer"/> whenever possible.
    /// </remarks>
    public byte[] GetBufferRaw()
    {
        if (_buffer is not MemoryStream memStream)
        {
            throw new Exception("Cannot get underlying buffer of FileStream");
        }

        return memStream.GetBuffer();
    }

    #endregion

    #region Serialize API

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(Span<byte> value)
    
    {
        if (IsLoading)
        {
            _buffer.ReadExactly(value);
        }
        else
        {
            _buffer.Write(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Serialize<T>(ref T value) where T : unmanaged
    {
        fixed (void* pValue = &value)
        {
            Serialize(new Span<byte>(pValue, sizeof(T)));
        }
    }

    public unsafe void Serialize(ref string value)
    {
        int length = 0;

        if (IsLoading)
        {
            Serialize(ref length);

            // Cannot create buffer of size 0, so handle here
            if (length == 0)
            {
                value = string.Empty;
                return;
            }

            bool isAscii = length > 0;
            int numBytes = isAscii ? length : length * -2;

            Span<byte> buffer = numBytes < 384 ? stackalloc byte[numBytes] : new byte[numBytes];
            ReadExactly(buffer);

            // Ideally we wouldn't do any copying, but that might be pushing it...
            fixed (void* pValue = &buffer[0]) value = isAscii
                ? new string((sbyte*)pValue)    // Copies and widens ASCII string
                : new string((char*)pValue);    // Copies UTF-16 string
        }
        else
        {
            if (string.IsNullOrEmpty(value))
            {
                Serialize(ref length);
            }
            else if (!ForceUTF16 && Ascii.IsValid(value))
            {
                length = value.Length + 1;  // +1 for ascii null terminator
                Serialize(ref length);
                Write(Encoding.ASCII.GetBytes(value));
                WriteByte(0);
            }
            else
            {
                length = ~value.Length;   // +2 for utf16 null terminator
                Serialize(ref length);
                Write(MemoryMarshal.Cast<char, byte>(value.AsSpan()));
                WriteByte(0);
                WriteByte(0);
            }
        }
    }

    public void Serialize<T>(ref T value, byte _ = 0) where T : ISerializable, new()
    {
        if (IsLoading)
        {
            value = new();
        }

        value.Serialize(this);
    }

    #region Array

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ArrayHelper<T>(ref T[] value, int count)
    {
        // If optional length wasn't provided, serialize it.
        if (count < 0)
        {
            if (!IsLoading)
            {
                count = value.Length;
            }

            Serialize(ref count);
        }

        // If we're loading, allocate array
        if (IsLoading)
        {
            value = new T[count];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Serialize<T>(ref T[] value, int count = -1) where T : unmanaged
    {
        ArrayHelper(ref value, count);
        if (value.Length == 0) return;

        Debug.Assert(value.Length > 0);

        // Serialize the contents of the array in one go.
        fixed (void* pValue = &value[0])
        {
            Serialize(new Span<byte>(pValue, sizeof(T) * value.Length));
        }
    }

    public void Serialize(ref string[] value, int count = -1)
    {
        ArrayHelper(ref value, count);

        for (int i = 0; i < value.Length; ++i)
        {
            Serialize(ref value[i]);
        }
    }

    public void Serialize<T>(ref T[] value, int count = -1, byte _ = 0) where T : ISerializable, new()
    {
        ArrayHelper(ref value, count);

        for (int i = 0; i < value.Length; i++)
        {
            Serialize(ref value[i]);
        }
    }

    #endregion

    #region List

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ListHelper<T>(ref List<T> value, int count)
    {
        // If optional length wasn't provided, serialize it.
        if (count < 0)
        {
            if (!IsLoading)
            {
                count = value.Count;
            }

            Serialize(ref count);
        }

        // If we're loading, initialize and set the count of the list
        if (IsLoading)
        {
            value = new List<T>(count);
            CollectionsMarshal.SetCount(value, count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize<T>(ref List<T> value, int count = -1) where T : unmanaged
    {
        ListHelper(ref value, count);

        // Read the contents of the list in one go.
        var span = MemoryMarshal.Cast<T, byte>(CollectionsMarshal.AsSpan(value));
        Serialize(span);
    }


    public void Serialize(ref List<string> value, int count = -1)
    {
        ListHelper(ref value, count);

        var listSpan = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < value.Count; ++i)
        {
            Serialize(ref listSpan[i]);
        }
    }

    public void Serialize<T>(ref List<T> value, int count = -1, byte _ = 0) where T : ISerializable, new()
    {
        ListHelper(ref value, count);

        var listSpan = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < value.Count; i++)
        {
            Serialize(ref listSpan[i]);
        }
    }

    #endregion

    #endregion

    public unsafe void BulkSerialize<T>(ref T[] value) where T : unmanaged
    {
        int SerializedElementSize = sizeof(T);
        Serialize(ref SerializedElementSize);

        Debug.Assert(SerializedElementSize == sizeof(T));

        int newArrayNum = IsLoading ? default : value.Length;
        Serialize(ref newArrayNum);

        Serialize(ref value, newArrayNum);
    }

    public virtual long SaveToFile(string? path = null)
    {
        if (HasError) return -1;

        Debug.Assert(!IsCompressing);

        // If a path wasn't specified, we're overwriting our current file
        path ??= LastSavedFullPath;

        try
        {
            if (_buffer is MemoryStream)
            {
                using (var fs = File.Create(path, 0, FileOptions.SequentialScan))
                {
                    fs.Write(GetBufferRaw(), 0, (int)Length);
                }
            }
            else
            {
                // If we're saving the file we already have open, flush any buffered data
                if (((FileStream)_buffer).Name == path)
                // if (path == FullName)
                {
                    Flush();
                }
                else
                {
                    // Otherwise create a new file and write to it
                    using (var fs = File.Create(path))
                    {
                        _buffer.Position = 0;
                        _buffer.CopyTo(fs);
                    }
                }

                LastSavedFullPath = Path.GetFullPath(path);
            }
        }
        catch
        {
            SetError(ArchiveError.FailedWrite, null);
            return -1;
        }

        long length = Length;

        if (!_leaveOpen)
        {
            _buffer.Dispose();
        }

        return length;
    }

    /// <summary>
    /// Disposes of the underlying stream, regardless of whether LeaveOpen has been set.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        DisposeUnmanagedResources();
        GC.SuppressFinalize(this);

        _disposed = true;
    }

    public virtual void DisposeUnmanagedResources()
    {
        _buffer.Dispose();
    }
}

public sealed class FileHelper
{
    private readonly FileInfo _fileInfo;
    public readonly long StartingLength;
    public readonly bool IsLegallyFormatted;

    // Names
    public string Name => _fileInfo.Name;
    public string FullName => _fileInfo.FullName;
    public string DirectoryName => _fileInfo.DirectoryName;

    // Attributes
    public bool IsFile => (_fileInfo.Attributes & FileAttributes.Directory) == 0;
    public bool IsDirectory => (_fileInfo.Attributes & FileAttributes.Directory) != 0;
    public bool Exists => (int)_fileInfo.Attributes != -1;
    public long Length => _fileInfo.Length;

    // Permissions
    public bool IsReadable => HasAccess(FileAccess.Read);
    public bool IsWritable => HasAccess(FileAccess.Write);

    public FileHelper(string path)
    {
        try
        {
            _fileInfo = new FileInfo(path);

            if (Exists && IsFile)
            {
                StartingLength = _fileInfo.Length;
            }

            IsLegallyFormatted = true;
        }
        catch // Illegally-formatted paths raise IOException
        {
            IsLegallyFormatted = false;
        }
    }

    public FileStream Open(FileMode mode, FileAccess access, FileShare share) => _fileInfo.Open(mode, access, share);

    private bool HasAccess(FileAccess access)
    {
        if (IsDirectory) return true;

        var options = new FileStreamOptions
        {
            Access = access,
            Mode = FileMode.OpenOrCreate,
            Options = Exists ? FileOptions.None : FileOptions.DeleteOnClose,    // If the entry doesn't exist yet, make sure it's deleted after we create it
        };

        try
        {
            using (File.Open(_fileInfo.FullName, options)) ;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
