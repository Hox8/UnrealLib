using UnrealLib.Enums;
using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj;

public class UFunction(FObjectExport export) : UStruct(export)
{
    #region Serialized members

    protected FunctionFlags FunctionFlags;
    protected short iNative;
    protected short RepOffset;
    protected byte OperPrecedence;

    #endregion

    public override void Serialize()
    {
        base.Serialize();

        Ar.Serialize(ref iNative);
        Ar.Serialize(ref OperPrecedence);
        Ar.Serialize(ref FunctionFlags);

        // @TODO this check exists in at least one other UObject class. Helper?
        if ((FunctionFlags & FunctionFlags.Net) != 0)
        {
            Ar.Serialize(ref RepOffset);
        }
    }
}