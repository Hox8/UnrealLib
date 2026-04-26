using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace UnrealLib.Core;

/// <summary>
/// Global utilities, constants, etc.
/// </summary>
public static class Globals
{
    // TOOD: These are messy and ill-placed. Names smell too.
    public static readonly FileStreamOptions TempReadStream = new() { Mode = FileMode.Open, Access = FileAccess.Read, Options = FileOptions.DeleteOnClose };
    public static readonly FileStreamOptions TempReadWriteStream = new() { Mode = FileMode.OpenOrCreate, Access = FileAccess.ReadWrite, Options = FileOptions.DeleteOnClose };

    public const uint PackageTag = 0x9E2A83C1;
    public const uint PackageTagSwapped = 0xC1832A9E;
    public const int CompressionChunkSize = 131072;

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

    /// <summary>
    /// Languages used between IB1 and IB3. Prefer using <see cref="GetLanguages(Game)"/> to interact with these.
    /// </summary>
    /// <remarks>
    /// Do not change the order of these elements!
    /// </remarks>
    internal static readonly string[] Languages = [ "INT", "BRA", "CHN", "DEU", "DUT", "ESN", "FRA", "ITA", "JPN", "KOR", "POR", "RUS", "SWE", "ESM", "IND", "THA" ];

    /// <summary>
    /// Returns a list of all languages applicable to the passed <see cref="Game"/>.
    /// </summary>
    public static ReadOnlySpan<string> GetLanguages(Game game) => game switch
    {
        Game.IB3 => Languages,                              // IB3 supports all 16 languages
        Game.VOTE => Languages.AsSpan(0, 1),                // Vote supports only International (English)
        _ => Languages.AsSpan(0, Languages.Length - 3)      // IB1 and IB2 do not support ESM, IND, and THA
    };

#if UNIX
    public const char PathSeparator = '/';
#else
    public const char PathSeparator = '\\';
#endif

    #region Game type

    public enum Game : byte
    {
        IB1,
        IB2,
        IB3,
        VOTE,
        MAX
    }

    /// <summary>
    /// Determines if a game represents a valid game option.
    /// </summary>
    /// <param name="game">Game enum to check.</param>
    /// <returns>True if the passed Game enum is a game entry.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsGameValid(Game game) => game < Game.MAX;

    public static string GetGameString(Game game, bool bShorthand) => game switch
    {
        Game.IB1 when bShorthand => "IB1",
        Game.IB2 when bShorthand => "IB2",
        Game.IB3 when bShorthand => "IB3",
        Game.VOTE when bShorthand => "VOTE",

        Game.IB1 => "Infinity Blade I",
        Game.IB2 => "Infinity Blade II",
        Game.IB3 => "Infinity Blade III",
        Game.VOTE => "VOTE!!!",

        _ => "Unknown"
    };

    #endregion

    /// <summary>
    /// Determines whether the specified number is a power of two.
    /// </summary>
    /// <returns>TRUE if the specified number is a power of two, FALSE otherwise.</returns>
    public static bool IsPowerOf2(int value) => (value & (value - 1)) == 0;

    /// <summary>
    /// Formats a count of bytes into a human-readable string. Automatically converts between KB, MB, and GB.
    /// </summary>
    /// <remarks>Appends the unit onto the end of the string, for example: "4.13 KB".</remarks>
    public static string FormatSize(long numBytes)
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
