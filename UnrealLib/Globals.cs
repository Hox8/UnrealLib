using System;
using UnrealLib.Enums;

namespace UnrealLib;

public static class Globals
{
    public const uint PackageTag = 0x9E2A83C1;
    public const uint PackageTagSwapped = 0xC1832A9E;

    // Do not change the order of these elements!
    public static ReadOnlySpan<string> Languages => new string[] { "INT", "BRA", "CHN", "DEU", "DUT", "ESN", "FRA", "ITA", "JPN", "KOR", "POR", "RUS", "SWE", "ESM", "IND", "THA" };

    public static string GetString(Game game, bool shorthand) => game switch
    {
        Game.IB1 when shorthand => "IB1",
        Game.IB2 when shorthand => "IB2",
        Game.IB3 when shorthand => "IB3",
        Game.Vote when shorthand => "VOTE",

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
    public static ReadOnlySpan<string> GetLanguages(Game game) => game switch
    {
        Game.IB3 => Languages,          // IB3 supports all 16 languages
        Game.Vote => Languages[0..1],   // Vote only supports American English
        _ => Languages[..^3]            // IB1 and IB2 do not support ESM, IND, and THA
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