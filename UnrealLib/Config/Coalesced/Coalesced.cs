using System;
using System.Collections.Generic;
using System.IO;
using UnrealLib.Enums;

namespace UnrealLib.Config.Coalesced;

public class Coalesced : UnrealArchive
{
    public Game Game = Game.Unknown;
    private Game _decryptedWith = Game.Unknown;
    public List<Ini> Inis = new();

    public CoalescedOptions Options = new();
    private const string HelperName = "ibhelper";

    public Coalesced(string path, Game game = Game.Unknown, bool delayInitialization = false) : base(path)
    {
        Game = game;

        if (!delayInitialization)
        {
            Load();
        }
    }

    public override bool Load()
    {
        if (HasError) return false;

        // Load Coalesced file
        if (!PathIsDirectory)
        {
            try
            {
                using var stream = File.OpenRead(QualifiedPath);
            }
            catch
            {
                SetError(UnrealArchiveError.PathNotReadable);
                return false;
            }

            using (var stream = new UnrealStream(File.ReadAllBytes(QualifiedPath)))
            {
                if (!TryDecrypt(stream))
                {
                    SetError(UnrealArchiveError.DecryptionFailed);
                    return false;
                }

                try
                {
                    stream.Position = 0;
                    stream.Serialize(ref Inis);
                }
                catch
                {
                    SetError(UnrealArchiveError.ParseFailed);
                    return false;
                }

#if !WITH_COALESCED_EDITOR
                // Error if the Coalesced file belongs to a different game
                if (Game is not Game.Unknown && Game != _decryptedWith)
                {
                    SetError(UnrealArchiveError.UnexpectedGame);
                    return false;
                }
#endif
                Game = _decryptedWith;
            }
        }
#if WITH_COALESCED_EDITOR
        // Load Coalesced folder
        else
        {
            // Try read game from helper file within folder
            if ((Game = ParseGameFromHelper(Path.Combine(QualifiedPath, HelperName))) == Game.Unknown)
            {
                SetError(UnrealArchiveError.InvalidFolder);
                return false;
            }

            foreach (var entry in Directory.GetFiles(QualifiedPath, "*.*", SearchOption.AllDirectories))
            {
                if (!Path.HasExtension(entry)) continue;
                string extension = Path.GetExtension(entry)[1..].ToUpperInvariant();

                // Filter out unsupported extensions
                if (!extension.Equals("INI") && Globals.GetLanguages(Game).IndexOf(extension) == -1) continue;

                var ini = new Ini(entry, true);
                ini.Path = "..\\..\\" + ini.Path[(_fileInfo.FullName.Length + 1)..].Replace('/', '\\');
                Inis.Add(ini);
            }
        }
#endif

        return true;
    }

    public override bool Save(string? path = null)
    {
        if (HasError) return false;

        var fileInfo = new FileInfo(path ?? QualifiedPath);

        // Save Coalesced file
        if (fileInfo.Extension != "")
        {
            // Create an UnrealStream with MemoryStream backing
            using var stream = new UnrealStream([], true);
            stream.ForceUTF16 = Options.ForceUnicode;

            stream.StartSaving();
            stream.Serialize(ref Inis);

            if (Options.DoSaveEncryption && Game is not Game.IB1)
            {
                AES.CryptoECB(stream, Game, false);
            }

            File.WriteAllBytes(fileInfo.FullName, stream.ToArray());
        }
#if WITH_COALESCED_EDITOR
        // Save Coalesced folder
        else
        {
            string folderPath = Path.ChangeExtension(_fileInfo.FullName, null);

            try
            {
                if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
                Directory.CreateDirectory(folderPath);
            }
            catch
            {
                SetError(UnrealArchiveError.FailedOverwrite);
                return false;
            }

            var iniOptions = new IniOptions(Options);
            foreach (var ini in Inis)
            {
                // Strip relative segment and replace separator chars in favor of Unix systems
                string outputPath = Path.Combine(folderPath, ini.Path[6..].Replace('\\', '/'));
                string outputDir = Path.GetDirectoryName(outputPath);

                Directory.CreateDirectory(outputDir);
                ini.Save(outputPath, iniOptions);
            }

            // Write Game to disk so we know how to re-encrypt the folder later
            string helperPath = Path.Combine(Path.ChangeExtension(QualifiedPath, null), HelperName);
            File.WriteAllBytes(helperPath, [(byte)_decryptedWith]);
            File.SetAttributes(helperPath, FileAttributes.Hidden);
        }
#endif

        return true;
    }

    #region Helpers

    private bool TryDecrypt(UnrealStream stream)
    {
        // Allocate enough memory so we can copy the first compressed block into memory.
        // We'll test compression keys against it rather than the entire stream.
        Span<byte> block = stackalloc byte[16];

        stream.Position = 0;
        stream.ReadExactly(block);

        // If we're already unencrypted, then we must have an IB1 coalesced file
        if (AES.BlockIsUnencrypted(block))
        {
            _decryptedWith = Game.IB1;
            return true;
        }

        // Start from IB2 and iterate each game up to / including VOTE.
        for (_decryptedWith = Game.IB2; _decryptedWith <= Game.Vote; _decryptedWith = (Game)((byte)_decryptedWith << 1))
        {
            // If the compressed block successfully decrypted, decrypt the whole stream.
            if (AES.TryDecryptBlock(block, _decryptedWith))
            {
                AES.CryptoECB(stream, _decryptedWith, true);
                return true;
            }
        }

        // None of the AES keys could decrypt the initial block - decryption failed.
        _decryptedWith = Game.Unknown;
        return false;
    }

    private static Game ParseGameFromHelper(string path)
    {
        Game game = Game.Unknown;

        if (File.Exists(path))
        {
            using var fs = File.OpenRead(path);

            game = (Game)fs.ReadByte();
            if (game is not (Game.IB1 or Game.IB2 or Game.IB3 or Game.Vote)) game = Game.Unknown;
        }

        return game;
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