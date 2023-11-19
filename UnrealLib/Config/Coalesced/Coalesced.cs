using System;
using System.Collections.Generic;
using System.IO;
using UnrealLib.Enums;

namespace UnrealLib.Config.Coalesced;

public class Coalesced : UnrealArchive
{
    private Game Game;
    private Game DecryptedWith = Game.Unknown;
    public CoalescedOptions Options = new();

    private List<Ini> Inis = new();

    /// <summary>
    /// Name of the file that Coalesced files use when exporting folders. Is used to re-save folders to Coalesced files using original encryption.
    /// </summary>
    private const string CoalescedHelperName = "ibhelper";

    /// <summary>
    /// HashSet used when saving Coalesced folders back into Coalesced files. Filters out unrelated files.
    /// </summary>
    private static readonly HashSet<string> ExtensionFilter = new(StringComparer.OrdinalIgnoreCase)
    {
        "INI", "INT", "BRA", "CHN", "DEU", "DUT", "ESN", "FRA", "ITA",
        "JPN", "KOR", "POR", "RUS", "SWE", "ESM", "IND", "THA"
    };
    
    public Coalesced(string path, Game game = Game.Unknown, bool delayInit = false)
    {
        InitPathInfo(path);
        Game = game;

        if (!delayInit)
        {
            Open();
        }
    }
    
    public sealed override bool Open(string? path = null)
    {
        // If optional path was provided, overwrite existing paths
        if (path is not null) InitPathInfo(path);
        
        if (string.IsNullOrEmpty(Filename))
        {
            ErrorContext = "Path is invalid!";
            return false;
        }

        return Init();
    }

    public override bool Save(string? path = null)
    {
        path ??= QualifiedPath;
        
        // Saving Coalesced file
        if (Path.HasExtension(path))
        {
            UnrealStream stream;
            
            try
            {
                stream = new UnrealStream(path, FileMode.Create);
                stream.StartSaving();
            }
            catch
            {
                ErrorContext = $"Failed to overwrite Coalesced file '{path}'!";
                return false;
            }
            
            stream.ForceUTF16 = Options.ForceUnicode;
            var options = new IniOptions(Options);

            // Sanitize Ini paths. Needs to be Windows-style, and have the relative segment
            foreach (var ini in Inis)
            {
                // Copy Coalesced options to Ini (is currently ignored when saving binary inis)
                ini.Options = options;
#if UNIX
                ini.Path = Globals.GetWindowsPath(ini.Path);
#endif
                if (!ini.Path.StartsWith(@"..\..\")) ini.Path = @"..\..\" + ini.Path;
            }
            
            stream.Serialize(ref Inis);
            
            // Encrypt Coalesced contents
            if (Options.DoSaveEncryption && Game is not Game.IB1)
            { 
                AES.CryptoECB(stream, Game, false);
            }
            
            stream.Flush();
        }
#if WITH_COALESCED_FOLDER_SUPPORT
        // Saving Coalesced folder
        else
        {
            try
            {
                // Try recurse delete existing folder
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch
            {
                ErrorContext = $"Failed to overwrite Coalesced folder '{path}'!";
                return false;
            }
            
            // Construct a new IniOptions instance to override and use Coalesced options
            var iniOptions = new IniOptions(Options);
            
            foreach (var ini in Inis)
            {
                // Strip the relative segment from the ini path— we do not want that for folder output.
                string iniPath = Path.Combine(path, ini.Path);
                string folderPath = Path.GetDirectoryName(iniPath);

                // Create folder if it doesn't already exist
                Directory.CreateDirectory(folderPath);
                
                ini.Save(iniPath, iniOptions);
            }
            
            // Create helper file so we know what key to use when re-encrypting
            using var helper = File.Create(Path.Combine(path, CoalescedHelperName));
            helper.WriteByte((byte)Game);
            File.SetAttributes(helper.Name, FileAttributes.Hidden);
        }
#endif
        
        Dispose();
        return true;
    }

    public override bool Init()
    {
        var attributes = new FileInfo(QualifiedPath).Attributes;

        if ((int)attributes == -1)
        {
            ErrorContext = "Path does not exist!";
            return false;
        }

#if WITH_COALESCED_FOLDER_SUPPORT
        // Open directory
        if ((attributes & FileAttributes.Directory) != 0)
        {
            Game = ParseGameFromHelper(Path.Combine(QualifiedPath, CoalescedHelperName));

            if (Game is Game.Unknown)
            {
                ErrorContext = "Invalid Coalesced folder!";
                return false;
            }

            foreach (string file in Directory.GetFiles(QualifiedPath, "*.*", SearchOption.AllDirectories))
            {
                // Filter out non-ini extensions (which are all length 3)
                if (!ExtensionFilter.Contains(file[^3..])) continue;

                if (TryAddIni(file[(QualifiedPath.Length + 1)..], out var ini))
                {
                    ini.Open(file);
                    Inis.Add(ini);
                }
            }
        }
        
        // Open file
        else
#endif
        {
            // Make a copy so we don't alter the original file
            using UnrealStream stream = new(File.ReadAllBytes(QualifiedPath));

            if (!TryDecrypt(stream))
            {
                ErrorContext = "Failed to decrypt Coalesced file!";
                return false;
            }

            try
            {
                stream.Position = 0;
                stream.Serialize(ref Inis);
            }
            catch
            {
                ErrorContext = "Failed to parse Coalesced file!";
                return false;
            }

            // Remove the relative segment from the start of each ini path
            foreach (var ini in Inis)
            {
                if (ini.Path.StartsWith(@"..\..\")) ini.Path = ini.Path[6..];
            }

            // If we were expecting one game, but got something else...
            if (Game is not Game.Unknown && DecryptedWith != Game)
            {
                ErrorContext = $"Was expecting {Game} Coalesced file, but got {DecryptedWith} instead!";
                return false;
            }
            
            // Otherwise, set Game to DecryptedWith so we know what encryption key to use later.
            Game = DecryptedWith;
        }

        State = UnrealArchiveState.Loaded;
        return true;
    }

    // Dispose() generally indicates the class is no longer being used, but we're setting vars as if we are?
    // @TODO Use a Clear() etc method instead
    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Inis.Clear();
        State = UnrealArchiveState.Unloaded;
        
        GC.SuppressFinalize(this);
    }

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
            DecryptedWith = Game.IB1;
            return true;
        }

        // Start from IB2 and iterate each game up to / including VOTE.
        for (DecryptedWith = Game.IB2; DecryptedWith <= Game.Vote; DecryptedWith = (Game)((byte)DecryptedWith << 1))
        {
            // If the compressed block successfully decrypted, decrypt the whole stream.
            if (AES.TryDecryptBlock(block, DecryptedWith))
            {
                AES.CryptoECB(stream, DecryptedWith, true);
                return true;
            }
        }

        // None of the AES keys could decrypt the initial block - decryption failed.
        DecryptedWith = Game.Unknown;
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
        for (int i = 0; i < Inis.Count; i++)
        {
            if (Inis[i].Path.Equals(iniName, StringComparison.OrdinalIgnoreCase))
            {
                Inis.RemoveAt(i);
                return true;
            }
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