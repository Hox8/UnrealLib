namespace UnrealLib.Core
{
    /// <summary>
    /// Base class for UObject resource types (export, import)
    /// </summary>
    public abstract class FObjectResource
    {
        public FName ObjectName { get; internal set; }

        public int OuterIndex { get; internal set; }

        // Transient
        public int Index { get; internal set; } // index into respective table
        public int Offset { get; internal set; } // serialized offset in file
    }

    /// <summary>
    /// UObject resource type for objects that are referenced by this package
    /// </summary>
    public class FObjectImport : FObjectResource, IDeserializable<FObjectImport>
    {
        public FName ClassPackage { get; internal set; }
        public FName ClassName { get; internal set; }

        public FObjectImport Deserialize(UnrealStream unStream)
        {
            Offset = unStream.Position;
            
            ClassPackage = unStream.Read<FName>();
            ClassName = unStream.Read<FName>();
            OuterIndex = unStream.ReadInt32();
            ObjectName = unStream.Read<FName>();
            return this;
        }
    }

    /// <summary>
    /// UObject resource type for objects that are contained within this package
    /// </summary>
    public class FObjectExport : FObjectResource, IDeserializable<FObjectExport>
    {
        public int ClassIndex { get; internal set; }
        public int SuperIndex { get; internal set; }
        public int ArchetypeIndex { get; internal set; }
        public EObjectFlags ObjectFlags { get; internal set; }
        public int SerialSize { get; internal set; }
        public int SerialOffset { get; internal set; }
        public EExportFlags ExportFlags { get; internal set; }
        public List<int> GenerationNetObjectCount { get; internal set; }
        public FGuid PackageGuid { get; internal set; }
        public EPackageFlags PackageFlags { get; internal set; }
        
        // In-memory only
        public int ChildIndex { get; set; } // What 'layer' this object is at. No parents == 0, 1 parent == 1 etc

        public FObjectExport Deserialize(UnrealStream unStream)
        {
            Offset = unStream.Position;
            
            ClassIndex = unStream.ReadInt32();
            SuperIndex = unStream.ReadInt32();
            OuterIndex = unStream.ReadInt32();
            ObjectName = unStream.Read<FName>();
            ArchetypeIndex = unStream.ReadInt32();
            ObjectFlags = (EObjectFlags)unStream.ReadInt64();

            SerialSize = unStream.ReadInt32();
            SerialOffset = unStream.ReadInt32();
            ExportFlags = (EExportFlags)unStream.ReadInt32();

            GenerationNetObjectCount = unStream.ReadIntList();
            PackageGuid = unStream.Read<FGuid>();
            PackageFlags = (EPackageFlags)unStream.ReadInt32();

            return this;
        }

        public void Serialize(UPK upk)
        {
            // Assumes position is set beforehand
            upk.UnStream.Write(ClassIndex);
            upk.UnStream.Write(SuperIndex);
            upk.UnStream.Write(OuterIndex);
            upk.UnStream.Write(ObjectName.Serialize());
            upk.UnStream.Write(ArchetypeIndex);
            upk.UnStream.Write((long)ObjectFlags);
            upk.UnStream.Write(SerialSize);
            upk.UnStream.Write(SerialOffset);
            upk.UnStream.Write((int)ExportFlags);
            upk.UnStream.WriteIntList(GenerationNetObjectCount);
            PackageGuid.Serialize(upk.UnStream);
            upk.UnStream.Write((int)PackageFlags);
        }

        public string ToString(UPK upk)
        {
            string className = ClassIndex != 0
                ? ClassIndex > 0 ? upk.GetName(upk.Exports[ClassIndex - 1]) : upk.GetName(upk.Imports[~ClassIndex])
                : "NULL";

            string superName = SuperIndex != 0
                ? SuperIndex > 0 ? upk.GetName(upk.Exports[SuperIndex - 1]) : upk.GetName(upk.Imports[~SuperIndex])
                : "NULL";

            string outerName = OuterIndex != 0
                ? OuterIndex > 0 ? upk.GetName(upk.Exports[OuterIndex - 1]) : upk.GetName(upk.Imports[~OuterIndex])
                : "NULL";

            string objectName = upk.GetName(this);

            string archetype = ArchetypeIndex != 0
                ? ArchetypeIndex > 0
                    ? upk.GetName(upk.Exports[ArchetypeIndex - 1])
                    : upk.GetName(upk.Imports[~ArchetypeIndex])
                : "NULL";

            return $"Object:\t{objectName}\n\tSuper:\t\t{superName}\n\tOuter:\t\t{outerName}\n\tArchetype:\t{archetype}";
        }
    }
}