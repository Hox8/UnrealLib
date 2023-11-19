using UnrealLib.Core;
using UnrealLib.Enums;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.UnObj;

// Base abstract class for individual UnrealScript properties. This is NOT (directly) related to default properties.
// Does not contain a value. More of a contract defining what the variable will be like, but isn't used for instantiation.
// Child classes may contain members to help describe its value, but will never store a value directly.
public abstract class UProperty(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UField(stream, pkg, export)
{
    protected int ArrayDim;
    protected int ElementSize;
    protected PropertyFlags PropertyFlags;
    protected short RepOffset;

    public new void Serialize(UnrealStream stream)
    {
        base.Serialize(stream);

        stream.Serialize(ref ArrayDim);
        stream.Serialize(ref PropertyFlags);

        if ((PropertyFlags & PropertyFlags.Net) != 0)
        {
            stream.Serialize(ref RepOffset);
        }
    }
}

public class UIntProperty(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UProperty(stream, pkg, export);

internal class UArrayProperty(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UProperty(stream, pkg, export)
{
    UProperty Inner;

    public new void Serialize(FPropertyTag tag)
    {
    }
}