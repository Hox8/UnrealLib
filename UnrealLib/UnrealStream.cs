using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnrealLib.Interfaces;

namespace UnrealLib;

[Flags]
public enum StreamState : byte
{
    Loading = 1 << 0,
    Saving = 1 << 1
}

public class UnrealStream : IDisposable
{
    #region Private fields

    private readonly Stream Stream;
    private StreamState State;

    #endregion

    #region Properties

    public bool ForceSerializeUnicode { get; set; } = false;

    public bool IsLoading => (State & StreamState.Loading) != 0;
    // public int VersionOverride { get; set; } = 0;   // To be used in future when dealing with differences between the games' internals

    public int Position
    {
        get => (int)Stream.Position;
        set => Stream.Position = value;
    }

    public int Length
    {
        get => (int)Stream.Length;
        set => Stream.SetLength(value);
    }

    #endregion

    #region Constructors

    public UnrealStream(string filePath, FileMode fileMode = FileMode.Open, FileAccess fileAccess = FileAccess.ReadWrite, StreamState defaultState = StreamState.Loading)
    {
        Stream = File.Open(filePath, fileMode, fileAccess, FileShare.Read);
        State = defaultState;
    }

    public UnrealStream(Stream stream, bool makeCopy, StreamState defaultState = StreamState.Loading)
    {
        if (makeCopy)
        {
            Stream = new MemoryStream();
            stream.CopyTo(Stream);
        }
        else
        {
            Stream = stream;    // Referencing an external MemoryStream can be dangerous!
        }

        State = defaultState;
    }

    #endregion

    #region Public methods

    // UnrealStream methods
    public void StartSaving()
    {
        if (!Stream.CanWrite)
        {
            throw new Exception();
        }

        State |= StreamState.Saving;
        State &= ~StreamState.Loading;
    }

    public void StartLoading()
    {
        State |= StreamState.Loading;
        State &= ~StreamState.Saving;
    }

    public void ReadExactly(Span<byte> buffer) => Stream.ReadExactly(buffer);
    public void Write(byte[] buffer, int offset, int count) => Stream.Write(buffer, offset, count);
    public void Write(ReadOnlySpan<byte> buffer) => Stream.Write(buffer);

    public byte[] ToArray()
    {
        if (!Stream.CanRead) throw new Exception("Stream does not support reading! You messed up.");
            
        Position = 0;

        byte[] buffer = GC.AllocateUninitializedArray<byte>(Length);
        ReadExactly(buffer);

        return buffer;
    }

    // Generic IO methods
    public void Open(string path) => throw new NotImplementedException();

    public void Save() => Stream.Close();

    #endregion

    #region Single-value serializers

    /// <summary>
    /// Serializes a single unmanaged type to/from the underlying stream.
    /// </summary>
    public unsafe void Serialize<T>(ref T value) where T : unmanaged
    {
        // I'm generally against using pointers in C#, but seeing as this avoids copying into an intermediate buffer,
        // and it's the backbone of most Serialize() calls, I'm happy to use this here.

        // This points to the value's memory address and wraps a Span around it, so it can be read from / written to
        fixed (T* ptr = &value)
        {
            var span = new Span<byte>(ptr, sizeof(T));

            if (IsLoading) Stream.ReadExactly(span);
            else Stream.Write(span);
        }
    }

