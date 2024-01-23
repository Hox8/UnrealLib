namespace UnrealLib.Core;

public record struct FGuid
{
    public int A, B, C, D;

    public override readonly string ToString() => $"{A:X8}-{B:X8}-{C:X8}-{D:X8}";
}
