using System.Runtime.InteropServices;
using System.Text;

namespace UnrealLib
{
    public class UnrealStream : IDisposable
    {
        private readonly BinaryReader _unReader;
        private readonly BinaryWriter _unWriter;
        private bool isDisposed = false;

        public UnrealStream(MemoryStream memStream)
        {
            _unReader = new BinaryReader(memStream);
            _unWriter = new BinaryWriter(memStream);
            Position = 0;
        }

        public UnrealStream(byte[] value)
        {
            var memStream = new MemoryStream();
            memStream.Write(value);
            _unReader = new BinaryReader(memStream);
            _unWriter = new BinaryWriter(memStream);
            Position = 0;
        }

        #region Read methods

        public long ReadInt64()
        {
            return _unReader.ReadInt64();
        }

        public int ReadInt32()
        {
            return _unReader.ReadInt32();
        }

        public short ReadInt16()
        {
            return _unReader.ReadInt16();
        }

        public float ReadFloat()
        {
            return _unReader.ReadSingle();
        }

        public byte ReadByte()
        {
            return _unReader.ReadByte();
        }

        public byte[] ReadBytes(int amt)
        {
            return _unReader.ReadBytes(amt);
        }

        public char ReadChar(bool UTF16 = false)
        {
            if (UTF16)
            {
                return MemoryMarshal.Cast<byte, char>(_unReader.ReadBytes(2)).ToArray()[0];
            }
            return _unReader.ReadChar();
        }

        /// <summary>
        /// Reads an Unreal string from a file. Supports both unicode and ascii strings, and trims the null character from the end
        /// </summary>
        /// <returns></returns>
        public string ReadFString()
        {
            int length = _unReader.ReadInt32();

            if (length == 0) return string.Empty;
            if (length < 0) // Unicode
            {
                length *= -2;
                byte[] unicodeBytes = _unReader.ReadBytes(length);
                return Encoding.Unicode.GetString(unicodeBytes).TrimEnd('\0');
            }
            byte[] asciiBytes = _unReader.ReadBytes(length);
            return Encoding.ASCII.GetString(asciiBytes).TrimEnd('\0');
        }

        // CStrings' only information is that they end with a null character. Encoding cannot be inferred.
        public string ReadCString(bool UTF16 = false)  // ASCII
        {
            var sb = new StringBuilder();
            while (true)
            {
                char c = ReadChar(UTF16);
                if (c == 0) break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        public T Read<T>() where T : IDeserializable<T>, new()
        {
            return new T().Deserialize(this);
        }

        public List<List<int>> ReadDependsMap(int capacity)
        {
            List<List<int>> list = new(capacity);

            for (int i = 0; i < list.Capacity; i++)
            {
                list.Add(ReadIntList());
            }
            return list;
        }

        /// <summary>
        /// Initializes, deserializes, and returns a list of an Unreal type implementing IDeserializable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> ReadObjectList<T>(int? capacity = null) where T : IDeserializable<T>, new()
        {
            List<T> list = new(capacity ?? ReadInt32());

            for (int i = 0; i < list.Capacity; i++)
            {
                T type = new();
                list.Add(type.Deserialize(this));
            }
            return list;
        }

        public List<string> ReadStringList(int? capacity = null)
        {
            List<string> list = new(capacity ?? ReadInt32());
            for (int i = 0; i < list.Capacity; i++)
            {
                list.Add(ReadFString());
            }
            return list;
        }

        public List<int> ReadIntList(int? capacity = null)
        {
            List<int> list = new(capacity ?? ReadInt32());
            for (int i = 0; i < list.Capacity; i++)
            {
                list.Add(ReadInt32());
            }
            return list;
        }

        #endregion

        #region Write methods

        public void Write(long value)
        {
            _unWriter.Write(value);
        }

        public void Write(int value)
        {
            _unWriter.Write(value);
        }

        public void Write(short value)
        {
            _unWriter.Write(value);
        }

        public void Write(float value)
        {
            _unWriter.Write(value);
        }

        public void Write(byte value)
        {
            _unWriter.Write(value);
        }

        public void Write(ReadOnlySpan<byte> value)
        {
            _unWriter.Write(value);
        }

        public void Write(string value, bool writeLength = true, bool forceUnicode = false)
        {
            if (value.Length == 0) Write(0);

            else if (forceUnicode || !value.All(char.IsAscii))   // Serialize Unicode string
            {
                if (writeLength) Write(-(value.Length + 1));
                Write(MemoryMarshal.Cast<char, byte>(value + (char)0));
            }
            else  // Serialize ASCII string
            {
                if (writeLength) Write(value.Length + 1);
                Write(Encoding.ASCII.GetBytes(value + (char)0));
            }
        }

        public void WriteObjectList<T>(List<T> list) where T: ISerializable
        {
            Write(list.Count);
            foreach (T item in list)
            {
                item.Serialize(this);
            }
        }

        public void WriteStringList(List<string> list, bool writeLength = true, bool forceUnicode = false)
        {
            Write(list.Count);
            foreach (string value in list)
            {
                Write(value, writeLength, forceUnicode);
            }
        }

        public void WriteIntList(List<int> list)
        {
            Write(list.Count);
            foreach (int value in list)
            {
                Write(value);
            }
        }

        #endregion

        #region Generic stream methods

        public int Length
        {
            get { return (int)_unReader.BaseStream.Length; }
        }

        public int Position
        {
            get { return (int)_unReader.BaseStream.Position; }
            set { _unReader.BaseStream.Position = value; }
        }

        public void SetLength(long length)
        {
            _unWriter.BaseStream.SetLength(length);
        }

        public byte[] ToArray()
        {
            _unReader.BaseStream.Position = 0;
            return _unReader.ReadBytes((int)_unReader.BaseStream.Length);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                _unReader.Dispose();
                _unWriter.Dispose();
                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
