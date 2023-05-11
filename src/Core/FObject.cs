namespace UnrealLib.Core
{
    /// <summary>
    /// Base class for UObject resource types (export, import)
    /// </summary>
    public abstract class FObjectResource
    {
        public FName ObjectName { get; internal set; }
        public int OuterIndex { get; internal set; }
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

        public FObjectExport Deserialize(UnrealStream unStream)
        {
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
    }
}
