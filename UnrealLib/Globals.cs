using System;
using UnrealLib.Enums;

namespace UnrealLib;

public static class Globals
{
    public const uint PackageTag = 0x9E2A83C1;
    public const uint PackageTagSwapped = 0xC1832A9E;

    // Do not change the order of these elements!
    public static readonly string[] Languages = { "INT", "BRA", "CHN", "DEU", "DUT", "ESN", "FRA", "ITA", "JPN", "KOR", "POR", "RUS", "SWE", "ESM", "IND", "THA" };

    public static string GameToString(Game game) => game switch
    {
        Game.IB1 => "Infinity Blade I",
        Game.IB2 => "Infinity Blade II",
        Game.IB3 => "Infinity Blade III",
        Game.Vote => "VOTE!!!",
        Game.All => "All",
        _ => "Unknown"
    };

    public static string GameToStringShorthand(Game game) => game switch
    {
        Game.IB1 => "IB1",
        Game.IB2 => "IB2",
        Game.IB3 => "IB3",
        Game.Vote => "VOTE!!!",
        Game.All => "All",
        _ => "Unknown"
    };

    /// <summary>
    /// Returns a list of all languages applicable to the passed <see cref="Game"/>.
    /// </summary>
    public static Span<string> GetLanguages(Game game) => game switch
    {
        // Position of languages within the array take advantage of each game's supporting range.

        Game.IB3 => Languages.AsSpan(),
        Game.Vote => Languages.AsSpan(0, 1),
        _ => Languages.AsSpan(0, Languages.Length - 3)
    };

    /// <summary>
    /// Generically casts a string value to type T.
    /// </summary>
    /// <returns>True if cast successfully, False if not.</returns>
    // This is twice as slow as parsing directly and incurs a small memory penalty.
    public static bool TryConvert<T>(string value, out T outValue)
    {
        try
        {
            outValue = (T)Convert.ChangeType(value, typeof(T));
            return true;
        }
        catch
        {
            outValue = default;
            return false;
        }
    }
}