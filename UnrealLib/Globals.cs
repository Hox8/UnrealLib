using System;
using UnrealLib.Enums;

namespace UnrealLib;

public static class Globals
{
    internal const uint PackageTag = 0x9E2A83C1;
    internal const uint PackageTagSwapped = 0xC1832A9E;

    // Do not change the order of these elements.
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

    /// <summary>
    /// Returns a list of all languages applicable to the passed <see cref="Game"/>.
    /// </summary>
    public static Span<string> GetLanguages(Game game) => game switch
    {
        Game.IB3 => Languages.AsSpan(),
        Game.Vote => Languages.AsSpan(0, 1),
        _ => Languages.AsSpan(0, Languages.Length - 3)
    };

    public static string GetWindowsPath(string path) => path.Replace('/', '\\');
    public static string GetUnixPath(string path) => path.Replace('\\', '/');
}