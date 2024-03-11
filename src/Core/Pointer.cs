using UnrealLib.Experimental.UnObj;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public record struct Pointer<T> : ISerializable where T : UObject
{
    public T? Object;

    public void Serialize(UnrealArchive _)
    {
        var Ar = (UnrealPackage)_;

        int index = Object?.Export?.FormattedIndex ?? 0;
        Ar.Serialize(ref index);

        if (Ar.IsLoading)
        {
            if (Ar.GetObject(index) is FObjectExport export)
            {
                Object = Ar.GetUObject(export, false) as T;
            }
        }
    }
}
