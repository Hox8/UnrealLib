using System.Collections.Generic;
using UnrealLib.Core;
using UnrealLib.Enums;

namespace UnrealLib.Experimental.UnObj;

public class UState(FObjectExport export) : UStruct(export)
{
    #region Serialized members

    protected int ProbeMask;
    protected StateFlags StateFlags;
    protected short LabelTableOffset;
    protected List<KeyValuePair<long, int>> FuncMap;

    #endregion

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref ProbeMask);
        Ar.Serialize(ref LabelTableOffset);
        Ar.Serialize(ref StateFlags);
        Ar.Serialize(ref FuncMap);
    }
}