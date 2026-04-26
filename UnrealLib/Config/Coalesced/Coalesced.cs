using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnrealLib.Config.Crypto;
using UnrealLib.Core;
using static UnrealLib.Core.Globals;

// @TODO: Method documentation. Remember to document exceptions as we aren't try-catching anymore

namespace UnrealLib.Config.Coalesced;

public sealed class Coalesced
{
    private Game GameType;
    private List<IniFile> Inis = [];

    private const string IniPrefix = @"..\..\";

#if UNIX
    private const string HelperFilename = ".ibhelper";
#else
    private const string HelperFilename = "ibhelper";
#endif

    #region Constructors

    // Hide default constructor
    private Coalesced() { }

    public static Coalesced FromFile(string path, int bufferSize = 65536)
    {
        var coal = new Coalesced();

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
        using var temp = new UnrealArchive($"{path}.temp", TempReadWriteStream);

        if (!TryDetermineGameFromFile(fs, out coal.GameType))
        {
            throw new Exception($"Failed to decrypt '{path}'!");
        }

        if (coal.GameType != Game.IB1)
        {
            // Decrypt the coalesced into the temp stream
            AES.EncryptStream(fs, temp.FileStream, coal.GameType, false);

            temp.Position = 0;
        }
        else
        {
            // IB1 coalesces its inis without encryption, so we simply hijack the input filestream
            temp._stream = fs;
        }

        // We can now read in the coalesced data
        temp.Serialize(ref coal.Inis);

        // Ini are serialized with relative paths which we want to strip
        foreach (var ini in coal.Inis.AsSpan())
        {
            if (ini.Path.StartsWith(IniPrefix))
            {
                ini.Path = ini.Path.Substring(IniPrefix.Length);
            }

#if UNIX
            // Convert from NTFS separators to Unix
            ini.Path = ini.Path.Replace('\\', '/');
#endif
        }

        return coal;
    }

    public static Coalesced FromFolder(string path)
    {
        Coalesced coal = new();

        if (!TryDetermineGameFromFolder(path, out coal.GameType))
        {
            // CoalescedException.Throw(CoalescedExceptionType.InvalidFolder, path);
            throw new Exception("Bad Coalesced folder");
        }

        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            // Only serialize files that meet criteria
            if (!IsValidIniFileName(file, coal.GameType))
                continue;

            var ini = IniFile.FromFile(file);

            // Add prefix and slice local filepath
            ini.Path = string.Concat(IniPrefix, file.AsSpan(path.Length + 1));

#if UNIX
            // Coalesced path separators must be NTFS style
            ini.Path = ini.Path.Replace('/', '\\');
#endif

            coal.Inis.Add(ini);
        }

        return coal;
    }

    #endregion

    #region IO

    public void ToFile(string path)
    {
        UDebug.Assert(IsGameValid(GameType));

        string tempPath = $"{path}.temp";

        // Serialize coalesced ini data to a temp file
        using (var Ar = new UnrealArchive(tempPath, FileMode.Create))
        {
            Ar.StartSaving();
            Ar.Serialize(ref Inis);
        }

        if (GameType == Game.IB1)
        {
            // IB1 coalescion does not use encryption, so we can simply move the temp file to its final destination
            File.Move(tempPath, path, true);
        }
        else
        {
            // IB2, IB3, and VOTE expect encrypted data.

            using var dest = File.Create(path);
            using var temp = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.DeleteOnClose);

            // Data must be padded to a multiple of 16 bytes before we perform AES encryption
            int remainder = (int)temp.Length % AES.BlockSize;
            if (remainder != 0)
            {
                temp.SetLength(temp.Length + AES.BlockSize - remainder);
            }

            AES.EncryptStream(temp, dest, GameType, true);
        }
    }

    public void ToFolder(string path)
    {
        // Delete any existing folder recursively
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        // Write out inis
        foreach (var ini in Inis.AsSpan())
        {
            var filePath = Path.Join(path, ini.Path);
            var parentDir = filePath.Substring(0, filePath.LastIndexOf(PathSeparator));

            // Make sure the ini's parent directory exists first
            Directory.CreateDirectory(parentDir);

            ini.ToFile(filePath);
        }

        // Write out GameType so we know how to re-encrypt
        using (var fs = File.Create(Path.Join(path, HelperFilename)))
        {
#if !UNIX
            // Mark file as hidden if we're running on Windows.
            // Not required on Unix as HelperFilename already has the dot prefix
            File.SetAttributes(fs.SafeFileHandle, FileAttributes.Hidden);
#endif

            // Write our GameType byte
            fs.WriteByte((byte)GameType);
        }
    }

    #endregion

    #region Public API

    public bool TryGetIni(string iniPath, [NotNullWhen(true)] out IniFile? outIni)
    {
        foreach (var ini in Inis.AsSpan())
        {
            if (ini.Path.Equals(iniPath, StringComparison.OrdinalIgnoreCase))
            {
                outIni = ini;
                return true;
            }
        }

        outIni = default;
        return false;
    }

    public bool TryRemoveIni(string iniPath)
    {
        if (TryGetIni(iniPath, out var ini))
        {
            Inis.Remove(ini);
            return true;
        }

        return false;
    }

    #endregion

    private static bool TryDetermineGameFromFolder(string folderPath, out Game game)
    {
        var helperPath = Path.Join(folderPath, HelperFilename);

        // Get our GameType
        using (var fs = File.OpenRead(helperPath))
        {
            game = (Game)fs.ReadByte();
        }

        return IsGameValid(game);
    }

    private static bool TryDetermineGameFromFile(Stream stream, out Game game)
    {
        // Read the first block to determine encryption.
        // We cannot use stackalloc here because ICryptoTransform does not accept spans
        var block = new byte[AES.BlockSize];
        stream.ReadExactly(block);

        // Put stream back to where it was
        stream.Position -= AES.BlockSize;

        // IB1 does not encrypt its coalesced ini files
        if (AES.IsBlockUnencrypted(block))
        {
            game = Game.IB1;
            return true;
        }

        // Try to decrypt block using each game's key
        for (game = Game.IB2; game < Game.MAX; game++)
        {
            if (AES.TryDecryptBlock(block, game))
            {
                return true;
            }
        }

        game = Game.MAX;
        return false;
    }

    private static bool IsValidIniFileName(string name, Game game)
    {
        // All extensions we are interested in are four characters long (including '.')
        const int ExtensionLength = 4;

        if (name.Length < ExtensionLength || name[^ExtensionLength] != '.')
        {
            return false;
        }

        // Get extension without period
        string extension = name.Substring(name.Length - (ExtensionLength - 1)).ToUpperInvariant();

        return extension == "INI" || GetLanguages(game).Contains(extension);
    }
}
