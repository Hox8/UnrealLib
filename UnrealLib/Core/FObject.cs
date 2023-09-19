using System;
using System.Collections.Generic;
using System.Text;
using UnrealLib.Enums;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

using PackageIdx = Int32;

public abstract class FObjectResource
{
    // Serialized
    internal PackageIdx OuterIndex;
    internal FName ObjectName;
    
    // Transient
    internal int SerializedIndex;
    internal int SerializedOffset;
    internal FObjectResource? Outer;
    
    public void Link(UnrealPackage pkg, int idx)
    {
        SerializedIndex = idx;
        
        // Link Outer object
        Outer = OuterIndex switch
        {
            > 0 => pkg.Exports[OuterIndex - 1],
            < 0 => pkg.Imports[~OuterIndex],
            _ => null
        };
        
        // Link names
        ObjectName.Name = pkg.Names[ObjectName.Index];
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder(ObjectName.ToString());

        FObjectResource obj = this;
        while (obj.Outer is not null)
        {
            sb.Insert(0, $"{obj.Outer.ObjectName}.");
            obj = obj.Outer;
        }

        return sb.ToString();
    }
}

public class FObjectImport : FObjectResource, ISerializable
{
    // Serialized
    internal FName ClassPackage;
    internal FName ClassName;

    public void Serialize(UnrealStream stream)
    {
        SerializedOffset = stream.Position;
        
        stream.Serialize(ref ClassPackage);
        stream.Serialize(ref ClassName);
        stream.Serialize(ref OuterIndex);
        stream.Serialize(ref ObjectName);
    }

    public new void Link(UnrealPackage pkg, int idx)
    {
        base.Link(pkg, idx);
        
        // Link names
        ClassPackage.Name = pkg.Names[ClassPackage.Index];
        ClassName.Name = pkg.Names[ClassName.Index];
        
        // Let ObjectName know that this import is using it
        ObjectName.Name.Register(this);
    }
}

public class FObjectExport : FObjectResource, ISerializable
{
    // Serialized
    internal PackageIdx ClassIndex;
    internal PackageIdx SuperIndex;
    internal PackageIdx ArchetypeIndex;
    internal ObjectFlags ObjectFlags;
    internal int SerialSize;
    internal int SerialOffset;
    internal ExportFlags ExportFlags;
    internal List<int> GenerationNetObjectCount;
    internal FGuid PackageGuid;
    internal PackageFlags PackageFlags;
    
    // Transient
    internal FObjectResource? Class;
    internal FObjectResource? Super;
    internal FObjectResource? Archetype;

    public void Serialize(UnrealStream stream)
    {
        SerializedOffset = stream.Position;
        
        stream.Serialize(ref ClassIndex);
        stream.Serialize(ref SuperIndex);
        stream.Serialize(ref OuterIndex);
        stream.Serialize(ref ObjectName);
        stream.Serialize(ref ArchetypeIndex);
        stream.Serialize(ref ObjectFlags);
        
        stream.Serialize(ref SerialSize);
        stream.Serialize(ref SerialOffset);
        
        stream.Serialize(ref ExportFlags);
        stream.Serialize(ref GenerationNetObjectCount);
        stream.Serialize(ref PackageGuid);
        stream.Serialize(ref PackageFlags);
    }

    public new void Link(UnrealPackage pkg, int idx)
    {
        base.Link(pkg, idx);
        
        // Link Class object
        Class = ClassIndex switch
        {
            > 0 => pkg.Exports[ClassIndex - 1],
            < 0 => pkg.Imports[~ClassIndex],
            _ => null
        };

        // Link Super object
        Super = SuperIndex switch
        {
            > 0 => pkg.Exports[SuperIndex - 1],
            < 0 => pkg.Imports[~SuperIndex],
            _ => null
        };

        // Link Archetype object
        Archetype = ArchetypeIndex switch
        {
            > 0 => pkg.Exports[ArchetypeIndex - 1],
            < 0 => pkg.Imports[~ArchetypeIndex],
            _ => null
        };
        
        // Let ObjectName know that this export is using it
        ObjectName.Name.Register(this);
    }

    /// <summary>
    /// Replaces this export's UObject data entirely with that a passed byte span.
    /// </summary>
    /// <remarks>
    /// Replacement UObject data must be self-contained i.e. containing UObject header, footer etc.
    /// </remarks>
    public void ReplaceData(UnrealStream stream, Span<byte> data)
    {
        stream.StartSaving();

        SerialSize = data.Length;
        SerialOffset = stream.Length;

        stream.Position = SerializedOffset;
        Serialize(stream);

        stream.Position = stream.Length;
        stream.Write(data);
        
        // stream.StartLoading();
    }
}