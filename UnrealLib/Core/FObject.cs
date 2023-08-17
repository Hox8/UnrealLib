using System.Text;
using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public abstract class FObjectResource
{
    /// <summary>
    /// The name of the UObject represented by this resource.
    /// </summary>
    // FName ObjectName;
    
    /// <summary>
    /// Location of the resource for this resource's Outer.
    /// </summary>
    // int OuterIndex;
    
    internal FName ObjectName;
    internal int OuterIndex;

    // Transient
    public int SerializedOffset { get; internal set; }
    public int SerializedTableIndex { get; internal set; }

    internal FObjectResource? _outer { get; set; }

    public void Link(UnrealPackage Ar)
    {
        _outer = OuterIndex switch
        {
            > 0 => Ar.Exports[OuterIndex - 1],
            < 0 => Ar.Imports[~OuterIndex],
            _ => null
        };
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder(ObjectName.ToString());

        FObjectResource? obj = this;
        
        while (obj._outer is not null)
        {
            sb.Insert(0, $"{obj._outer.ObjectName}.");
            obj = obj._outer;
        }

        return sb.ToString();
    }
}

public class FObjectImport : FObjectResource, ISerializable
{
    internal FName ClassPackage;
    internal FName ClassName;

    public void Serialize(UnrealStream UStream)
    {
        SerializedOffset = UStream.Position;
        
        UStream.Serialize(ref ClassPackage);
        UStream.Serialize(ref ClassName);
        UStream.Serialize(ref OuterIndex);
        UStream.Serialize(ref ObjectName);
    }

    public new void Link(UnrealPackage Ar)
    {
        base.Link(Ar);
    }
}

public class FObjectExport : FObjectResource, ISerializable
{
    private int ClassIndex;
    private int SuperIndex;
    private int ArchetypeIndex;
    private ObjectFlags ObjectFlags;
    public int SerialSize;
    public int SerialOffset;
    private ExportFlags ExportFlags;
    private List<int> GenerationNetObjectCount;
    private FGuid PackageGuid;
    private PackageFlags PackageFlags;
    
    // In-memory
    internal FObjectResource? _class { get; set; }
    internal FObjectResource? _super { get; set; }
    internal FObjectResource? _archetype { get; set; }

    public void Serialize(UnrealStream UStream)
    {
        SerializedOffset = UStream.Position;
        
        UStream.Serialize(ref ClassIndex);
        UStream.Serialize(ref SuperIndex);
        
        UStream.Serialize(ref OuterIndex);
        UStream.Serialize(ref ObjectName);
        
        UStream.Serialize(ref ArchetypeIndex);
        UStream.Serialize(ref ObjectFlags);
        
        UStream.Serialize(ref SerialSize);
        UStream.Serialize(ref SerialOffset);
        
        UStream.Serialize(ref ExportFlags);
        
        UStream.Serialize(ref GenerationNetObjectCount);
        UStream.Serialize(ref PackageGuid);
        UStream.Serialize(ref PackageFlags);
    }

    public new void Link(UnrealPackage Ar)
    {
        base.Link(Ar);
        
        _class = ClassIndex switch
        {
            > 0 => Ar.Exports[ClassIndex - 1],
            < 0 => Ar.Imports[~ClassIndex],
            _ => null
        };

        _super = SuperIndex switch
        {
            > 0 => Ar.Exports[SuperIndex - 1],
            < 0 => Ar.Imports[~SuperIndex],
            _ => null
        };

        _archetype = ArchetypeIndex switch
        {
            > 0 => Ar.Exports[ArchetypeIndex - 1],
            < 0 => Ar.Imports[~ArchetypeIndex],
            _ => null
        };
    }
}