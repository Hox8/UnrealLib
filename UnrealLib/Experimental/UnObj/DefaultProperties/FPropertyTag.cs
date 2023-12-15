using System.Runtime.InteropServices;
using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj.DefaultProperties;

#if KEEP_UNKNOWN_DEFAULT_PROPERTIES
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
#endif

/// <summary>
/// Stores the "metadata" of a default property.
/// </summary>
public class FPropertyTag
{
    #region Serialized members

    public FName Name, Type, TargetName;
    public int Size, ArrayIndex;

#if KEEP_UNKNOWN_DEFAULT_PROPERTIES
    // This field stores the values of unrecognized properties where they can't be assigned directly to a UObject field
    // In an ideal world, this field will remain unused
    public FPropertyValue Value;
#endif

    #endregion

    #region Accessors

    public override string ToString() => Name.GetString;

    #endregion

    /// <returns>True if this property was fully-serialized (not equal to "None"), otherwise false.</returns>
    public bool Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref Name);
        if (Name == "None") return false;

        Ar.Serialize(ref Type);
        Ar.Serialize(ref Size);
        Ar.Serialize(ref ArrayIndex);

        // Structs and Enums have an additional serialized field we need to take care of
        if (Type == "StructProperty" || Type == "ByteProperty")
        {
            Ar.Serialize(ref TargetName);
        }

        return true;
    }
}
