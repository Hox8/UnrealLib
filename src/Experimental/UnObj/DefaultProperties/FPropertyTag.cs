using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealLib.Core;
using UnrealLib.Experimental.Textures.TGA;
using UnrealLib.Interfaces;

namespace UnrealLib.Experimental.UnObj.DefaultProperties;

// @TODO use a pack of 4 and bring down reference type offsets to 4 from 8
[StructLayout(LayoutKind.Explicit)]
public struct FPropertyValue
{
    [FieldOffset(0)] internal bool Bool;
    [FieldOffset(0)] internal byte Byte;
    [FieldOffset(0)] internal int Int;
    [FieldOffset(0)] internal float Float;
    [FieldOffset(8)] internal string String;
    [FieldOffset(8)] internal FName Name;
    [FieldOffset(8)] internal byte[] Bytes; // Used to contain unrecognized arrays/structs

    public FPropertyValue(bool value) => Bool = value;
    public FPropertyValue(byte value) => Byte = value;
    public FPropertyValue(int value) => Int = value;
    public FPropertyValue(float value) => Float = value;
    public FPropertyValue(string value) => String = value;
    public FPropertyValue(in FName value) => Name = value;
    public FPropertyValue(byte[] value) => Bytes = value;
}

/// <summary>
/// Stores the "metadata" of a default property.
/// </summary>
public class FPropertyTag
{
    #region Serialized members

    public FName Name, Type, TargetName;
    public int Size, ArrayIndex, ArraySize;

    // This field stores the values of unrecognized properties where they can't be assigned directly to a UObject field
    // In an ideal world, this field will remain unused
    public FPropertyValue Value;

    #endregion

    #region Accessors

    public override string ToString() => Name.ToString();

    #endregion

    #region Writers

    // Not completely sure as to how I'm going to deal with Value moving forward...

