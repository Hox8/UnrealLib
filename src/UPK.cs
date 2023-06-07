using System.Text;
using UnrealLib.Core;
using UnrealLib.UnrealScript;

namespace UnrealLib
{
    // This should really just be an extension to FNameEntry
    public class NameInfo
    {
        public List<FObjectImport> Imports { get; } = new(); // References to all import objects who use this name
        public List<FObjectExport> Exports { get; set; } = new(); // References to all export objects who use this name

        public int MinExportInstance { get; set; } =
            int.MaxValue; // The lowest observed instance used by any exports, if any

        public int MinImportInstance { get; set; } =
            int.MaxValue; // The lowest observed instance used by any imports, if any

        public override string ToString()
        {
            return
                $"Import count: {Imports.Count}\nExport count: {Exports.Count}\nMin export instance: {MinExportInstance}\nMin import instance: {MinImportInstance}";
        }
    }

    public class UPK
    {
        public UnrealStream UnStream { get; init; }
        public FPackageFileSummary Summary { get; private set; } = new();
        public List<FNameEntry> Names { get; private set; }
        public List<FObjectImport> Imports { get; private set; }

        public List<FObjectExport> Exports { get; private set; }
        // public List<List<int>> Depends { get; private set; }

        #region Name constants

        // These are each indexes to corresponding name in the name table
        public int? NAME_None { get; private set; }
        public int? NAME_ByteProperty { get; private set; }
        public int? NAME_IntProperty { get; private set; }
        public int? NAME_BoolProperty { get; private set; }
        public int? NAME_FloatProperty { get; private set; }
        public int? NAME_ObjectProperty { get; private set; }
        public int? NAME_NameProperty { get; private set; }
        public int? NAME_ClassProperty { get; private set; }
        public int? NAME_ArrayProperty { get; private set; }
        public int? NAME_StructProperty { get; private set; }
        public int? NAME_StrProperty { get; private set; }
        public int Test { get; private set; }

        #endregion

        // Transient
        public Dictionary<int, NameInfo>
            NameMap { get; private set; } // A dictionary for each name index containing usage info

        public bool Modified { get; set; } = false;

        public UPK(string filePath)
        {
            UnStream = new UnrealStream(File.ReadAllBytes(filePath)); // @TODO error checking
            Initialize();
        }

        public UPK(MemoryStream memStream)
        {
            UnStream = new UnrealStream(memStream);
            Initialize();
        }

        /// <summary>
        /// Reads in the package summary and the name/import/export tables
        /// </summary>
        private void Initialize()
        {
            UnStream.Position = 0;
            Summary.Deserialize(UnStream);

            Names = new List<FNameEntry>(Summary.NameCount);
            NameMap = new Dictionary<int, NameInfo>(Summary.NameCount);
            for (int i = 0; i < Summary.NameCount; i++)
            {
                FNameEntry nameEntry = new FNameEntry().Deserialize(UnStream);
                Names.Add(nameEntry);

                NameMap[i] = new NameInfo();
            }

            Imports = new List<FObjectImport>(Summary.ImportCount);
            for (int i = 0; i < Summary.ImportCount; i++)
            {
                FObjectImport import = new FObjectImport().Deserialize(UnStream);
                import.Index = i;
                Imports.Add(import);

                FName name = import.ObjectName;
                NameMap[name.NameIndex].Imports.Add(import);
                NameMap[name.NameIndex].MinImportInstance =
                    Math.Min(NameMap[name.NameIndex].MinImportInstance, name.NameInstance);
            }

            Exports = new List<FObjectExport>(Summary.ExportCount);
            for (int i = 0; i < Summary.ExportCount; i++)
            {
                FObjectExport export = new FObjectExport().Deserialize(UnStream);
                export.Index = i;
                Exports.Add(export);

                FName name = export.ObjectName;
                NameMap[name.NameIndex].Exports.Add(export);
                NameMap[name.NameIndex].MinExportInstance =
                    Math.Min(NameMap[name.NameIndex].MinExportInstance, name.NameInstance);
            }

            PostConstruction();
        }

