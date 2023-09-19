namespace UnrealLib.Core;

public struct FGenerationInfo
{
    private int ExportCount, NameCount, NetObjectCount;

    public override string ToString() => $"{ExportCount}/{NameCount}/{NetObjectCount}";
}
