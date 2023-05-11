using System.Text;
using UnrealLib.Core;

namespace UnrealLib
{
    /// <summary>
    /// Manages the data associated with an Unreal Package
    /// </summary>
    public class UPK
    {
        public UnrealStream UnStream { get; }

        public FPackageFileSummary Summary { get; internal set; }
        public List<FNameEntry> Names { get; internal set; }            // Table of all strings used throughout the package, excluding Summary FolderName
        public List<FObjectImport> Imports { get; internal set; }       // Table of all objects referenced by this package
        public List<FObjectExport> Exports { get; internal set; }       // Table of all objects contained within this package
        public List<List<int>> Depends { get; internal set; }           // 2d int array. Each element corresponds to an index in the export table, each with an array of indexes to the import table

        public bool Modified { get; set; } = false; // IBPatcher. Shouldn't this be declared in ITS project?
        public bool FailedDeserialization { get; private set; } = false;

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

        private void Initialize()
        {
            // Deserialize Unreal package summary
            UnStream.Position = 0;
            Summary = new FPackageFileSummary().Deserialize(UnStream);

            if (Summary.Tag != FPackageFileSummary.TAG)
            {
                FailedDeserialization = true;
                return;
            }

            // Read rest of header
            Names = UnStream.ReadObjectList<FNameEntry>(Summary.NameCount);
            Imports = UnStream.ReadObjectList<FObjectImport>(Summary.ImportCount);
            Exports = UnStream.ReadObjectList<FObjectExport>(Summary.ExportCount);
            // Depends = UnStream.ReadDependsMap(Summary.ExportCount);  // always empty for Infinity Blade games, so faster to simply skip
            UnStream.Position += Summary.ExportCount * 4;
        }

        #region Helper methods

        #region GetName

        public string GetName(FName name)
        {
            return $"{Names[name.NameIndex].Name}{(name.NameInstance > 0 ? $"_{name.NameInstance - 1}" : "")}";
        }

        /// <summary>
        /// Returns the name of the object at the specified index. Index less than 0 represents an import object, index more than 0 represents an export object.
        /// </summary>
        /// <param name="objIdx"></param>
        /// <returns></returns>
        public string GetName(int objIdx)
        {
            if (objIdx == 0) return string.Empty;
            if (objIdx < 0)
            {
                return GetName(Imports[~objIdx].ObjectName);
            }
            return GetName(Exports[objIdx - 1].ObjectName);
        }

        /// <summary>
        /// Returns the full name of the passed export object
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public string GetName(FObjectExport e)
        {
            var sb = new StringBuilder();
            int parentIndex = e.OuterIndex;

            while (parentIndex != 0)
            {
                sb.Insert(0, $"{GetName(Exports[--parentIndex].ObjectName)}.");
                parentIndex = Exports[parentIndex].OuterIndex;
            }

            return sb.Append(GetName(e.ObjectName)).ToString();
        }

        #endregion

        public int GetNameIndex(string value)
        {
            for (int i = 0; i < Names.Count; i++)
            {
                if (Names[i] == value) return i;
            }
            return -1;
        }

        /// <summary>
        /// Finds an object whose name matches the given string and returns its index
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetObjectIndex(string value) // @TODO make case-insensitive
        {
            // First try separate the leaf name from the full name
            string leafName = value[(value.LastIndexOf('.')+1)..];
            string parentNames = value != leafName ? value[..value.LastIndexOf('.')] : string.Empty;

            // Then verify leafName is within this package's name table
            int nameIndex = GetNameIndex(leafName);
            if (nameIndex == -1) return 0;

            // Search exports for leaf name + full name if applicable
            for (int i = 0; i < Exports.Count; i++)
            {
                if (Exports[i].ObjectName.NameIndex == nameIndex)
                {
                    if (!string.IsNullOrEmpty(parentNames) && GetName(Exports[i]) != value) continue;
                    return i + 1;
                }
            }

            // Search imports for leaf name
            for (int i = 0; i < Imports.Count; i++)
            {
                if (Imports[i].ObjectName.NameIndex == nameIndex) return ~i;
            }

            return 0;
        }

        #endregion
    }
}