        /// Steps to take after the first pass deserialization of the package file
        private void PostConstruction()
        {
            // Re-iterate exports to populate child index
            foreach (var e in Exports)
            {
                e.ChildIndex = 0;
                int outer = e.OuterIndex;

                while (outer != 0)
                {
                    outer = outer > 0 ? Exports[outer - 1].OuterIndex : 0;
                    e.ChildIndex++;
                }
            }

            // Sort NameMap from least children to most
            // IMPORTANT: Searching for exports via name depends on these entries being sorted in order to work correctly
            foreach (var entry in NameMap)
            {
                if (entry.Value.Exports.Count > 0)
                {
                    entry.Value.Exports = entry.Value.Exports.OrderBy(x => x.ChildIndex).ToList();
                }
            }

            InitNames();
        }

        // Link common names to "constant" values
        private void InitNames()
        {
            NAME_None = FindName("None")?.NameIndex;
            NAME_ByteProperty = FindName("ByteProperty")?.NameIndex;
            NAME_IntProperty = FindName("IntProperty")?.NameIndex;
            NAME_BoolProperty = FindName("BoolProperty")?.NameIndex;
            NAME_FloatProperty = FindName("FloatProperty")?.NameIndex;
            NAME_ObjectProperty = FindName("ObjectProperty")?.NameIndex;
            NAME_NameProperty = FindName("NameProperty")?.NameIndex;
            NAME_ClassProperty = FindName("ClassProperty")?.NameIndex;
            NAME_ArrayProperty = FindName("ArrayProperty")?.NameIndex;
            NAME_StructProperty = FindName("StructProperty")?.NameIndex;
            NAME_StrProperty = FindName("StrProperty")?.NameIndex;
        }

        /// <summary>
        /// Returns the string representation of an FName
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetName(FName name)
        {
            return $"{Names[name.NameIndex]}{(name.NameInstance > 0 ? $"_{name.NameInstance - 1}" : "")}";
        }

        /// <summary>
        /// Returns the string representation of an object's full path
        /// </summary>
        /// <param name="obj">The import OR export object to search the name of</param>
        /// <param name="returnFullName">Boolean dictating whether to return just the full object path or just its leaf name</param>
        /// <returns></returns>
        public string GetName(FObjectResource obj, bool returnFullName = true)
        {
            if (!returnFullName) return GetName(obj.ObjectName);
            var sb = new StringBuilder();

            FObjectResource parent = obj;
            while (true)
            {
                sb.Insert(0, $".{GetName(parent.ObjectName)}");

                if (parent.OuterIndex == 0) break;
                parent = parent.OuterIndex > 0 ? Exports[parent.OuterIndex - 1] : Imports[~parent.OuterIndex];
            }

            sb.Remove(0, 1); // Remove leftover '.' prefix

            return sb.ToString();
        }

        /// <summary>
        /// Looks for a name in the names table and returns a new FName pointing to it.
        /// Instances are automatically handled, and string comparisons are case-insensitive.
        /// </summary>
        /// <param name="value">The string value to find within the name table</param>
        /// <returns>A new FName object upon success, otherwise null</returns>
        public FName? FindName(string value)
        {
            string key;
            int instance;

            // Process name instance
            int idx = value.LastIndexOf('_');
            if (idx != 0)
            {
                if (!int.TryParse(value[(idx + 1)..], out instance))
                {
                    key = value;
                }
                else key = value[..idx];
            }
            else
            {
                key = value;
                instance = 0;
            }

            // Locate string in name table
            for (int i = 0; i < Names.Count; i++)
            {
                if (string.Equals(Names[i].Name, key, StringComparison.OrdinalIgnoreCase))
                {
                    if (NameMap[i].Exports.Count > 0 && NameMap[i].MinExportInstance > 1)
                        instance--; // For UEE consistency

                    instance += NameMap[i].MinExportInstance;
                    return new FName() { NameIndex = i, NameInstance = instance };
                }
            }

            return null;
        }

