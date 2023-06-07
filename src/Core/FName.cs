using System.Buffers.Binary;

namespace UnrealLib.Core
{
    /// <summary>
    /// References an existing FNameEntry in the name table via index. Additionally uses an instance delimiter
    /// </summary>
    public class FName : IDeserializable<FName>, ISerializable
    {
        public int NameIndex { get; internal set; }
        public int NameInstance { get; internal set; }

        public FName Deserialize(UnrealStream unStream)
        {
            NameIndex = unStream.ReadInt32();
            NameInstance = unStream.ReadInt32();
            return this;
        }

        public void Serialize(UnrealStream unStream)
        {
            unStream.Write(NameIndex);
            unStream.Write(NameInstance);
        }
        
        /// <summary>
        /// Writes the FName to a byte array
        /// </summary>
        /// <returns>A byte array containing the serialized FName</returns>
        public byte[] Serialize()
        {
            byte[] outputBuffer = new byte[8];

            // Write NameIndex to the output buffer
            BinaryPrimitives.WriteInt32LittleEndian(outputBuffer, NameIndex);

            // Write NameInstance to the output buffer at index 4
            BinaryPrimitives.WriteInt32LittleEndian(outputBuffer.AsSpan(4), NameInstance);

            return outputBuffer;
        }

        public static bool operator ==(FName a, FName b) =>
            a.NameIndex == b.NameIndex && a.NameInstance == b.NameInstance;
        public static bool operator !=(FName a, FName b) => !(a == b);

        public static bool operator ==(FName a, int b) => a.NameIndex == b;
        public static bool operator !=(FName a, int b) => a.NameIndex != b;
        
        public static bool operator ==(FName a, int? b) => a.NameIndex == b;
        public static bool operator !=(FName a, int? b) => a.NameIndex != b;

        public string ToString(UPK upk)
        {
            return $"{upk.Names[NameIndex].Name}{(NameInstance > 0 ? $"_{(NameInstance - 1).ToString()}" : "")}";
        }
    }

    /// <summary>
    /// Represents a string 'name' in the name table
    /// </summary>
    public class FNameEntry : IDeserializable<FNameEntry>
    {
        public string Name { get; internal set; }
        public EObjectFlags NameFlags { get; internal set; }
        public bool ExistsAsImport { get; set; } = false;

        public static bool operator ==(FNameEntry left, string right) => left.Name == right;
        public static bool operator !=(FNameEntry left, string right) => !(left == right);

        public override string ToString() => Name;

        public FNameEntry Deserialize(UnrealStream unStream)
        {
            Name = unStream.ReadFString();
            NameFlags = (EObjectFlags)unStream.ReadInt64();
            return this;
        }
    }
}
