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

        public static bool operator ==(FName a, FName b)
        {
            return a.NameIndex == b.NameIndex && a.NameInstance == b.NameInstance;
        }
        public static bool operator !=(FName a, FName b) => !(a == b);
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