        /// <summary>
        /// Wrapper for <see cref="FindName(string)"/>. Splits object paths into its components (e.g. "a.b.c" => "a", "b", "c")
        /// and processes each name separately
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An array of FNames upon success of all components, otherwise null</returns>
        public FName[]? FindName(string[] value)
        {
            FName[] result = new FName[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                result[i] = FindName(value[i]);
                if (result[i] is null) return null;
            }

            return result;
        }

        /// <summary>
        /// Finds an export matching the passed value.
        /// 'Value' is converted into FName(s) as needed. Case-insensitive
        /// </summary>
        /// <param name="value"></param>
        /// <returns>The requested FObjectExport object on success, otherwise null</returns>
        public FObjectExport? FindExport(string value)
        {
            FName[]? names = FindName(value.Split('.'));
            if (names is null) return null;

            // Find the furthest child export object
            foreach (FObjectExport export in NameMap[names[^1].NameIndex].Exports)
            {
                if (export.ObjectName == names[^1])
                {
                    // Compare full path. Leaf name is unfortunately checked again
                    FObjectResource parent = export;
                    int i = names.Length - 1;
                    while (i >= 0 && parent.ObjectName == names[i])
                    {
                        if (i == 0 || parent.OuterIndex == 0)
                        {
                            if (i == 0) return export;
                            break;
                        }

                        parent = parent.OuterIndex > 0 ? Exports[parent.OuterIndex - 1] : Imports[~parent.OuterIndex];
                        i--;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the object whose serialized bytes include the passed offset
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public FObjectExport? GetObjectAtOffset(long offset)
        {
            foreach (var export in Exports)
            {
                if (export.SerialOffset + export.SerialSize >= offset) return export;
            }

            return null;
        }

        /// <summary>
        /// For DEBUG testing only; not meant for public api.
        /// Copies function data to end of archive and changes its length by amt.
        /// </summary>
        /// <param name="f">UFunction reference</param>
        /// <param name="e">FObjectExport reference</param>
        /// <param name="amt">Int number of bytes to adjust by</param>
        public void MoveExtendFunc(UFunction f, FObjectExport e, int amt)
        {
            if (amt < 0) return;

            // Determine whether function returns a return value (crappy method)

            f.Script.AddRange(new List<byte>(Enumerable.Repeat((byte)0x0B, amt)));
            f.ScriptBytecodeSize += amt;
            f.ScriptStorageSize += amt;
            e.SerialSize += amt;
            e.SerialOffset = UnStream.Length;
            if ((f.FunctionFlags & UFunction.EFunctionFlags.FUNC_Native) != 0)
            {
                // f.FunctionFlags &= ~UFunction.EFunctionFlags.FUNC_Native;
                // f.FunctionFlags |= UFunction.EFunctionFlags.FUNC_Simulated;
                // f.FunctionFlags &= ~UFunction.EFunctionFlags.FUNC_Static;
            }

            f.FunctionFlags = UFunction.EFunctionFlags.FUNC_Public;
            f.FunctionFlags |= UFunction.EFunctionFlags.FUNC_Simulated;

            UnStream.Position = e.Offset;
            e.Serialize(this);

            UnStream.Position = UnStream.Length;
            f.Serialize(this);
        }

        /// <summary>
        /// This method's name needs to be changed!
        /// Reads an export's contents into its corresponding UObject type
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        public UObject? DeserializeObject(FObjectExport export)
        {
            if (export is null) return null;
            
            // If export has a class index of 0, it is a UClass object
            string name = export.ClassIndex == 0 ? "Core.Class" :
                export.ClassIndex > 0 ? GetName(Exports[export.ClassIndex - 1]) : GetName(Imports[~export.ClassIndex]);

            switch (name)
            {
                case "Core.ScriptStruct":
                    return new UScriptStruct(this, export);
                
                case "Core.Function":
                    return new UFunction(this, export);

                case "Core.State":
                    return new UState(this, export);

                case "Core.Class":
                    return new UClass(this, export);

                default:
                    Console.WriteLine($"'{name}' is not a supported UObject type!");
                    return null;
            }
        }
    }
}