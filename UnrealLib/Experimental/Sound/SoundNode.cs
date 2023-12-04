using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;

namespace UnrealLib.Experimental.Sound;

public class SoundNode(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UObject(stream, pkg, export)
{
    int NodeUpdateHint;
    SoundNode[] ChildNodes;
}
