using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnrealLib.Enums;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

using PackageIdx = Int32;

public abstract class FObjectResource
{
    // Serialized
    internal PackageIdx OuterIndex;
    internal FName ObjectName;
    
    // Transient
    public int SerializedIndex { get; protected set; }
    public int SerializedOffset { get; protected set; }
    public FObjectResource? Outer { get; private set; }
    
    // Properties
    public string Name => ObjectName.Name.Name; // @TODO: This was incorrectly set on some lightmap exports in 00_P_AsiaForest
    public string FullName => this.ToString();
    
    public virtual void Link(UnrealPackage pkg, int idx)
    {
        SerializedIndex = idx;
        
        // Link Outer object
        Outer = pkg.GetObject(OuterIndex);
        
        // Link names
        ObjectName.Name = pkg.GetName(ObjectName.Index);
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
    public FName ClassPackage;
    public FName ClassName;

    public void Serialize(UnrealStream stream)
    {
        SerializedOffset = (int)stream.Position;
        
        stream.Serialize(ref ClassPackage);
        stream.Serialize(ref ClassName);
        stream.Serialize(ref OuterIndex);
        stream.Serialize(ref ObjectName);
    }

    public override void Link(UnrealPackage pkg, int idx)
    {
        base.Link(pkg, idx);
        
        // Link names
        ClassPackage.Name = pkg.GetName(ClassPackage.Index);
        ClassName.Name = pkg.GetName(ClassName.Index);
        
        // Let ObjectName know that this import is using it
        ObjectName.Name.Register(this);
    }
}

public class FObjectExport : FObjectResource, ISerializable
{
    #region Serialized

    /// <summary>Serialized index pointing to this UObject's class in the import/export table.</summary>
    internal PackageIdx ClassIndex;
    /// <summary>Serialized index pointing to this UObject's parent object in the import/export table.</summary>
    internal PackageIdx SuperIndex;
    /// <summary>Serialized index pointing to this UObject's template object in the import/export table.</summary>
    internal PackageIdx ArchetypeIndex;
    /// <summary>Flags describing this UObject.</summary>
    internal ObjectFlags ObjectFlags;
    /// <summary>The length of this UObject's data after serializing to disk.</summary>
    public int SerialSize;
    /// <summary>The file offset where this UObject's data is serialized.</summary>
    public int SerialOffset;
    /// <summary>Flags describing this FObjectExport.</summary>
    internal ExportFlags ExportFlags;
    internal List<int> GenerationNetObjectCount;
    internal FGuid PackageGuid;
    internal PackageFlags PackageFlags;

    #endregion

    #region Transient

    public FObjectResource Class { get; private set; }
    public FObjectResource? Super { get; private set; }
    public FObjectResource? Archetype { get; private set; }
    public UObject? Object { get; private set; }

    #endregion

    public void Serialize(UnrealStream stream)
    {
        SerializedOffset = (int)stream.Position;
        
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

    public override void Link(UnrealPackage pkg, int idx)
    {
        base.Link(pkg, idx);

        // Link Class object
        // If ClassIndex is 0, set Class to UClass. Class reference should NEVER be null.
        // PKG needs to cache common names like UClass and many more so we can do this without repeated lookups
        // @TODO
        // Class = ClassIndex == 0 ? null : pkg.GetObject(ClassIndex);
        Class = pkg.GetObject(ClassIndex);

        // Link Super object
        Super = pkg.GetObject(SuperIndex);

        // Link Archetype object
        Archetype = pkg.GetObject(ArchetypeIndex);
        
        // Let ObjectName know that this export is using it
        ObjectName.Name.Register(this);
    }
}