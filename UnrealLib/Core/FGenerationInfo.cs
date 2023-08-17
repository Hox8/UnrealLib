using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FGenerationInfo : ISerializable
{
    private int ExportCount;
    private int NameCount;
    private int NetObjectCount;

    public void Serialize(UnrealStream UStream)
    {
        UStream.Serialize(ref ExportCount);
        UStream.Serialize(ref NameCount);
        UStream.Serialize(ref NetObjectCount);
    }

    public override string ToString() => $"{ExportCount}/{NameCount}/{NetObjectCount}";
}