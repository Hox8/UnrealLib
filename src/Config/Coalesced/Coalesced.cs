using System;
using System.Collections.Generic;
using System.IO;
using UnrealLib.Enums;

namespace UnrealLib.Config.Coalesced;

public class Coalesced : UnrealArchive
{
    private Game Game;
    private List<Ini> Inis;
    public CoalescedOptions Options = new();

    private const string HelperFileName = "ibhelper";

    #region Constructors

    private Coalesced(string filePath, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, bool makeCopy = false) : base(filePath, mode, access, makeCopy: makeCopy) { }

    public static Coalesced FromFile(string filePath, Game game = Game.Unknown, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, bool makeCopy = false)
    {
        var coal = new Coalesced(filePath, mode, access, makeCopy);

        // If the base ctor enocuntered any issues, return early
        if (coal.HasError) return coal;

        try
        {
            if (!coal.TryDecrypt())
            {
                // Failed to decrypt the file
                coal.SetError(ArchiveError.FailedDecrypt);
            }
            else if (game != Game.Unknown && coal.Game != game)
            {
                // The Coalesced belongs to a game we weren't expecting
                coal.SetError(ArchiveError.UnexpectedGame);
            }
            else
            {
                coal.Position = 0;
                coal.Serialize(ref coal.Inis);
            }
        }
        catch
        {
            // Other errors just chalk up to FailedParse (invalid Coalesced file)
            coal.SetError(ArchiveError.FailedParse, null);
        }

        // We've read all we need from the stream, so dispose of it here
        coal._buffer.Dispose();

        return coal;
    }

    public static Coalesced FromFolder(string filePath, Game game = Game.Unknown, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, bool makeCopy = false)
    {
        var coal = new Coalesced(filePath, mode, access, makeCopy);

        // If the base ctor enocuntered any issues, return early
        if (coal.HasError) return coal;

        if (!coal.IsDirectory)
        {
            coal.SetError(ArchiveError.RequiresFolder);
            return coal;
        }

        // Locate the helper file so we know how to encrypt the files
        if (!coal.ParseHelperFile(Path.Combine(coal.FullName, HelperFileName)))
        {
            coal.SetError(ArchiveError.InvalidCoalescedFolder, null);
            return coal;
        }

        coal.Inis = [];
        var languages = Globals.GetLanguages(coal.Game);
        foreach (var file in Directory.EnumerateFiles(filePath, "*.*", SearchOption.AllDirectories))
        {
            // Filter out files lacking a 3-character file extension
            if (file.Length < 4 || file[^4] != '.') continue;

            // Filter out files lacking the right extensions
            string extension = file[^3..].ToUpperInvariant();
            if (extension != "INI" && languages.IndexOf(extension) == -1) continue;

            // Read file as an ini and add it to the list.
            // Add relative path bit at the front, since UE3 expects it to be there
            var ini = Ini.FromFile(file);
            ini.Path = "..\\..\\" + ini.Path[(coal.FullName.Length + 1)..].Replace('/', '\\');
            coal.Inis.Add(ini);
        }

        return coal;
    }

    #endregion

    public override long SaveToFile(string? path = null)
    {
        if (HasError) return -1;

        path ??= FullName;

        ForceUTF16 = Options.ForceUnicode;
        bool saveEncrypted = Options.DoSaveEncryption && Game is not Game.IB1;
        long bytesWritten;

        using (_buffer = File.Create(path, 131072, FileOptions.SequentialScan))
        {
            StartSaving();
            Serialize(ref Inis);

            if (saveEncrypted)
            {
                // AES requires length to be a multiple of 16. Pad it here if we fall short
                int remainder = (int)Length % 16;
                if (remainder != 0)
                {
                    Length += 16 - remainder;
                }
            }

            bytesWritten = Length;
        }

        if (saveEncrypted)
        {
            string tempPath = $"{path}.temp";

            using (var unencryptedStream = File.OpenRead(path))
            {
                using (var encryptedStream = File.Create(tempPath, 131072, FileOptions.SequentialScan))
                {
                    AES.EncryptStream(unencryptedStream, encryptedStream, Game, true);
                    bytesWritten = encryptedStream.Length;
                }
            }

            File.Move(tempPath, path, true);
        }

        return bytesWritten;
    }

