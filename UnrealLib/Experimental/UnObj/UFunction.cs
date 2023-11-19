using UnrealLib.Enums;
using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj;

public class UFunction(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UStruct(stream, pkg, export)
{
    protected FunctionFlags FunctionFlags;
    protected short iNative;
    protected short RepOffset;
    protected byte OperPrecedence;

    public override void Serialize(UnrealStream stream)
    {
        base.Serialize(stream);

        stream.Serialize(ref iNative);
        stream.Serialize(ref OperPrecedence);
        stream.Serialize(ref FunctionFlags);

        if ((FunctionFlags & FunctionFlags.Net) != 0)
        {
            stream.Serialize(ref RepOffset);
        }
    }
}