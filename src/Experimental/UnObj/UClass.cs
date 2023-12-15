using System.Collections.Generic;
using UnrealLib.Core;
using UnrealLib.Enums;

namespace UnrealLib.Experimental.UnObj;

public class UClass(FObjectExport export) : UState(export)
{
    #region Serialized members

    protected ClassFlags ClassFlags;
    protected int ClassWithin;
    protected FName ClassConfigName;
    protected KeyValuePair<long, int>[] ComponentNameToDefaultObjectMap;   // KeyValuePair<FName, UComponent*>
    protected long[] Interfaces;
    protected FName DLLBindName;

    #endregion

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref ClassFlags);
        Ar.Serialize(ref ClassWithin);
        Ar.Serialize(ref ClassConfigName);
        Ar.Serialize(ref ComponentNameToDefaultObjectMap);
        Ar.Serialize(ref Interfaces);
        Ar.Serialize(ref DLLBindName);
    }
}