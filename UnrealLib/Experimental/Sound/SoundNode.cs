using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;

namespace UnrealLib.Experimental.Sound;

public class SoundNode(FObjectExport export) : UObject(export)
{
    int NodeUpdateHint;
    SoundNode[] ChildNodes;
}
