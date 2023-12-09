using System.Text;
using UnrealLib.Enums;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

/// <summary>
/// Base class for UObject types.
/// </summary>
public abstract class FObjectResource
{
    #region Serialized members

    internal int OuterIndex;            // Object index of this resource's outer resource
    internal FName ObjectName;         // Name of the UObject represented by this resource

    #endregion

    #region Transient members
 
    public FObjectResource? Outer { get; protected set; }   // This resource's Outer
    public UnrealPackage Package { get; protected set; }    // The UnrealPackage this export belongs to
#if TRACK_OBJECT_USAGE
    public readonly List<FObjectResource> Users = [];              // List to track objects directly referencing this resource
    public readonly List<FObjectResource> ClassUsers = [];         // List of objects referencing this object as a class
#endif

#endregion

    #region Accessors

    public string GetName() => ObjectName.Name;    // Name of just this object resource
    public string GetFullName()
    {
        var sb = new StringBuilder(GetName());

        for (FObjectResource outer = Outer; outer is not null; outer = outer.Outer)
        {
            sb.Insert(0, $"{outer.GetName()}.");
        }

        return sb.ToString();
    }
    public override string ToString() => GetFullName();

    #endregion

    public virtual void Link(UnrealPackage Ar)
    {
        Package = Ar;

        ObjectName.NameEntry.Users.Add(this);

        Outer = Ar.GetObject(OuterIndex);

#if TRACK_OBJECT_USAGE
        Outer?.Users.Add(this);
#endif
    }
}

/// <summary>
/// UObject resource type for objects referenced by this package, but contained within another.
/// </summary>
public sealed class FObjectImport : FObjectResource, ISerializable
{
    #region Serialized members

    internal FName ClassPackage;
    internal FName ClassName;

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref ClassPackage);
        Ar.Serialize(ref ClassName);
        Ar.Serialize(ref OuterIndex);
        Ar.Serialize(ref ObjectName);
    }
}

/// <summary>
/// UObject resource type for objects contained within this package.
/// </summary>
public sealed class FObjectExport : FObjectResource, ISerializable
{
    #region Serialized members

    internal int ClassIndex;
    internal int SuperIndex;
    internal int ArchetypeIndex;

    internal ObjectFlags ObjectFlags;
    internal int SerialSize;
    internal int SerialOffset;

    internal ExportFlags ExportFlags;
    internal int[] GenerationNetObjectCount;
    internal FGuid Guid;
    internal PackageFlags PackageFlags;

    #endregion

    #region Transient members

    public FObjectResource? Class { get; private set; }
    public FObjectResource? Super { get; private set; }
    public FObjectResource? Archetype { get; private set; }
    public UObject? Object { get; private set; }

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref ClassIndex);
        Ar.Serialize(ref SuperIndex);
        Ar.Serialize(ref OuterIndex);
        Ar.Serialize(ref ObjectName);
        Ar.Serialize(ref ArchetypeIndex);

        Ar.Serialize(ref ObjectFlags);
        Ar.Serialize(ref SerialSize);
        Ar.Serialize(ref SerialOffset);

        Ar.Serialize(ref ExportFlags);
        Ar.Serialize(ref GenerationNetObjectCount);
        Ar.Serialize(ref Guid);
        Ar.Serialize(ref PackageFlags);
    }

    public override void Link(UnrealPackage Ar)
    {
        base.Link(Ar);

        Class = Ar.GetObject(ClassIndex);
        Super = Ar.GetObject(SuperIndex);
        Archetype = Ar.GetObject(ArchetypeIndex);

#if TRACK_OBJECT_USAGE
        Class?.ClassUsers.Add(this);
#endif
    }
}
