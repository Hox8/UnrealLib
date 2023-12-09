using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj;

public class UField(FObjectExport export) : UObject(export)
{
    protected UField? Next;

    public override void Serialize()
    {
        int nextIndex = default;

        base.Serialize();

        Ar.Serialize(ref nextIndex);

        if (Ar.GetObject(nextIndex) is FObjectExport export && export.Object is UField ufield)
        {
            Next = ufield;
        }
    }
}