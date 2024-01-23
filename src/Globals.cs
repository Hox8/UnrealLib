using System;
using UnrealLib.Enums;

namespace UnrealLib;

public static class Globals
{
    public const uint PackageTag = 0x9E2A83C1;
    public const uint PackageTagSwapped = 0xC1832A9E;
    public const int CompressionChunkSize = 131072;     // Anything much larger than this will cause crashes during De/CompressPackage()! Problem probably lies with DotNetZip

    // Final package versions
    public const int PackageVerIB3 = 868;
    public const int PackageVerIB2 = 864;
    public const int PackageVerVOTE = 864;
    public const int PackageVerIB1 = 788;

    // Final engine versions
    public const int EngineVerIB3 = 13249;
    public const int EngineVerIB2 = 9714;
    public const int EngineVerVOTE = 9711;
    public const int EngineVerIB1 = 7982;

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

    /// <summary>
    /// Formats a count of bytes into a human-readable string. Automatically converts between KB, MB, and GB.
    /// </summary>
    /// <remarks>Appends the unit onto the end of the string, for example: "4.13 KB".</remarks>
    public static string FormatSizeString(long numBytes)
    {
        const int Kilobyte = 1024;
        const int Megabyte = Kilobyte * 1024;
        const int Gigabyte = Megabyte * 1024;

        return numBytes switch
        {
            >= Gigabyte => $"{(double)numBytes / Gigabyte:N2} GB",
            >= Megabyte => $"{(float)numBytes / Megabyte:N2} MB",
            _ => $"{(float)numBytes / Kilobyte:N2} KB"
        };
    }
}