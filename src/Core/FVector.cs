namespace UnrealLib.Core;

public class FVector
{
    public float X { get; internal set; }
    public float Y { get; internal set; }
    public float Z { get; internal set; }

    public FVector(UPK upk)
    {
        X = upk.UnStream.ReadFloat();
        Y = upk.UnStream.ReadFloat();
        Z = upk.UnStream.ReadFloat();
    }
}