    public long SaveToFolder(string path)
    {
        if (HasError) return -1;

        long bytesWritten = 0;

        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }
        catch
        {
            SetError(ArchiveError.FailedDelete, path);
            return -1;
        }

        var iniOptions = new IniOptions(Options);
        foreach (var ini in Inis)
        {
            // Copy Coalesced options to Ini
            ini.Options = iniOptions;

            // Strip relative segment (6 chars) and replace separator chars in favor of Unix systems.
            // Windows is OK with mixing separator chars!
            string outPath = Path.Combine(path, ini.Path[6..].Replace('\\', '/'));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

            bytesWritten += ini.SaveToFile(outPath);

            if (ini.ErrorType is IniError.FailedWrite)
            {
                SetError(ArchiveError.FailedWrite, outPath);
                return -1;
            }
        }

        // Write Game to disk so we know how to re-encrypt the folder later
        string helperPath = Path.Combine(path, HelperFileName);

        File.WriteAllBytes(helperPath, [(byte)Game]);
        File.SetAttributes(helperPath, FileAttributes.Hidden);

        return bytesWritten;
    }

    #region Helpers

    public bool TryDecrypt()
    {
        byte[] firstBlock = new byte[16];

        ReadExactly(firstBlock);

        if (AES.BlockIsUnencrypted(firstBlock))
        {
            Game = Game.IB1;
            return true;
        }

        // Start from IB2 and iterate each game up to / including VOTE.
        for (Game = Game.IB2; Game <= Game.Vote; Game = (Game)((byte)Game << 1))
        {
            if (!AES.TryDecryptBlock(firstBlock, Game)) continue;

            var options = new FileStreamOptions() { Access = FileAccess.ReadWrite, Mode = FileMode.Create, Options = FileOptions.DeleteOnClose };
            var decryptedStream = File.Open($"{DirectoryName}/{Path.GetRandomFileName()}", options);

            _buffer.Position = 0;
            AES.EncryptStream(_buffer, decryptedStream, Game, false);

            _buffer.Dispose();
            _buffer = decryptedStream;

            return true;
        }

        return false;
    }

    private bool ParseHelperFile(string path)
    {
        if (File.Exists(path))
        {
            using var fs = File.OpenRead(path);

            Game = (Game)fs.ReadByte();
            if (Game is not (Game.IB1 or Game.IB2 or Game.IB3 or Game.Vote))
            {
                Game = Game.Unknown;
                return false;
            }
        }

        return true;
    }

    public bool TryGetIni(string iniName, out Ini result)
    {
        foreach (var ini in Inis)
        {
            if (ini.Path.Equals(iniName, StringComparison.OrdinalIgnoreCase))
            {
                result = ini;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool TryAddIni(string iniName, out Ini result)
    {
        if (TryGetIni(iniName, out result)) return false;

        result = new Ini { Path = iniName };
        return true;
    }

    public bool TryRemoveIni(string iniName)
    {
        if (TryGetIni(iniName, out Ini ini))
        {
            Inis.Remove(ini);
            return true;
        }

        return false;
    }

    #endregion
}

public sealed class CoalescedOptions
{
    /// <summary>
    /// Whether to serialize global (sectionless) Ini properties and comments. If false, these will be omitted during serialization.
    /// </summary>
    public bool KeepGlobals { get; set; } = false;
    /// <summary>
    /// Whether to serialize Ini comments. If not set, all comments (';' '#') will be omitted during serialization.
    /// </summary>
    public bool KeepComments { get; set; } = true;
    /// <summary>
    /// Whether to keep empty sections when serializing ini files. If set to false, section headers without any properties will be removed.
    /// </summary>
    public bool KeepEmptySections { get; set; } = false;
    /// <summary>
    /// Whether to force Unicode encoding. If set, strings will be encoded in Unicode even if they're ASCII-compatible.
    /// </summary>
    public bool ForceUnicode { get; set; } = false;
    /// <summary>
    /// Whether to encrypt the Coalesced file on save. If set, the Coalesced file will use its game's encryption key if applicable.
    /// </summary>
    public bool DoSaveEncryption { get; set; } = true;
}
