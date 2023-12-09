using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj;

public class UStruct(FObjectExport export) : UField(export)
{
    #region Serialized members

    protected int SuperIndex;
    protected int ChildIndex;
    protected int ScriptBytecodeSize;
    protected int ScriptStorageSize;
    protected byte[] Script;

    #endregion

    #region Transient members

    protected FObjectExport? SuperStruct { get; private set; }
    protected FObjectExport? Children { get; private set; }

    #endregion

    public override void Serialize()
    {
        base.Serialize();

        Ar.Serialize(ref SuperIndex);
        Ar.Serialize(ref ChildIndex);

        Ar.Serialize(ref ScriptBytecodeSize);
        Ar.Serialize(ref ScriptStorageSize);

        if (Ar.IsLoading)
        {
            Script = new byte[ScriptStorageSize];
            Ar.ReadExactly(Script);
        }
        else
        {
            Ar.Write(Script);
        }

        // Link

        if (Ar.IsLoading)
        {
            if (Ar.GetExport(SuperIndex) is FObjectExport superStruct)
            {
                SuperStruct = superStruct;
            }

            if (Ar.GetExport(ChildIndex) is FObjectExport children)
            {
                Children = children;
            }
        }
    }
}