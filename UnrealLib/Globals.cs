using UnrealLib.Enums;

namespace UnrealLib;

public static class Globals
{
    internal const uint PackageTag = 0x9E2A83C1;
    internal const uint PackageTagSwapped = 0xC1832A9E;
    
    internal const string CoalescedKeyIB3 = "6nHmjd:hbWNf=9|UO2:?;K0y+gZL-jP5";
    internal const string CoalescedKeyIB2 = "|FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B";
    internal const string CoalescedKeyVOTE = "DKksEKHkldF#(WDJ#FMS7jla5f(@J12|";

    internal const string CoalescedHelperName = "ibhelper";

    // Do not change the order of these elements.
    public static readonly string[] Languages =
        { "INT", "BRA", "CHN", "DEU", "DUT", "ESN", "FRA", "ITA", "JPN", "KOR", "POR", "RUS", "SWE", "ESM", "IND", "THA"};

    public static string GameToString(Game game) => game switch
    {
        Game.IB1 => "Infinity Blade I",
        Game.IB2 => "Infinity Blade II",
        Game.IB3 => "Infinity Blade III",
        Game.Vote => "VOTE!!!",
        Game.All => "All",
        _ => "Unknown"
    };

    /// <summary>
    /// Returns a list of all languages applicable to the passed <see cref="Game"/>.
    /// </summary>
    /// <param name="game"></param>
    /// <returns></returns>
    public static Span<string> GetLanguages(Game game) => game switch
    {
        Game.Vote => Languages.AsSpan(0, 1),
        Game.IB3 => Languages.AsSpan(0, Languages.Length),
        _ => Languages.AsSpan(0, Languages.Length - 3)
    };
}