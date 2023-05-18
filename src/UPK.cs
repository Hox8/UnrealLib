using System.Text;
using UnrealLib.Core;

namespace UnrealLib
{
    // This should really just be an extension to FNameEntry
    public class NameInfo
    {
        public List<FObjectImport> Imports { get; set; } = new();   // References to all import objects who use this name
        public List<FObjectExport> Exports { get; set; } = new();   // References to all export objects who use this name
        public int MinExportInstance { get; set; } = int.MaxValue;  // The lowest observed instance used by any exports, if any
        public int MinImportInstance { get; set; } = int.MaxValue;  // The lowest observed instance used by any imports, if any

        public override string ToString()
        {
            return $"Import count: {Imports.Count}\nExport count: {Exports.Count}\nMin export instance: {MinExportInstance}\nMin import instance: {MinImportInstance}";
        }
    }

    public class UPK
    {
        public UnrealStream UnStream { get; init; }
        public FPackageFileSummary Summary { get; private set; } = new();
        public List<FNameEntry> Names { get; private set; }
        public List<FObjectImport> Imports { get; private set; }
        public List<FObjectExport> Exports { get; private set; }
        public List<List<int>> Depends { get; private set; }

        // Transient
        public Dictionary<int, NameInfo> NameMap { get; private set; }  // A dictionary for each name index containing usage info
        public bool Modified { get; set; } = false;

        public UPK(string filePath)
        {
            UnStream = new UnrealStream(File.ReadAllBytes(filePath));   // @TODO error checking
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
                NameMap[name.NameIndex].MinImportInstance = Math.Min(NameMap[name.NameIndex].MinImportInstance, name.NameInstance);
            }

            Exports = new List<FObjectExport>(Summary.ExportCount);
            for (int i = 0; i < Summary.ExportCount; i++)
            {
                FObjectExport export = new FObjectExport().Deserialize(UnStream);
                export.Index = i;
                Exports.Add(export);

                FName name = export.ObjectName;
                NameMap[name.NameIndex].Exports.Add(export);
                NameMap[name.NameIndex].MinExportInstance = Math.Min(NameMap[name.NameIndex].MinExportInstance, name.NameInstance);
            }
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
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
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
            sb.Remove(0, 1);  // Remove leftover '.' prefix

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

            for (int i = 0; i < Names.Count; i++)
            {
                if (string.Equals(Names[i].Name, key, StringComparison.OrdinalIgnoreCase))
                {
                    if (NameMap[i].Exports.Count > 0 && NameMap[i].MinExportInstance > 1) instance--;  // For UEE consistency

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

            // @TODO expand to include Import objects too
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
    }
}