    /// <summary>
    /// Serializes a single string to/from the underlying stream.
    /// </summary>
    /// <remarks>
    /// Serialized strings are expected use the following format:
    /// <br/><br/>1. An <see cref="Int32"/> size. Negative values indicate Unicode encoding, positive indicates ASCII.
    /// <br/><br/>2. A null-terminated string in the aforementioned encoding.
    /// </remarks>
    public void Serialize(ref string value)
    {
        Span<byte> lengthBuffer = stackalloc byte[4];
        Span<byte> stringBuffer;

        if (IsLoading)
        {
            Stream.ReadExactly(lengthBuffer);
            int length = MemoryMarshal.Read<int>(lengthBuffer);

            // Reads full C-style string into buffer and trims off null character

            if (length > 0)
            {
                stringBuffer = new byte[length];
                Stream.ReadExactly(stringBuffer);
                value = Encoding.ASCII.GetString(stringBuffer[..^1]);
            }
            else if (length < 0)
            {
                stringBuffer = new byte[length * -2];
                Stream.ReadExactly(stringBuffer);
                value = Encoding.Unicode.GetString(stringBuffer[..^2]);
            }
            else
            {
                value = string.Empty;
            }
        }
        else
        {
            // If string is null or empty, write an int32 0 value
            if (string.IsNullOrEmpty(value))
            {
                lengthBuffer.Clear();
                Stream.Write(lengthBuffer);
            }
            // If string can be turned into ASCII and we aren't forcing Unicode, write ASCII string
            else if (!ForceSerializeUnicode && IsPureASCII(value))
            {
                MemoryMarshal.Write(lengthBuffer, value.Length + 1);
                Stream.Write(lengthBuffer);

                stringBuffer = Encoding.ASCII.GetBytes(value);
                Stream.Write(stringBuffer);
                Stream.WriteByte(0);    // Append ASCII null terminator
            }
            // Write Unicode string
            else
            {
                MemoryMarshal.Write(lengthBuffer, (value.Length + 1) * -1);
                Stream.Write(lengthBuffer);

                stringBuffer = Encoding.Unicode.GetBytes(value);
                Stream.Write(stringBuffer);
                Stream.WriteByte(0);
                Stream.WriteByte(0);    // Append Unicode null terminator
            }
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

    #region List serializers

    public void Serialize<T>(ref List<T> value, int length = -1) where T : unmanaged
    {
        Span<byte> lengthBuffer = stackalloc byte[4];

        if (IsLoading)
        {
            // If length wasn't specified, then read one from stream
            if (length < 0)
            {
                Stream.ReadExactly(lengthBuffer);
                length = MemoryMarshal.Read<int>(lengthBuffer);
            }

            // Initialize list
            value = new List<T>(length);
            CollectionsMarshal.SetCount(value, length);
        }
        else if (length < 0)
        {
            // If length wasn't specified, write list length to disk.
            MemoryMarshal.Write(lengthBuffer, value.Count);
            Stream.Write(lengthBuffer);
        }

        // Serialize each item of the List
        var listSpan = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < listSpan.Length; i++)
        {
            Serialize(ref listSpan[i]);
        }
    }

    public void Serialize(ref List<string> value, int length = -1)
    {
        Span<byte> lengthBuffer = stackalloc byte[4];

        if (IsLoading)
        {
            // If length wasn't specified, then read one from stream
            if (length < 0)
            {
                Stream.ReadExactly(lengthBuffer);
                length = MemoryMarshal.Read<int>(lengthBuffer);
            }

            // Initialize list
            value = new List<string>(length);
            CollectionsMarshal.SetCount(value, length);
        }
        else if (length < 0)
        {
            // If length wasn't specified, write list length to disk.
            MemoryMarshal.Write(lengthBuffer, value.Count);
            Stream.Write(lengthBuffer);
        }

        // Serialize each item of the List
        var listSpan = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < listSpan.Length; i++)
        {
            Serialize(ref listSpan[i]);
        }
    }

    public void Serialize<T>(ref List<T> value, int length = -1, byte _ = 0) where T : ISerializable, new()
    {
        Span<byte> lengthBuffer = stackalloc byte[4];

        if (IsLoading)
        {
            // If length wasn't specified, then read one from stream
            if (length < 0)
            {
                Stream.ReadExactly(lengthBuffer);
                length = MemoryMarshal.Read<int>(lengthBuffer);
            }

            // Initialize list
            value = new List<T>(length);
            CollectionsMarshal.SetCount(value, length);
        }
        else if (length < 0)
        {
            // If length wasn't specified, write List length to disk.
            MemoryMarshal.Write(lengthBuffer, value.Count);
            Stream.Write(lengthBuffer);
        }

        // Serialize each item of the List
        var listSpan = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < listSpan.Length; i++)
        {
            Serialize(ref listSpan[i]);
        }
    }

    #endregion

    #region Helpers

    public static bool IsPureASCII(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] > 127) return false;
        }

        return true;
    }

    #endregion

    public void Dispose()
    {
        Stream.Dispose();
        GC.SuppressFinalize(this);
    }
}