#define WITH_FOLDER_SUPPORT     // Optional features useful for coalesced editors

using UnrealLib.Enums;

namespace UnrealLib.Coalesced;

// Persistent UnrealStream field was REMOVED. Not needed as all data is read into memory
public class Coalesced
{
    public FileInfo? FileInfo;
    public Game Game;                   // The game specified during initialization
    private Game _realGame = Game.IB1;  // The game which decrypted the file successfully
    public CoalescedSerializerOptions Options = new();
    
    public Dictionary<string, Ini> Inis = new();
    public string ErrorContext = string.Empty;

    public bool GameIsUnspecified => Game is Game.Unknown;

    /// <summary>
    /// Parameterless constructor supports delayed initialization
    /// </summary>
    public Coalesced() { }

    public Coalesced(string path, Game game = Game.Unknown) => Init(path, game);

    public void Init(string path, Game game=Game.Unknown)
    {
        FileInfo = new FileInfo(path);
        Game = game;

        if ((int)FileInfo.Attributes == -1)
        {
            // @ERROR: Passed path does not exist
            ErrorContext = $"Path '{path}' does not exist!";
            return;
        }

        // Read from Coalesced file
        if ((FileInfo.Attributes & FileAttributes.Archive) != 0)
        {
            var ms = new MemoryStream();
            using (var fs = File.OpenRead(path)) fs.CopyTo(ms);
            
            using var unStream = new UnrealStream(ms);
            unStream.IsLoading = true;

            if (TryDecrypt(unStream))
            {
                try
                {
                    unStream.Position = 0;
                    unStream.Serialize(ref Inis);
                }
                catch
                {
                    ErrorContext = $"Failed to parse '{path}'!";
                }
            }
            else ErrorContext = $"Failed to decrypt '{path}'!";

            // If the game decrypted successfully using a key other than what was specified...
            if (!GameIsUnspecified && Game != _realGame) ErrorContext = $"Coalesced file is not {Globals.GameToString(Game)}!";
            Game = _realGame;

            unStream.IsLoading = false;
        }
        
#if WITH_FOLDER_SUPPORT
        // Read from Coalesced folder
        else if ((FileInfo.Attributes & FileAttributes.Directory) != 0)
        {
            Game = TryParseGame(Path.Combine(path, Globals.CoalescedHelperName));
            if (Game is Game.Unknown)
            {
                ErrorContext = $"'{Path.GetFileName(path)}' is not a valid Coalesced folder!";
                return;
            }

            foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file).TrimStart('.');

                // Filter out non-ini files.
                if (!extension.Equals("ini", StringComparison.OrdinalIgnoreCase) && !Globals.Languages.Contains(extension, StringComparer.OrdinalIgnoreCase)) continue;
                Inis.Add($"..\\..{file[path.Length..]}", new Ini(file));
            }
        }
#endif
    }

    public bool Save(string path="")
    {
        if (path.Length != 0) FileInfo = new FileInfo(path);

        string outPath = FileInfo.FullName;

        // Saving to a file
        if (Path.HasExtension(outPath))
        {
            try
            {
                File.Delete(outPath);
            }
            catch
            {
                ErrorContext = $"Failed to overwrite Coalesced file '{outPath}'!";
                return false;
            }

            using var unStream = new UnrealStream(outPath, FileMode.Create);
            unStream.ForceUnicode = Options.ForceUnicodeEncoding;

            unStream.IsSaving = true;

            unStream.Serialize(ref Inis);
            if (Options.SaveEncrypted && Game is not Game.IB1)
            {
                AES.CryptoECB(unStream, Game, false);
            }
        }

#if WITH_FOLDER_SUPPORT
        // Saving to a folder
        else
        {
            try
            {
                if (Directory.Exists(outPath)) Directory.Delete(outPath, true);
            }
            catch
            {
                ErrorContext = $"Failed to overwrite Coalesced folder '{outPath}'!"; ;
                return false;
            }

            foreach (var ini in Inis)
            {
                string iniPath = Path.Combine(outPath, ini.Key[6..]);
                
                Directory.CreateDirectory(Path.GetDirectoryName(iniPath));
                using var sw = new StreamWriter(iniPath);
                
                foreach (var section in ini.Value.Sections)
                {
                    // Write section header
                    sw.WriteLine($"[{section.Key}]");
                
                    // Write section props
                    foreach (var prop in section.Value.Properties)
                    {
                        // Escape newline characters
                        sw.WriteLine(prop.ToString().Replace("\n", "\\n"));
                    }
                }
            }

            string helperPath = Path.Combine(outPath, Globals.CoalescedHelperName);
            if (File.Exists(helperPath)) File.Delete(helperPath);

            using var helper = File.Create(helperPath);
            helper.WriteByte((byte)Game);
            File.SetAttributes(helper.Name, FileAttributes.Hidden);
        }
#endif
        return true;
    }

    private bool TryDecrypt(UnrealStream unStream)
    {
        // Copy the first block of the UnrealStream
        unStream.Position = 0;
        byte[] block = new byte[16];
        unStream.BaseStream.Read(block);

        // If already unencrypted, return true
        if (block[2] == 0 && block[3] == 0) return true;

        // Try decrypt the block using each game's encryption
        for (_realGame = Game.IB2; _realGame <= Game.Vote; _realGame = (Game)((byte)_realGame << 1))
        {
            if (AES.TryDecryptBlock(block, _realGame))
            {
                // If block was successfully decrypted, decrypt the entire stream and return true
                AES.CryptoECB(unStream, _realGame, true);
                return true;   
            }
        }

        // Decryption failed
        _realGame = Game.Unknown;
        return false;
    }
    
    /// <summary>
    /// Attempts to read a Game value from a passed Helper file.
    /// </summary>
    /// <param name="helperFilePath"></param>
    /// <returns></returns>
    private static Game TryParseGame(string helperFilePath)
    {
        if (!File.Exists(helperFilePath)) return Game.Unknown;

        // Parse the helper file
        using var fs = File.OpenRead(helperFilePath);
        
        // @ERROR: Invalid / tampered helper file
        if (fs.Length != 1) return Game.Unknown;

        var game = (Game)fs.ReadByte();
        if (game is not (Game.IB1 or Game.IB2 or Game.IB3 or Game.Vote)) return Game.Unknown;
            
        return game;
    }
}