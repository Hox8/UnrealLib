using UnrealLib.Core;
using UnrealLib.Enums;

namespace UnrealLib.Experimental.UnObj;

// Base abstract class for individual UnrealScript properties. This is NOT (directly) related to default properties.
// Does not contain a value. More of a contract defining what the variable will be like, but isn't used for instantiation.
// Child classes may contain members to help describe its value, but will never store a value directly.
public abstract class UProperty(FObjectExport export) : UField(export)
{
    #region Serialized members

    protected int ArrayDim;
    protected int ElementSize;
    protected PropertyFlags PropertyFlags;
    protected short RepOffset;

    #endregion

    public new void Serialize()
    {
        base.Serialize();

        Ar.Serialize(ref ArrayDim);
        Ar.Serialize(ref PropertyFlags);

        if ((PropertyFlags & PropertyFlags.Net) != 0)
        {
            Ar.Serialize(ref RepOffset);
        }
    }
}

//public class UIntProperty(UnrealArchive stream, UnrealPackage pkg, FObjectExport export) : UProperty(stream, pkg, export);

//internal class UArrayProperty(UnrealArchive stream, UnrealPackage pkg, FObjectExport export) : UProperty(stream, pkg, export)
//{
//    UProperty Inner;

//    public new void Serialize(FPropertyTag tag)
//    {
//    }
//}