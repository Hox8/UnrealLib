using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnrealLib.Interfaces;

namespace UnrealLib;

public class UnrealStream : Stream
{
    /// <summary>
    /// Determines the size (in bytes) above which an incoming string must be allocated on the heap.
    /// </summary>
    private const int SmallStringLimit = 256;

    private readonly Stream _buffer;

    #region Properties

    /// <summary>
    /// Influences whether data is serialized from or to a data source.
    /// </summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>
    /// When true, strings will always be serialized as UTF-16.
    /// </summary>
    public bool ForceUTF16 { get; set; } = false;

    #endregion

    #region Constructors

    public UnrealStream(string path, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite)
    {
        _buffer = File.Open(path, mode, access, FileShare.Read);
    }

    public UnrealStream(byte[] buffer, bool resizable = false)
    {
        if (resizable)
        {
            _buffer = new MemoryStream();
            _buffer.Write(buffer);
        }
        else
        {
            _buffer = new MemoryStream(buffer);
        }
    }

    #endregion

    #region Serializers - Single values

    /// <summary>
    /// Serializes a single unmanaged type to/from the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Serialize<T>(ref T value) where T : unmanaged
    {
        fixed (T* pValue = &value)
        {
            // Wrap a span around the pointer so it can be used by the Stream API
            var span = new Span<byte>(pValue, sizeof(T));

            if (IsLoading)
            {
                ReadExactly(span);
            }
            else
            {
                Write(span);
            }
        }
    }

    /// <summary>
    /// Serializes a single string to/from the underlying stream.
    /// </summary>
    /// <remarks>
    /// Serialized strings are required to use the following format:
    /// <br/><br/>1. An <see cref="Int32"/> size. Negative values indicate Unicode encoding, positive indicates ASCII.
    /// <br/><br/>2. A null-terminated string in the aforementioned encoding.
    /// </remarks>
    public void Serialize(ref string value)
    {
        int length = 0;

        if (IsLoading)
        {
            Serialize(ref length);
            int absoluteLength = length < 0 ? length * -2 : length;

            // Allocate on the stack if the string is small enough
            var buffer = absoluteLength <= SmallStringLimit
                ? stackalloc byte[absoluteLength]
                : GC.AllocateUninitializedArray<byte>(absoluteLength);

            ReadExactly(buffer);

            // Get string from bytes, ignoring the last w/char (null terminator)
            value = length == 0 ? string.Empty : length < 0                 // Empty string
                ? new string(MemoryMarshal.Cast<byte, char>(buffer[..^2]))  // UTF16 string
                : Encoding.ASCII.GetString(buffer[..^1]);                   // ASCII string
        }
        else
        {
            if (string.IsNullOrEmpty(value))
            {
                Serialize(ref length);
            }
            else if (!ForceUTF16 && Ascii.IsValid(value))
            {
                length = value.Length + 1;  // +1 to accommodate null terminator
                Serialize(ref length);
                Write(Encoding.ASCII.GetBytes(value));
                WriteByte(0);
            }
            else
            {
                length = ~value.Length;     // +1 for null terminator, and negated to indicate UTF16
                Serialize(ref length);
                Write(MemoryMarshal.Cast<char, byte>(value.AsSpan()));
                WriteByte(0);
                WriteByte(0);
            }
        }
    }

    public void Serialize<T1, T2>(ref KeyValuePair<T1, T2> value) where T1 : unmanaged where T2 : unmanaged
    {
        // @WARN: Will raise null exception if uninitialized values are passed
        T1 left = value.Key;
        T2 right = value.Value;

        Serialize(ref left);
        Serialize(ref right);

        if (IsLoading)
        {
            value = new(left, right);
        }
    }

    /// <summary>
    /// Serializes a custom type implementing its own Serialize method.
    /// </summary>
    /// <param name="_">Dummy argument to get around C#'s overload limitations.</param>
    public void Serialize<T>(ref T value, byte _ = 0) where T : ISerializable, new()
    {
        if (IsLoading)
        {
            value = new T();
        }

        value.Serialize(this);
    }

    #endregion

    #region Serializers - List values

    /// <summary>
    /// Helper method used across list serializers.<br/>
    /// Serializes length to/from the stream if non-negative, and resizes the list if loading.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ListSerializationHelper<T>(ref List<T> value, int length = -1)
    {
        // If we haven't passed a length, serialize to/from stream as normal.
        if (length < 0)
        {
            if (!IsLoading)
            {
                // Get list length and serialize it to the stream
                length = value.Count;
            }

            Serialize(ref length);
        }

        // Allocate a new list if we're loading from the stream
        if (IsLoading)
        {
            value = new List<T>(length);
            CollectionsMarshal.SetCount(value, length);
        }
    }

    public void Serialize<T>(ref List<T> value, int length = -1) where T : unmanaged
    {
        ListSerializationHelper(ref value, length);

        var sList = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < sList.Length; i++)
        {
            Serialize(ref sList[i]);
        }
    }

    public void Serialize(ref List<string> value, int length = -1)
    {
        ListSerializationHelper(ref value, length);

        var sList = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < sList.Length; i++)
        {
            Serialize(ref sList[i]);
        }
    }

    public void Serialize<T1, T2>(ref List<KeyValuePair<T1, T2>> value) where T1 : unmanaged where T2 : unmanaged
    {
        ListSerializationHelper(ref value);

        var sList = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < sList.Length; i++)
        {
            Serialize(ref sList[i]);
        }
    }

    public void Serialize<T>(ref List<T> value, int length = -1, byte _ = 0) where T : ISerializable, new()
    {
        ListSerializationHelper(ref value, length);

        var sList = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < sList.Length; i++)
        {
            Serialize(ref sList[i]);
        }
    }

    #endregion

    #region Stream methods

    public void StartSaving() => IsLoading = false;
    public void StartLoading() => IsLoading = true;

    public byte[] ToArray()
    {
        if (_buffer is MemoryStream ms) return ms.ToArray();

        var buffer = GC.AllocateUninitializedArray<byte>((int)Length);
        Position = 0;
        Read(buffer);

        return buffer;
    }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _buffer.Close();
        }
    }

    #endregion
}