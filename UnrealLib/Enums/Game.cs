namespace UnLib.Enums;

[Flags]
public enum Game : byte
{
    Unknown = 0,
    Vote = 1 << 0,
    IB1 = 1 << 1,
    IB2 = 1 << 2,
    IB3 = 1 << 3,
    All = Vote | IB1 | IB2 | IB3
}