    public static void WriteNew(UnrealPackage Ar, string name, int arrayIndex, bool value)
    {
        new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("BoolProperty"), ArrayIndex = arrayIndex, Size = 0, Value = new(value) }.Serialize(Ar);
    }

    public static void WriteNew(UnrealPackage Ar, string name, int arrayIndex, byte value)
    {
        new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("ByteProperty"), TargetName = Ar.GetOrAddName("None"), ArrayIndex = arrayIndex, Size = 1 }.Serialize(Ar);
        Ar.Serialize(ref value);
    }

    public static void WriteNew(UnrealPackage Ar, string name, int arrayIndex, int value, bool objOverride = false)
    {
        new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName(objOverride ? "ObjectProperty" : "IntProperty"), ArrayIndex = arrayIndex, Size = 4 }.Serialize(Ar);
        Ar.Serialize(ref value);
    }

    public static void WriteNew(UnrealPackage Ar, string name, int arrayIndex, float value)
    {
        new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("FloatProperty"), ArrayIndex = arrayIndex, Size = 4 }.Serialize(Ar);
        Ar.Serialize(ref value);
    }

    public static void WriteNew(UnrealPackage Ar, string name, int arrayIndex, string value)
    {
        new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("StrProperty"), ArrayIndex = arrayIndex, Size = value.Length }.Serialize(Ar);
        Ar.Serialize(ref value);
    }

    public static void WriteNew(UnrealPackage Ar, string name, int arrayIndex, FName value)
    {
        new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("NameProperty"), ArrayIndex = arrayIndex, Size = 8 }.Serialize(Ar);
        Ar.Serialize(ref value);
    }

    public static void WriteNew(UnrealPackage Ar, string name, string targetName, int arrayIndex, string value)
    {
        new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("ByteProperty"), TargetName = Ar.GetOrAddName(targetName), ArrayIndex = arrayIndex, Size = 8 }.Serialize(Ar);
        Ar.GetOrAddName(value).Serialize(Ar);
    }

    public static void WriteNew<T>(UnrealPackage Ar, string name, int arrayIndex, T value, byte _ = 0) where T : ISerializable
    {
        int prePos = (int)Ar.Position;

        // Construct and write out tag
        var tag = new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("StructProperty"), ArrayIndex = arrayIndex };
        tag.Serialize(Ar);

        // Let object perform binary serialization
        value.Serialize(Ar);
        int postPos = (int)Ar.Position;

        // Update size and reserialize tag
        Ar.Position = prePos;
        tag.Size = postPos - prePos;
        tag.Serialize(Ar);

        Ar.Position = postPos;

        Debug.WriteLine($"Wrote variable UProperty: {tag.Name}, {tag.Type} ({tag.Size} bytes)");
    }

    public unsafe static void WriteNew<T>(UnrealPackage Ar, string name, string targetName, int arrayIndex, T value, byte _ = 0, byte __ = 0) where T : unmanaged
    {
        new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("StructProperty"), TargetName = Ar.GetOrAddName(targetName), ArrayIndex = arrayIndex, Size = sizeof(T) }.Serialize(Ar);
        Ar.Serialize(ref value);
    }

    public unsafe static void WriteNew<T>(UnrealPackage Ar, string name, string targetName, string itemName, int arrayIndex, T value, byte _ = 0, byte __ = 0) where T : unmanaged
    {
        new FPropertyTag() { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("StructProperty"), TargetName = Ar.GetOrAddName(targetName), ArrayIndex = arrayIndex, Size = sizeof(T) + 8 }.Serialize(Ar);

        var item = Ar.GetOrAddName(itemName);

        Ar.Serialize(ref item);
        Ar.Serialize(ref value);
    }

    public static void WriteNew<T>(UnrealPackage Ar, string name, string target, int arrayIndex, T value) where T : PropertyHolder
    {
        int offset_tagStart = (int)Ar.Position;

        // Construct and write out tag
        var tag = new FPropertyTag { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("StructProperty"), TargetName = Ar.GetOrAddName(target), ArrayIndex = arrayIndex };
        tag.Serialize(Ar);

        int offset_valueStart = (int)Ar.Position;

        // Let object write out its own tags and mark final offset
        value.WriteProperties(Ar);

        // Calculate size and seek back to the start of the root tag
        tag.Size = (int)Ar.Position - offset_valueStart;
        Ar.Position = offset_tagStart;

        if (tag.Size <= 8)
        {
            // We didn't write any properties. Clear out the tag + bulk data
            Ar.Length = Ar.Position;
        }
        else
        {
            // We wrote at least one property. Re-serialize updated tag and skip over the bulk data we've just written
            tag.Serialize(Ar);
            Ar.Position += tag.Size;
        }
    }

    public static void WriteNew<T>(UnrealPackage Ar, string name, T[] value) where T : unmanaged
    {
        var tagOffset = (int)Ar.Position;

        var tag = new FPropertyTag { Name = Ar.GetOrAddName(name), Type = Ar.GetOrAddName("ArrayProperty"), ArraySize = value.Length };
        tag.Serialize(Ar);

        var valueOffset = (int)Ar.Position;
        Ar.Serialize(ref value, value.Length);

        tag.Size = sizeof(int) + ((int)Ar.Position - valueOffset);

        Ar.Position = tagOffset;
        tag.Serialize(Ar);

        Ar.Position += tag.Size - sizeof(int);

        Debug.WriteLine($"Wrote variable UProperty: {tag.Name}, {tag.Type} ({tag.Size} bytes)");
    }

    #endregion

    public void SkipUnknownProperty(UnrealArchive Ar)
    {
        Ar.Position += Size;

        if (Type.ToString() == "StructProperty")
        {
            Ar.Position -= 8;   // FName TargetName
        }
        else if (Type.ToString() == "ArrayProperty")
        {
            Ar.Position -= 4;   // int ArraySize
        }
    }

    /// <returns>True if this property was fully-serialized (not equal to "None"), otherwise false.</returns>
    public bool Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Name);
        if (Name.ToString() == "None") return false;

        Ar.Serialize(ref Type);
        Ar.Serialize(ref Size);         // @TODO THIS IS WRONG! Must calculate this during saves
        Ar.Serialize(ref ArrayIndex);

        // These fields are treated as metadata and do not contribute
        // to the Size field, hence they must be serialized now.
        switch (Type.ToString())
        {
            case "StructProperty":
            case "ByteProperty":
                Ar.Serialize(ref TargetName);
                break;
            case "BoolProperty":
                Ar.Serialize(ref Value.Bool);
                break;
            case "ArrayProperty":
                Ar.Serialize(ref ArraySize);
                break;
        }

        return true;
    }
}
