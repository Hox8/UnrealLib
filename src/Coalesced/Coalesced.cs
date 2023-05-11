using static System.Collections.Specialized.BitVector32;

namespace UnrealLib.Coalesced
{    
    public class Property : IDeserializable<Property>, ISerializable
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public Property Deserialize(UnrealStream unStream)
        {
            Key = unStream.ReadFString();
            Value = unStream.ReadFString();
            return this;
        }

        public void Serialize(UnrealStream unStream)
        {
            unStream.Write(Key, writeLength: true, forceUnicode: true);
            unStream.Write(Value, writeLength: true, forceUnicode: true);
        }
    }

    public class Section : IDeserializable<Section>, ISerializable
    {
        public string Name { get; set; }
        public List<Property> Properties { get; set; }

        public Section Deserialize(UnrealStream unStream)
        {
            Name = unStream.ReadFString();
            Properties = unStream.ReadObjectList<Property>();
            return this;
        }

        public void Serialize(UnrealStream unStream)
        {
            unStream.Write(Name, writeLength: true, forceUnicode: true);
            unStream.WriteObjectList(Properties);
        }
    }

    public class Ini : IDeserializable<Ini>, ISerializable
    {
        public string Path { get; set; }
        public List<Section> Sections { get; set; }

        public Ini Deserialize(UnrealStream unStream)
        {
            Path = unStream.ReadFString();
            Sections = unStream.ReadObjectList<Section>();
            return this;
        }

        public void Serialize(UnrealStream unStream)
        {
            unStream.Write(Path, writeLength: true, forceUnicode: true);
            unStream.WriteObjectList(Sections);
        }

        #region Helper methods

        public Section GetSection(string targetSection)
        {
            foreach (Section section in Sections)
            {
                if (section.Name == targetSection) return section;
            }

            // if section was not found, add new section to end and return that instead
            Sections.Add(new Section() { Name = targetSection, Properties = new() });
            return Sections[^1];
        }

        public void TryRemoveSection(string targetSection)
        {
            foreach (Section section in Sections)
            {
                if (section.Name == targetSection)
                {
                    Sections.Remove(section);
                    return;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// A class which manages the data associated with a coalesced file
    /// </summary>
    public class Coalesced
    {
        public UnrealStream UnStream { get; private set; }
        public bool FailedDecryption { get; private set; } = false;

        public Game Game;           // Used to determine what encryption to perform
        public List<Ini> Inis;

        public Coalesced(string filePath, Game game)
        {
            UnStream = new UnrealStream(File.ReadAllBytes(filePath));
            Game = game;

            Initialize();
        }

        public Coalesced(MemoryStream memStream, Game game)
        {
            UnStream = new UnrealStream(memStream);
            Game = game;

            Initialize();
        }

        private void Initialize()
        {
            if (IsEncrypted()) AES.CryptoECB(UnStream, Game, modeIsDecrypt: true);
            if (IsEncrypted()) FailedDecryption = true;
            else Deserialize();
        }

        public bool IsEncrypted()
        {
            UnStream.Position = 2;
            return UnStream.ReadByte() != 0 || UnStream.ReadByte() != 0;
        }

        public Coalesced Deserialize()
        {
            UnStream.Position = 0;
            Inis = UnStream.ReadObjectList<Ini>();
            return this;
        }

        public void Serialize()
        {
            UnStream.Position = 0;
            UnStream.WriteObjectList(Inis);
            UnStream.SetLength(UnStream.Position);  // If new coalesced is shorter than original, we'll need to trim the remainder

            // If game isn't IB1, encrypt the coalesced. This is because IB1 is the only game which doesn't encryption
            if (Game != Game.IB1)
            {
                AES.CryptoECB(UnStream, Game, modeIsDecrypt: false);
            }
        }

        #region Helper methods

        public Ini GetIni(string targetIni)
        {
            foreach (Ini ini in Inis)
            {
                if (ini.Path == targetIni) return ini;
            }

            // if ini was not found, add new ini to end and return that instead
            Inis.Add(new Ini() { Path = targetIni, Sections = new() });
            return Inis[^1];
        }

        public void TryRemoveIni(string targetIni)
        {
            foreach (Ini ini in Inis)
            {
                if (ini.Path == targetIni)
                {
                    Inis.Remove(ini);
                    return;
                }
            }
        }

        #endregion
    }
}
