using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnrealLib.Core.Interfaces;

namespace UnrealLib.Core;

// @TODO work on dispose pattern
//
// Due to the discovery that ISerializable cannot be used on unmanaged structs,
// perhaps time to think of a new way to serialize?

public class UnrealArchive : IDisposable
{
    /// <summary>
    /// The backing FileStream.
    /// Why is this public??
    /// </summary>
    public Stream _stream;

    // Unsafe. Hacky. Fix me.
    public FileStream FileStream => (FileStream)_stream;

    /// <summary>
    /// Toggle affecting serialization behavior.
    /// </summary>
    public bool IsLoading { get; internal set; } = true;

    private bool _disposed;

    // private bool ForceUTF16 = false;    // Remove?

    #region Constructors

    internal UnrealArchive(Stream stream)
    {
        _stream = stream;
    }

    public UnrealArchive(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite)
    {
        _stream = File.Open(path, mode, access, FileShare.Read);
    }

    public UnrealArchive(string path, FileStreamOptions options)
    {
        _stream = File.Open(path, options);
    }

    #endregion

    #region Public API

    public void StartSaving()
    {
        IsLoading = false;
    }

    public void StartLoading()
    {
        IsLoading = true;
    }

    public void Close()
    {
        _stream.Close();
        Dispose();
    }

    #endregion

    #region Stream API

    public int Length { get => (int)_stream.Length; set => _stream.SetLength(value); }
    public int Position { get => (int)_stream.Position; set => _stream.Position = value; }

    public void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
    public void Write(ReadOnlySpan<byte> buffer) => _stream.Write(buffer);
    public void WriteByte(byte value) => _stream.WriteByte(value);

    public void ReadExactly(byte[] buffer, int offset, int count) => _stream.ReadExactly(buffer, offset, count);
    public void ReadExactly(Span<byte> buffer) => _stream.ReadExactly(buffer);

    #endregion

    #region Serialize API

    // NOTE: Serialize methods with `ISerializeble` generic constraint have a dummy byte parameter that is unused.
    // This is required because C# does not support overloading by constraints. The dummy parameter satisfies C#'s requirements for a valid overload.

    #region Single-value

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(Span<byte> value)
    {
        if (IsLoading)
        {
            _stream.ReadExactly(value);
        }
        else
        {
            _stream.Write(value);
        }
    }

    // This doesn't pin, but the serialization is fast enough to get away with it...
    // If and when this poses a real problem, switch back to pinning via 'fixed'
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Serialize<T>(ref T value) where T : unmanaged
        => Serialize(new Span<byte>(Unsafe.AsPointer(ref value), sizeof(T)));

    // ISerializable
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize<T>([NotNull] ref T? value, byte _ = 0) where T : ISerializable, new()
    {
        if (IsLoading)
        {
            value = new T();
        }

        value!.Serialize(this);
    }

    // UBools are serialized as ints
    public void SerializeUBool(ref bool value)
    {
        int temp = value ? 1 : 0;

        Serialize(ref temp);

        value = temp != 0;
    }

    // String
    public unsafe void Serialize([NotNull] ref string? value)
    {
        int length = 0;

        if (IsLoading)
        {
            Serialize(ref length);

            // Guard against crashes during development
            UDebug.Assert(System.Math.Abs(length) <= ushort.MaxValue);

            // Cannot create buffer of size 0, so handle here
            if (length == 0)
            {
                value = string.Empty;
                return;
            }

            // Strings in Unreal can be ASCII or UTF16 depending on the sign of their size
            bool isAscii = length > 0;
            int numBytes = isAscii ? length : length * -2;

            // Create temporary buffer on the stack if the string is small enough
            Span<byte> buffer = (numBytes < 384) ? stackalloc byte[numBytes] : new byte[numBytes];
            ReadExactly(buffer);

            // Fix buffer in-place and call extern string ctors
            fixed (void* pValue = &buffer[0]) value = isAscii
                ? new string((sbyte*)pValue)    // Copy and widen ASCII string
                : new string((char*)pValue);    // Copy UTF-16 string
        }
        else
        {
            if (string.IsNullOrEmpty(value))
            {
                Serialize(ref length);
            }
            else if (Ascii.IsValid(value))
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

                Write(value.AsBytes());
                WriteByte(0);
                WriteByte(0);
            }
        }

        // This should never be the case
        UDebug.Assert(value is not null);
    }

    #endregion

    #region Array

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ArrayHelper<T>([NotNull] ref T[]? value, int count)
    {
        UDebug.Assert(IsLoading || value is not null, "Trying to serialize null array!");

        // If a length wasn't specified, serialize it now
        if (count < 0)
        {
            if (!IsLoading)
            {
                count = value!.Length;
            }

            Serialize(ref count);
        }

        if (IsLoading)
        {
            value = new T[count];
        }

        // This should never be the case
        UDebug.Assert(value is not null);
    }

    // Unmanaged
    public void Serialize<T>([NotNull] ref T[]? value, int count = -1) where T : unmanaged
    {
        ArrayHelper(ref value, count);

        // Unmanaged types have fixed size, so we can serialize in bulk
        Serialize(value.AsBytes());
    }

    // ISerializable
    public void Serialize<T>([NotNull] ref T[]? value, int count = -1, byte _ = 0) where T : ISerializable, new()
    {
        ArrayHelper(ref value, count);

        for (int i = 0; i < value.Length; i++)
        {
            Serialize(ref value[i]);
        }
    }

    // String
    public void Serialize([NotNull] ref string[]? value, int count = -1)
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
    internal void ListHelper<T>([NotNull] ref List<T>? value, int count = -1)
    {
        UDebug.Assert(IsLoading || value is not null, "Trying to serialize null list!");

        // If a length wasn't specified, serialize it now
        if (count < 0)
        {
            if (!IsLoading)
            {
                count = value!.Count;
            }

            Serialize(ref count);
        }

        if (IsLoading)
        {
            value = new List<T>(count);
            CollectionsMarshal.SetCount(value, count);
        }

        // This should never be the case
        UDebug.Assert(value is not null);
    }

    // Unmanaged
    public void Serialize<T>([NotNull] ref List<T>? value, int count = -1) where T : unmanaged
    {
        ListHelper(ref value, count);

        // Unmanaged types have fixed size, so we can serialize in bulk
        Serialize(value.AsBytes());
    }

    // ISerializable
    public void Serialize<T>([NotNull] ref List<T>? value, int count = -1, byte _ = 0) where T : ISerializable, new()
    {
        ListHelper(ref value, count);

        foreach (ref var item in value.AsSpan())
        {
            Serialize(ref item);
        }
    }

    // String
    public void Serialize([NotNull] ref List<string>? value, int count = -1)
    {
        ListHelper(ref value, count);

        foreach (ref var item in value.AsSpan())
        {
            Serialize(ref item);
        }
    }

    #endregion

    #endregion

    public void Dispose()
    {
        if (_disposed) return;

        _stream.Dispose();
        GC.SuppressFinalize(this);

        _disposed = true;
    }
}
