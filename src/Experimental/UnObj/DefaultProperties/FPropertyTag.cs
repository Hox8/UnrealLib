using System;
using System.Runtime.InteropServices;
using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj.DefaultProperties;

[StructLayout(LayoutKind.Explicit)]
public struct FPropertyValue
{
    [FieldOffset(0)] internal bool Bool;
    [FieldOffset(0)] internal byte Byte;
    [FieldOffset(0)] internal int Int;
    [FieldOffset(0)] internal float Float;
    [FieldOffset(8)] internal string String;
    [FieldOffset(8)] internal FName Name;
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

    public override string ToString() => Name.GetString;

    #endregion

    #region Write test


    #endregion

    /// <returns>True if this property was fully-serialized (not equal to "None"), otherwise false.</returns>
    public bool Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Name);
        if (Name == "None") return false;

        Ar.Serialize(ref Type);
        Ar.Serialize(ref Size);
        Ar.Serialize(ref ArrayIndex);

        // These fields are treated as metadata and do not contribute
        // to the Size field, hence they must be serialized now.
        switch (Type.GetString)
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
