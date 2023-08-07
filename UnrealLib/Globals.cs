using UnLib.Enums;

namespace UnLib;

public static class Globals
{
    internal const uint PackageTag = 0x9E2A83C1;
    internal const uint PackageTagSwapped = 0xC1832A9E;
    
    internal const string CoalescedKeyIB3 = "6nHmjd:hbWNf=9|UO2:?;K0y+gZL-jP5";
    internal const string CoalescedKeyIB2 = "|FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B";
    internal const string CoalescedKeyVOTE = "DKksEKHkldF#(WDJ#FMS7jla5f(@J12|";

    public static string GameToString(Game game) => game switch
    {
        Game.IB1 => "Infinity Blade I",
        Game.IB2 => "Infinity Blade II",
        Game.IB3 => "Infinity Blade III",
        Game.Vote => "VOTE!!!",
        Game.All => "All",
        _ => "Unknown"
    };
}