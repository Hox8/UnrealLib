using System;
using System.Collections.Generic;
using System.IO;
using UnrealLib.Enums;

namespace UnrealLib.Config.Coalesced;

// Coalesced class works exclusively with MemoryStream buffers due to encryption

public class Coalesced : UnrealArchive
{
    private Game _decryptedWith = Game.Unknown;
    public List<Ini> Inis = new();

    public CoalescedOptions Options = new();
    private const string HelperName = "ibhelper";

    // MemoryStream must be resizable (ECB padding, addition of Inis/Sections/Properties)
    public Coalesced(string filePath, Game game = Game.Unknown) : base()
    {
        InitFileinfo(filePath, true);

        var memoryStream = new MemoryStream();
        SetStream(memoryStream);

        if (!PathIsDirectory)
        {
            memoryStream.Write(File.ReadAllBytes(filePath));
            InitialLength = Length;
        }

        Game = game;

        Load();
    }

    public override void Load()
    {
        if (HasError) return;

        // Read in Coalesced file
        if (!PathIsDirectory)
        {
            if (!TryDecrypt())
            {
                Error = ArchiveError.FailedDecrypt;
                return;
            }

            try
            {
                Position = 0;
                Serialize(ref Inis);
            }
            catch
            {
                Error = ArchiveError.FailedParse;
                return;
            }

            // Error if the Coalesced file belongs to a game we weren't expecting
            if (Options.DoSaveEncryption && Game is not Game.Unknown && Game != _decryptedWith)
            {
                Error = ArchiveError.UnexpectedGame;
                return;
            }

            Game = _decryptedWith;
        }
#if WITH_COALESCED_EDITOR
        // Read in Coalesced folder
        else
        {
            // Locate the helper file so we know how to encrypt the files
            if (!ParseHelperFile(Path.Combine(QualifiedPath, HelperName)))
            {
                Error = ArchiveError.InvalidCoalescedFolder;
                return;
            }

            foreach (var entry in Directory.GetFiles(QualifiedPath, "*.*", SearchOption.AllDirectories))
            {
                if (!Path.HasExtension(entry)) continue;
                string extension = Path.GetExtension(entry)[1..].ToUpperInvariant();

                // Filter out unsupported extensions
                if (!extension.Equals("INI") && Globals.GetLanguages(Game).IndexOf(extension) == -1) continue;

                var ini = new Ini(entry, true);
                ini.Path = "..\\..\\" + ini.Path[(FileInfo.FullName.Length + 1)..].Replace('/', '\\');
                Inis.Add(ini);
            }
        }
#endif
    }

    public override long Save()
    {
        if (HasError) return -1;

        ForceUTF16 = Options.ForceUnicode;

        // Save Coalesced file
        if (!PathIsDirectory)
        {
            Position = 0;

            StartSaving();
            Serialize(ref Inis);

            // Since we're reusing the original buffer, we need to set the final length in case we're smaller.
            SetLength(Position);

            if (Game is not Game.IB1)
            {
                AES.CryptoECB(this, Game, false);
            }

            return base.Save();
        }
#if WITH_COALESCED_EDITOR
        // Save Coalesced folder
        else
        {
            try
            {
                if (Directory.Exists(QualifiedPath)) Directory.Delete(QualifiedPath, true);
                Directory.CreateDirectory(QualifiedPath);
            }
            catch
            {
                Error = ArchiveError.FailedDelete;
                return -1;
            }

            var iniOptions = new IniOptions(Options);
            foreach (var ini in Inis)
            {
                // Strip relative segment and replace separator chars in favor of Unix systems
                string outputPath = Path.Combine(QualifiedPath, ini.Path[6..].Replace('\\', '/'));
                string outputDir = Path.GetDirectoryName(outputPath);

                Directory.CreateDirectory(outputDir);
                ini.Save(outputPath, iniOptions);
            }

            // Write Game to disk so we know how to re-encrypt the folder later
            string helperPath = Path.Combine(Path.ChangeExtension(QualifiedPath, null), HelperName);
            File.WriteAllBytes(helperPath, [(byte)_decryptedWith]);
            File.SetAttributes(helperPath, FileAttributes.Hidden);

            return 0;
        }
#else
        Error = ArchiveError.FailedSave;
        return -1;
#endif
    }

    private bool TryDecrypt()
    {
        // Allocate enough memory so we can copy the first compressed block into memory.
        // We'll test compression keys against it rather than the entire stream.
        Span<byte> block = stackalloc byte[16];

        Position = 0;
        ReadExactly(block);

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
                AES.CryptoECB(this, _decryptedWith, true);
                return true;
            }
        }

        // None of the AES keys could decrypt the initial block - decryption failed.
        _decryptedWith = Game.Unknown;
        return false;
    }

    #region Helpers

#if WITH_COALESCED_EDITOR
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
#endif

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