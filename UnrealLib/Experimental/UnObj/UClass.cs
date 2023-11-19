using System.Collections.Generic;
using UnrealLib.Core;
using UnrealLib.Enums;

namespace UnrealLib.Experimental.UnObj;

//unsafe struct FImplementedInterface
//{
//    UClass* Class;
//    UProperty* PointerProperty;
//}

public class UClass(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UState(stream, pkg, export)
{
    protected ClassFlags ClassFlags;
    protected int ClassWithin;
    protected FName ClassConfigName;
    protected List<KeyValuePair<long, int>> ComponentNameToDefaultObjectMap;   // KeyValuePair<FName, UComponent*>
    protected List<long> Interfaces;  // List<FImplementedInterface>
    protected FName DLLBindName;

    public override void Serialize(UnrealStream stream)
    {
        base.Serialize(stream);

        stream.Serialize(ref ClassFlags);
        stream.Serialize(ref ClassWithin);
        stream.Serialize(ref ClassConfigName);
        stream.Serialize(ref ComponentNameToDefaultObjectMap);
        stream.Serialize(ref Interfaces);
        stream.Serialize(ref DLLBindName);
    }
}