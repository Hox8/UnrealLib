using System.Collections.Generic;
using UnrealLib.Core;
using UnrealLib.Enums;

namespace UnrealLib.Experimental.UnObj;

public class UState(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UStruct(stream, pkg, export)
{
    protected int ProbeMask;
    protected StateFlags StateFlags;
    protected short LabelTableOffset;
    protected List<KeyValuePair<long, int>> FuncMap;   // KeyValuePair<FName, UFunction*>

    public override void Serialize(UnrealStream stream)
    {
        base.Serialize(stream);

        stream.Serialize(ref ProbeMask);
        stream.Serialize(ref LabelTableOffset);
        stream.Serialize(ref StateFlags);
        stream.Serialize(ref FuncMap);
    }
}