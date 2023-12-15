using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj;

public class UField(FObjectExport export) : UObject(export)
{
    protected UField? Next;

    public override void Serialize(UnrealArchive Ar)
    {
        int nextIndex = default;

        base.Serialize(Ar);

        Ar.Serialize(ref nextIndex);

        if (((UnrealPackage)Ar).GetObject(nextIndex) is FObjectExport export && export.Object is UField ufield)
        {
            Next = ufield;
        }
    }
}