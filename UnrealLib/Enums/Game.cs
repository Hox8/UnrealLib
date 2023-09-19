using System;

namespace UnrealLib.Enums;

[Flags]
public enum Game : byte
{
    // Coalesced depends on this specific order
    Unknown = 0,
    IB1 = 1 << 1,
    IB2 = 1 << 2,
    IB3 = 1 << 3,
    Vote = 1 << 4,
    All = Vote | IB1 | IB2 | IB3
}