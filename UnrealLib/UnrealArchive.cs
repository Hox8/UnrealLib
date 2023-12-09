using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib;

// I've realized UPK archives and Coalesced archives don't exist without an UnrealArchive being used at some point.
// Therefore, this class exists to merge the functionality of UnrealArchive abstract class and UnrealArchive stream class.
// Should make a lot of serializer operations much easier too, especially for UObjects.

// @TODO: Follow style I'm using for Core UObject classes- regions for serialized, transient, and accessors etc
// @TODO: Copy over FileInfo init code from old UnrealArchive.cs here
// @TODO: Investigate the dispose methods of the original UnrealArchive class and port them here
// @TODO: Clean up and organize things

// Enum should use that EnumExtensions attribute from Nuget
public class UnrealArchive : Stream, IDisposable
{
    private Stream _buffer;
    private bool _disposed;

    public ArchiveError Error { get; protected set; }

    #region Accessors

    public string Filename => FileInfo.Name;
    public string QualifiedPath => FileInfo.FullName;
    public string? DirectoryName => FileInfo.DirectoryName;
    public bool PathIsDirectory => (FileInfo.Attributes & FileAttributes.Directory) != 0;

    public bool StartSaving() => IsLoading = false;
    public bool StartLoading() => IsLoading = true;

    #endregion

    public UnrealArchive() { }

    public UnrealArchive(string filePath, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite)
    {
        bool pathMustExist = mode is not (FileMode.Create or FileMode.CreateNew or FileMode.OpenOrCreate);

        _buffer = new FileStream(filePath, mode, access);

        if (!InitFileinfo(filePath, pathMustExist)) return;
        InitialLength = FileInfo.Length;
    }

    public UnrealArchive(byte[] data, bool shouldBeResizable = false)
    {
        if (shouldBeResizable)
        {
            _buffer = new MemoryStream(data.Length);
            _buffer.Write(data);
        }
        else
        {
            _buffer = new MemoryStream(data);
        }

        InitialLength = data.Length;
    }

    protected void SetStream(Stream stream)
    {
        _buffer = stream;
    }

