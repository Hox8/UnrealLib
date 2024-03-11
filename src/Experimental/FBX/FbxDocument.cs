using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace UnrealLib.Experimental.FBX
{
    public class FbxDocument
    {
        public static readonly byte[] Header = "Kaydara FBX Binary  \0"u8.ToArray();

        // Taking a page out of Blender's book as the FBX CRC formula is unknown. Therefore, use this one which works for Unix epoch.
        private const string TimeID = "1970-01-01 10:00:00:000";
        private static readonly byte[] FileID = [0x28, 0xB3, 0x2A, 0xEB, 0xB6, 0x24, 0xCC, 0xC2, 0xBF, 0xC8, 0xB0, 0x2A, 0xA9, 0x2B, 0xFC, 0xF1];
        private static readonly byte[] FileCRC = [0xFA, 0xBC, 0xAB, 0x09, 0xD0, 0xC8, 0xD4, 0x66, 0xB1, 0x76, 0xFB, 0x83, 0x1C, 0xF7, 0x26, 0x7E];
        private static readonly byte[] FooterID = [0xF8, 0x5A, 0x8C, 0x6A, 0xDE, 0xF5, 0xD9, 0x7E, 0xEC, 0xE9, 0x0C, 0xE3, 0x75, 0x8F, 0x29, 0x0B];

        public short UnkVersion;
        public int FbxVersion;
        public List<FbxNode> Nodes;

        public void Serialize(UnrealArchive Ar)
        {
            var header = Header;
            Ar.Serialize(ref header, 21);

            // @TODO implement this
            //if (header != Header)
            //{
            //    throw new Exception();
            //}

            Ar.Serialize(ref UnkVersion);
            Ar.Serialize(ref FbxVersion);

            // Probably can support a wider range (7500 DOES require explicit support) but needs testing
            Debug.Assert(FbxVersion <= 7400 && FbxVersion >= 7300, "FBX version is unsupported");

            if (Ar.IsLoading)
            {
                Nodes = [];
                
                while (true)
                {
                    FbxNode node = new();
                    node.Serialize(Ar);

                    Nodes.Add(node);

                    // Null nodes will have an EndOffset of 0
                    if (node.EndOffset == 0)
                        break;
                }
            }
            else
            {
                // Set time and hash to Unix epoch. This is a hard requirement until CRC is figured out
                this["FileId"].GetProperties()[0] = new(FileID);
                this["CreationTime"].GetProperties()[0] = new(TimeID);

                foreach (var node in Nodes)
                {
                    node.Serialize(Ar);
                }
            }

            // FBX files have footers. Blender does not care whether they exist, but UE3 WILL crash!
            if (!Ar.IsLoading)
            {
                Ar.Write(FileCRC);
                Ar.Position += 18;
                Ar.Serialize(ref FbxVersion);
                Ar.Position += 120;
                Ar.Write(FooterID);
            }
        }

        #region Public API

        public FbxNode? this[string name] => Nodes.Find(node => node.Name == name);

        // later turn into static ctors, CreateNew() and FromFile()
        // Actually, move into a validate method? This way it will work for existing files too
        // If required node doesn't exist, add it. Update existing nodes etc
        public void CreateNew()
        {
            FbxVersion = 7300;  // UE3 yells at us if this we're not using 7.3, so use as default
            UnkVersion = 26;

            var date = System.DateTime.Now;

            // Remember all children if not null must end with a null termintator!

            Nodes =
                [
                    // FBXHeaderExtension
                    // MANDATORY (UE3)
                    new()
                    {
                        Name = "FBXHeaderExtension",
                        Children = [
                            new() { Name = "FBXHeaderVersion", Properties = [new(1003)]},
                            new() { Name = "FBXVersion", Properties = [new(FbxVersion)]},
                            new() { Name = "EncryptonType", Properties = [new(0)] },
                            new() { Name = "CreationTimeStamp", Children = [
                                new() { Name = "Version", Properties = [ new(1000) ]},
                                new() { Name = "Year", Properties = [ new(date.Year) ]},
                                new() { Name = "Month", Properties = [ new(date.Month) ]},
                                new() { Name = "Day", Properties = [ new(date.Day) ]},
                                new() { Name = "Hour", Properties = [ new(date.Hour) ]},
                                new() { Name = "Minute", Properties = [ new(date.Minute) ]},
                                new() { Name = "Millisecond", Properties = [ new(date.Millisecond) ]},
                                new()
                                ]
                            },
                            new()
                        ]
                    },

                    // FileID
                    // MANDATORY (UE3)
                    new()
                    {
                        Name = "FileId", Properties = [new(FileID)]
                    },

                    // CreationTime
                    // MANDATORY (UE3)
                    new()
                    {
                        Name = "CreationTime", Properties = [new(TimeID)]
                    },

                    // Creator
                    // OPTIONAL
                    new()
                    {
                        Name = "Creator", Properties = [new("Hox's bootleg FBX exporter")]
                    },

                    // GlobalSettings
                    // MANDATORY (Blender)
                    new()
                    {
                        Name = "GlobalSettings",
                        Children = [
                            new() { Name = "Version", Properties = [ new(1000) ]},
                            new() { Name = "Properties70", Children = [
                                new() { Name = "P", Properties = [ new("UpAxis"), new("int"), new("Integer"), new(""), new(2) ]},
                                new() { Name = "P", Properties = [ new("UpAxisSign"), new("int"), new("Integer"), new(""), new(1) ]},

                                new() { Name = "P", Properties = [ new("FrontAxis"), new("int"), new("Integer"), new(""), new(1) ]},
                                new() { Name = "P", Properties = [ new("FrontAxisSign"), new("int"), new("Integer"), new(""), new(-1) ]},

                                new() { Name = "P", Properties = [ new("CoordAxis"), new("int"), new("Integer"), new(""), new(0) ]},
                                new() { Name = "P", Properties = [ new("CoordAxisSign"), new("int"), new("Integer"), new(""), new(1) ]},

                                new() { Name = "P", Properties = [ new("OriginalUpAxis"), new("int"), new("Integer"), new(""), new(2) ]},
                                new() { Name = "P", Properties = [ new("OriginalUpAxisSign"), new("int"), new("Integer"), new(""), new(1) ]},

                                new() { Name = "P", Properties = [ new("UnitScaleFactor"), new("double"), new("Number"), new(""), new(1) ]},
                                new() { Name = "P", Properties = [ new("OriginalUnitScaleFactor"), new("double"), new("Number"), new(""), new(1) ]},

                                new() { Name = "P", Properties = [ new("AmbientColor"), new("ColorRGB"), new("Color"), new(""), new(0), new(0), new(0) ]},

                                new() { Name = "P", Properties = [ new("DefaultCamera"), new("KString"), new(""), new(""), new("Producer Perspective") ]},

                                new() { Name = "P", Properties = [ new("TimeMode"), new("enum"), new(""), new(""), new(0) ]},
                                new() { Name = "P", Properties = [ new("TimeSpanStart"), new("KTime"), new("Time"), new(""), new(0) ]},
                                new() { Name = "P", Properties = [ new("TimeSpanStop"), new("KTime"), new("Time"), new(""), new(46186158000) ]},

                                new() { Name = "P", Properties = [ new("CustomFrameRate"), new("double"), new("Number"), new(""), new(-1) ]},

                                new()
                                ] }
                            ]
                    },

                    // Documents
                    // OPTIONAL

                    // References
                    // OPTIONAL

                    // Definitions
                    // MANDATORY (UE3)
                    new()
                    {
                        Name = "Definitions", Children = [
                            new() { Name = "Version", Properties = [new(100)] },
                            new() { Name = "Count", Properties = [new(6)]},

                            ]
                    },

                    // Objects
                    // MANDATORY
                    //new()
                    //{
                    //    Name = "Objects",
                    //    Children = [
                    //            new() { Name = "Geometry", }
                    //        ]
                    //},

                    // Connections
                    // MANDATORY

                    // Takes
                    // OPTIONAL

                    // Null terminator
                    new()
                ];

        }

        #endregion
    }
}
