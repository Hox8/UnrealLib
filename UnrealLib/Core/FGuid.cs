using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FGuid : ISerializable
{
    private int A, B, C, D;

    public void Serialize(UnrealStream UStream)
    {
        UStream.Serialize(ref A);
        UStream.Serialize(ref B);
        UStream.Serialize(ref C);
        UStream.Serialize(ref D);
    }

    public override string ToString() => $"{A:X8}-{B:X8}-{C:X8}-{D:X8}";
}