    protected bool InitFileinfo(string filePath, bool pathMustExist)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Error = ArchiveError.PathInvalid;
            return false;
        }

        FileInfo = new FileInfo(filePath);

        try
        {
            // -1 indicates no attributes, meaning the path does not exist
            if ((int)FileInfo.Attributes == -1)
            {
                // If we don't care about existence (probably saving), don't set error.
                // We must still access FileInfo attrs to catch exceptions caused by invalid paths
                if (pathMustExist)
                {
                    Error = ArchiveError.PathNonexistent;
                    return false;
                }
            }
        }
        catch
        {
            // If we've caught an exception, that means that path was malformed
            Error = ArchiveError.PathInvalid;
            return false;
        }

        return true;
    }

    #region Core

    /// <summary>
    /// Influences Unreal package serialization when set.
    /// </summary>
    public int Version { get; internal set; }

    /// <summary>
    /// Returns the Game this Archive's version corresponds to.
    /// </summary>
    /// <remarks>IB2 and VOTE!!! are synonymous.</remarks>
    public Game Game
    {
        // Latest version of IB2 is saved with 864, but I believe this is incorrect. Treat IB2 as 842 for now.
        get => Version switch { > 864 => Game.IB3, > 788 => Game.IB2, 788 => Game.IB1, _ => Game.Unknown };
        protected set => Version = value switch { Game.IB3 => 868, Game.IB2 or Game.Vote => 842, Game.IB1 => 788, _ => 0 };
    }

    /// <summary>
    /// When true, strings will always be serialized as UTF-16.
    /// </summary>
    public bool ForceUTF16 { get; set; } = false;

    /// <summary>
    /// Influences whether data is serialized from or to a data source.
    /// </summary>
    public bool IsLoading { get; private set; } = true;

    #endregion

    #region File IO and stat tracking

    protected FileInfo FileInfo;
    public long InitialLength { get; protected set; }

    #endregion

    #region Error handling

    public enum ArchiveError
    {
        None,

        FailedRead,
        FailedWrite,
        FailedParse,
        FailedDecrypt,
        FailedDecompress,
        FailedDelete,
        FailedSave,

        PathInvalid,
        PathNonexistent,

        UnsupportedDecompress,

        UnexpectedGame,
        InvalidCoalescedFolder,
    }

    public bool HasError => Error is not ArchiveError.None;
    public string ErrorString => Error switch
    {
        ArchiveError.None => "No error",

        ArchiveError.PathNonexistent => "Path does not exist",

        ArchiveError.FailedRead => "File does not support reading",
        ArchiveError.FailedWrite => "File does not support writing",
        ArchiveError.FailedParse => "Failed to parse file",
        ArchiveError.FailedDecrypt => "Failed to decrypt file",
        ArchiveError.FailedDecompress => "Failed to decompress file",
        ArchiveError.FailedDelete => "Failed to remove path",
        ArchiveError.FailedSave => "Failed to perform a save",

        ArchiveError.UnsupportedDecompress => "Compressed archives are not supported"
    };

    #endregion

    #region Stream Api

    public override bool CanRead => _buffer.CanRead;
    public override bool CanSeek => _buffer.CanSeek;
    public override bool CanWrite => _buffer.CanWrite;
    public override long Length => _buffer.Length;
    public override long Position { get => _buffer.Position; set => _buffer.Position = value; }

    public override void Flush() => _buffer.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _buffer.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _buffer.Seek(offset, origin);
    public override void SetLength(long value) => _buffer.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _buffer.Write(buffer, offset, count);

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

    public void Serialize(ref string value)
    {
        int length = 0;

        if (IsLoading)
        {
            Serialize(ref length);
            int numBytes = length < 0 ? length * -2 : length;

            // Allocate temp buffer on the stack if string is small enough
            Span<byte> buffer = numBytes < 384 ? stackalloc byte[numBytes] : new byte[numBytes];
            ReadExactly(buffer);

            value = length == 0 ? string.Empty : length < 0                     // Empty
                ? new string(MemoryMarshal.Cast<byte, char>(buffer[..^2]))      // Utf16
                : Encoding.ASCII.GetString(buffer[..^1]);                       // Ascii
        }
        else
        {
            if (string.IsNullOrEmpty(value))
            {
                Serialize(ref length);
            }
            else if (!ForceUTF16 && Ascii.IsValid(value))
            {
                length = value.Length + 1;  // accommodate ascii null terminator
                Serialize(ref length);
                Write(Encoding.ASCII.GetBytes(value));
                WriteByte(0);
            }
            else
            {
                length = ~value.Length;   // accommodate utf16 null terminator
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

        // Read the contents of the array in one go.
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

    public virtual void Load() { }
    public virtual long Save()
    {
        if (FileInfo is null) return -1;

        Position = 0;

        try
        {
            if (_buffer is MemoryStream)
            {
                using (var fs = File.Create(FileInfo.FullName, 0, FileOptions.SequentialScan))
                {
                    fs.Write(GetBufferRaw(), 0, (int)Length);
                }
            }
            else
            {
                Flush();
            }
        }
        catch
        {
            Error = ArchiveError.FailedWrite;
            return -1;
        }

        return Length;
    }

    /// <summary>
    /// Save overload for where we want to save to a different path.
    /// </summary>
    /// <param name="newFilePath"></param>
    public long Save(string newFilePath)
    {
        InitFileinfo(newFilePath, false);
        return Save();
    }

    public override string ToString() => FileInfo.Name;

    public new void Dispose()
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

public abstract class ErrorHelper<T> where T : Enum
{
    public T Error { get; protected set; }
    public abstract bool HasError { get; }

    public string ErrorString => GetString(Error);
    public void SetError(T error) => Error = error;
    public abstract string GetString(T error);
}
