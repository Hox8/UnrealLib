using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj;

public class UField(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UObject(stream, pkg, export)
{
    protected UField? Next;

    public override void Serialize(UnrealStream stream)
    {
        int nextIndex = default;

        base.Serialize(stream);

        stream.Serialize(ref nextIndex);
        if (pkg.GetExport(nextIndex)?.Object is UField ufield)
        {
            Next = ufield;
        }
    }
}