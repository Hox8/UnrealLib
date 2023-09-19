namespace UnrealLib.Core;

public struct FGuid
{
    private int A, B, C, D;

    public override string ToString() => $"{A:X8}-{B:X8}-{C:X8}-{D:X8}";
}
