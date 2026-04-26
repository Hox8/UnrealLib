using System;
using UnrealLib.Core.Interfaces;
using PackageIndex = int;

namespace UnrealLib.Core;

#region Enums

[Flags]
public enum ExportFlags : uint
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,

    /// <summary>
    /// Whether the export was forced into the export table via RF_ForceTagExp.
    /// </summary>
    ForcedExport = 1 << 0,

    /// <summary>
    /// Indicates that this export was added by the script patcher, so the
    /// object's data will come from memory, not disk.
    /// </summary>
    ScriptPatcherExport = 1 << 1,

    /// <summary>
    /// Indicates that this export is a UStruct which will be patched with
    /// additional member fields by the script patcher.
    /// </summary>
    MemberFieldPatchPending = 1 << 2,

    /// <summary>
    /// All flags.
    /// </summary>
    AllFlags = uint.MaxValue,
}

#endregion

public abstract class FObjectResource
{
    #region Serialized members

    /// <summary>
    /// Name of the UObject represented by this resource.
    /// </summary>
    internal FName ObjectName;

    /// <summary>
    /// Location of the resource for this resource's Outer. Values of 0 indicates that this resource
    /// represents a top-level UPackage object.
    /// </summary>
    public PackageIndex OuterIndex;

    /// <summary>
    /// The location of the beginning of the data for this object resource.
    /// </summary>
    public int ResourceOffset;

    #endregion

    #region Transient members

    /// <summary>
    /// The package that houses this object resource.
    /// </summary>
    internal UnrealPackage Package;

    /// <summary>
    /// Location of this resource in its table.
    /// </summary>
    internal PackageIndex ResourceIndex;

    /// <summary>
    /// This object resource's Outer.
    /// </summary>
    public FObjectResource? Outer;

    #endregion

    #region Accessors

    public override string ToString() => GetNameStringFull();
    public string NameString => ObjectName.NameString;
    public string NameStringNumbered() => ObjectName.ToString();

    /// <summary>
    /// Returns the full name of a UObject in relation to its outers.
    /// </summary>
    public string GetNameStringFull()
    {
        int numSegments = 0;

        for (var outer = Outer; outer is not null; outer = outer.Outer)
        {
            numSegments++;
        }

        // Resulting string is ...{C}.{B}.{A}, where A is ourself and B onwards are our outers
        string[] names = new string[numSegments + 1];
        names[numSegments] = NameStringNumbered();

        for (var outer = Outer; outer is not null; outer = outer.Outer)
        {
            names[--numSegments] = outer.NameStringNumbered();
        }

        // string.join is more efficient than using a StringBuilder
        return string.Join('.', names);
    }

    #endregion

    /// <summary>
    /// Links a UObject resource to the given package.
    /// </summary>
    /// <param name="Ar"></param>
    /// <param name="index"></param>
    public abstract void Link(UnrealPackage Ar, int index);
}

public sealed class FObjectImport : FObjectResource, ISerializable
{
    #region Serialized members

    /// <summary>
    /// The name of the package that contains the class of the UObject represented by this resource.
    /// </summary>
    public FName ClassPackage;

    /// <summary>
    /// The name of the class for the UObject represented by this resource.
    /// </summary>
    public FName ClassName;

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        // Do not dilute name usage statistics if we aren't being
        // referenced--aka ready for removal/pruning
        //if (!Ar.IsLoading && !IsReferenced())
        //    return;

        ResourceOffset = Ar.Position;

        Ar.Serialize(ref ClassPackage);
        Ar.Serialize(ref ClassName);
        Ar.Serialize(ref OuterIndex);
        Ar.Serialize(ref ObjectName);

        if (!Ar.IsLoading)
        {
            if (OuterIndex == 0)
            {
                // Top-level imports must be packages
                UDebug.Assert(ClassName == "Package");
            }
        }
    }

    public override void Link(UnrealPackage Ar, int index)
    {
        Package = Ar;
        ResourceIndex = index;

        // Let our NameEntry know we're using it
        ObjectName.NameEntry.Register(this);

        if (Package.TryGetObjectAtIndex(OuterIndex, out Outer))
        {
#if !SLIM_BUILD
            // Let our Outer know we're using it
            Outer.RegisterUser(this);
#endif
        }
    }
}

public sealed class FObjectExport : FObjectResource, ISerializable
{
    private const ObjectFlags DefaultFlags = ObjectFlags.Transactional | ObjectFlags.LoadContextFlags;

    #region Serialized members

    /// <summary>
    /// Location of the resource for this export's class.
    /// A value of zero indicates that this export represents a UClass object.
    /// </summary>
    public PackageIndex ClassIndex;

    /// <summary>
    /// Location of the resource for this export's SuperField (parent).
    /// A value of zero indicates that this export does not represent a UStruct object.
    /// </summary>
    public PackageIndex SuperIndex;

    /// <summary>
    /// Location of the resource for this export's archetype (template).
    /// </summary>
    public PackageIndex ArchetypeIndex;

    /// <summary>
    /// Object flags for the UObject represented by this resource.
    /// </summary>
    public ObjectFlags ObjectFlags;

    /// <summary>
    /// The number of bytes to serialize when saving/loading this export's UObject.
    /// </summary>
    public int SerialSize;

    /// <summary>
    /// The location of the beginning of the data for this export's UObject.
    /// </summary>
    public int SerialOffset;

    /// <summary>
    /// Set of flags detailing whether this export was forced into the export table or not.
    /// </summary>
    public ExportFlags ExportFlags;

    /// <summary>
    /// If this object is a top-level package, this is the number of net serializable objects in each generation of that package.
    /// </summary>
    public int[] GenerationNetObjectCount = [];

    /// <summary>
    /// If this object is a top-level package, this is the GUID for the original package file.
    /// </summary>
    public Guid PackageGuid;

    /// <summary>
    /// If this object is a top-level package, this is the package flags for the original package file.
    /// </summary>
    public PackageFlags PackageFlags;

    #endregion

    #region Accessors

    public bool HasAnyFlags(ObjectFlags flags) => (ObjectFlags & flags) != 0;
    public bool IsTemplate() => HasAnyFlagsRecursive(ObjectFlags.ArchetypeObject | ObjectFlags.ClassDefaultObject);
    public bool IsDefaultObject() => HasAnyFlagsRecursive(ObjectFlags.ClassDefaultObject);
    public bool IsTransactionable() => HasAnyFlagsRecursive(ObjectFlags.Transactional);

    /// <summary>
    /// Iterates through an object's Outers until it finds a matching flag.
    /// </summary>
    /// <param name="flags">The flag to check for.</param>
    /// <returns>True if the flags were found; false otherwise.</returns>
    private bool HasAnyFlagsRecursive(ObjectFlags flags)
    {
        for (FObjectResource? curResource = this; curResource is not null; curResource = curResource.Outer)
        {
            if (curResource is FObjectExport export && export.HasAnyFlags(flags))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        ResourceOffset = Ar.Position;

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
        Ar.Serialize(ref PackageGuid);
        Ar.Serialize(ref PackageFlags);
    }

    public override void Link(UnrealPackage Ar, int index)
    {
        Package = Ar;
        ResourceIndex = index;

        // Let our NameEntry know we're using it
        ObjectName.NameEntry.Register(this);

        if (Package.TryGetObjectAtIndex(OuterIndex, out Outer))
        {
#if !SLIM_BUILD
            // Let our Outer know we're using it
            Outer.RegisterUser(this);
#endif
        }
    }
}
