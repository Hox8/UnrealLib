using System;
using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj;

public class UStruct(UnrealStream stream, UnrealPackage pkg, FObjectExport export) : UField(stream, pkg, export)
{
    protected FObjectExport? SuperStruct;
    protected FObjectExport? Children;
    protected int ScriptBytecodeSize;
    protected int ScriptStorageSize;
    protected byte[] Script;

    public override void Serialize(UnrealStream stream)
    {
        int superStructIndex = SuperStruct?.SerializedIndex ?? 0;
        int childIndex = Children?.SerializedIndex ?? 0;

        base.Serialize(stream);

        stream.Serialize(ref superStructIndex);
        stream.Serialize(ref childIndex);

        stream.Serialize(ref ScriptBytecodeSize);
        stream.Serialize(ref ScriptStorageSize);

        if (stream.IsLoading)
        {
            Script = GC.AllocateUninitializedArray<byte>(ScriptStorageSize);
            stream.ReadExactly(Script);
        }
        else
        {
            stream.Write(Script);
        }

        // MOVE INTO LINK METHOD (or really just outside the base serialize method)
        if (stream.IsLoading)
        {
            if (_pkg.GetExport(superStructIndex) is FObjectExport superStruct)
            {
                SuperStruct = superStruct;
            }
#if DEBUG
            else if (_pkg.GetExport(superStructIndex) is FObjectImport)
                throw new Exception();
#endif

            if (_pkg.GetExport(childIndex) is FObjectExport children)
            {
                Children = children;
            }
#if DEBUG
            else if (_pkg.GetExport(childIndex) is FObjectImport)
                throw new Exception();
#endif
        }
    }
}