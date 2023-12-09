namespace UnrealLib.Core;

public readonly struct FGenerationInfo
{
    public readonly int ExportCount, NameCount, NetObjectCount;

    public override string ToString() => $"{ExportCount} / {NameCount} / {NetObjectCount}";
}