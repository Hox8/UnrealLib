using System.Text;
using UnLib.Enums;
using UnLib.Enums.Textures;
using UnLib.Interfaces;

namespace UnLib;

public sealed class UnrealStream : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly UnrealPackage? _unPackage;
    private readonly BinaryWriter _writer;
    public bool ForceUnicode = false; // Forces strings to be serialized in unicode, even if they can fit into ascii
    public bool IsLoading = false;

    public bool IsSaving
    {
        get => !IsLoading;
        set => IsLoading = !value;
    }

    // @WARN: UnrealStream cannot detect external changes to memStream
    public UnrealStream(MemoryStream memStream, UnrealPackage? unPackage = null)
    {
        _unPackage = unPackage;
        _reader = new BinaryReader(memStream);
        _writer = new BinaryWriter(memStream);
    }

    public UnrealStream(string filePath, UnrealPackage? unPackage = null)
    {
        var fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

        _unPackage = unPackage;
        _reader = new BinaryReader(fs);
        _writer = new BinaryWriter(fs);
    }

    public Stream BaseStream => _reader.BaseStream;

    public void CopyTo(Stream destination)
    {
        _reader.BaseStream.CopyTo(destination);
    }

    public void Close()
    {
        _reader.BaseStream.Position = 0;
        _reader.Close();
        Dispose();
    }

    #region Serializers

    // FLOAT
    public void Serialize(ref float value)
    {
        if (IsLoading)
            value = _reader.ReadSingle();
        else
            _writer.Write(value);
    }

    // UINT64
    public void Serialize(ref long value)
    {
        if (IsLoading)
            value = _reader.ReadInt64();
        else
            _writer.Write(value);
    }

    // UINT32
    public void Serialize(ref uint value)
    {
        if (IsLoading)
            value = _reader.ReadUInt32();
        else
            _writer.Write(value);
    }

    // INT32
    public void Serialize(ref int value)
    {
        if (IsLoading)
            value = _reader.ReadInt32();
        else
            _writer.Write(value);
    }

    // UINT16
    public void Serialize(ref short value)
    {
        if (IsLoading)
            value = _reader.ReadInt16();
        else
            _writer.Write(value);
    }

    // BYTE
    public void Serialize(ref byte value)
    {
        if (IsLoading)
            value = _reader.ReadByte();
        else
            _writer.Write(value);
    }

    // STRING
    public void Serialize(ref string value)
    {
        if (IsLoading)
        {
            var length = _reader.ReadInt32();

            if (length == 0)
            {
                value = string.Empty;
            }
            else if (length > 0)
            {
                value = Encoding.ASCII.GetString(_reader.ReadBytes(length - 1));
                _reader.BaseStream.Position += 1;
            }
            else
            {
                length *= -2;
                value = Encoding.Unicode.GetString(_reader.ReadBytes(length - 2));
                _reader.BaseStream.Position += 2;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(value))
            {
                // Write int32 '0' to indicate an empty string.
                _writer.Write(0);
            }
            else if (!ForceUnicode && IsPureAscii(value))
            {
                // Encode ASCII string + null terminator.
                _writer.Write(value.Length + 1);
                _writer.Write(Encoding.ASCII.GetBytes(value));
                _writer.Write((byte)0);
            }
            else
            {
                // Encode Unicode string + null terminator (double ASCII).
                _writer.Write((value.Length + 1) * -1);
                _writer.Write(Encoding.Unicode.GetBytes(value));
                _writer.Write((short)0);
            }
        }
    }

    public void Serialize<T>(ref T value) where T : ISerializable, new()
    {
        if (IsLoading) value = new T();

        value.Serialize(this);
    }

    #endregion

    #region List Serializers

    public void Serialize(ref List<int> value, int? length = null)
    {
        if (IsLoading)
        {
            length ??= _reader.ReadInt32();
            value = new List<int>((int)length);

            for (var i = 0; i < length; i++) value.Add(_reader.ReadInt32());
        }
        else
        {
            if (length is null) _writer.Write(value.Count);

            for (var i = 0; i < value.Capacity; i++) _writer.Write(value[i]);
        }
    }

    public void Serialize(ref List<string> value, int? length = null)
    {
        if (IsLoading)
        {
            length ??= _reader.ReadInt32();
            value = new List<string>((int)length);

            for (var i = 0; i < length; i++)
            {
                var item = string.Empty;
                Serialize(ref item);

                value.Add(item);
            }
        }
        else
        {
            if (length is null) _writer.Write(value.Count);

            for (var i = 0; i < value.Capacity; i++)
            {
                var item = value[i];
                Serialize(ref item);
            }
        }
    }

    public void Serialize<T>(ref List<T> value, int? length = null) where T : ISerializable, new()
    {
        if (IsLoading)
        {
            value = new List<T>(length ?? _reader.ReadInt32());
            for (var i = 0; i < value.Capacity; i++)
            {
                T item = new();
                item.Serialize(this);

                value.Add(item);
            }
        }
        else
        {
            if (length is null) _writer.Write(value.Count);

            for (var i = 0; i < value.Count; i++) value[i].Serialize(this);
        }
    }

    public void Serialize<T>(ref Dictionary<string, T> value, int? length = null) where T : ISerializable, new()
    {
        if (IsLoading)
        {
            var capacity = length ?? _reader.ReadInt32();
            value = new Dictionary<string, T>(capacity);

            for (var i = 0; i < capacity; i++)
            {
                var key = string.Empty;
                T item = new();

                Serialize(ref key);
                Serialize(ref item);

                value[key] = item;
            }
        }
        else
        {
            if (length is null) _writer.Write(value.Count);

            foreach (var section in value)
            {
                var key = section.Key;
                var item = section.Value;

                Serialize(ref key);
                Serialize(ref item);
            }
        }
    }

    public void Serialize(ref List<byte> value, int? length = null)
    {
        if (IsLoading)
        {
            length ??= _reader.ReadInt32();
            value = _reader.ReadBytes((int)length).ToList();
        }
        else
        {
            _writer.Write(value.Count);
            _writer.Write(value.ToArray());
        }
    }

    #endregion

    #region Enum Serializers

    public void Serialize(ref PackageFlags value)
    {
        if (IsLoading)
            value = (PackageFlags)_reader.ReadUInt32();
        else
            _writer.Write((uint)value);
    }

    public void Serialize(ref ObjectFlags value)
    {
        if (IsLoading)
            value = (ObjectFlags)_reader.ReadUInt64();
        else
            _writer.Write((ulong)value);
    }

    public void Serialize(ref ExportFlags value)
    {
        if (IsLoading)
            value = (ExportFlags)_reader.ReadUInt32();
        else
            _writer.Write((uint)value);
    }

    public void Serialize(ref CompressionFlags value)
    {
        if (IsLoading)
            value = (CompressionFlags)_reader.ReadUInt32();
        else
            _writer.Write((uint)value);
    }

    public void Serialize(ref PixelFormat value)
    {
        if (IsLoading)
            value = (PixelFormat)_reader.ReadUInt32();
        else
            _writer.Write((uint)value);
    }

    public void Serialize(ref TextureCreateFlags value)
    {
        if (IsLoading)
            value = (TextureCreateFlags)_reader.ReadUInt32();
        else
            _writer.Write((uint)value);
    }

    #endregion

    #region Helpers

    public int Position
    {
        get => (int)_reader.BaseStream.Position;
        set => _writer.BaseStream.Position = value;
    }

    public int Length => (int)_reader.BaseStream.Length;

    public void SetLength(int length)
    {
        _writer.BaseStream.SetLength(length);
    }

    public byte[] ToArray()
    {
        if (BaseStream is MemoryStream ms) return ms.ToArray();
        _reader.BaseStream.Position = 0;
        return _reader.ReadBytes((int)_reader.BaseStream.Length);
    }

    public void Write(byte[] value)
    {
        _writer.Write(value);
    }

    public void Write(byte[] value, int index, int count)
    {
        _writer.Write(value, index, count);
    }

    public static bool IsPureAscii(string value)
    {
        for (var i = 0; i < value.Length; i++)
            if (value[i] > 127)
                return false;

        return true;
    }

    #endregion

    #region Resources

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _reader.Dispose();
            _writer.Dispose();
        }

        // Release unmanaged resources
    }

    ~UnrealStream()
    {
        Dispose(false);
    }

    #endregion
}