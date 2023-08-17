#define WITH_FOLDER_SUPPORT     // Optional features useful for coalesced editors

using UnrealLib.Enums;

namespace UnrealLib.Coalesced;

// Persistent UnrealStream field was REMOVED. Not needed as all data is read into memory
public class Coalesced
{
    public FileInfo? FileInfo;
    public Game Game;
    public bool GameIsUnspecified = true;
    
    /*
     * CoalescedSerializationOptions (or maybe just Ini):
     * AllowComments            // If false, will remove full-line comments
     * AllowCommentsInline      // If false, will remove trailing comments from properties
     * DoPruning                // If true, will remove empty files and sections
     * DoEncryption             // If true, will save encrypted coalesced files where applicable
     * ForceUnicode             // If true, will force unicode strings even if ASCII-compatible
     */
    
    public Dictionary<string, Ini> Inis = new();
    public string ErrorContext = string.Empty;
    
    /// <summary>
    /// Parameterless constructor supports delayed initialization
    /// </summary>
    public Coalesced() { }

    public Coalesced(string path, Game game = Game.Unknown) => Init(path, game);

    public void Init(string path, Game game=Game.Unknown)
    {
        FileInfo = new FileInfo(path);

        if ((int)FileInfo.Attributes == -1)
        {
            // @ERROR: Passed path does not exist
            ErrorContext = $"'{path}' does not exist!";
            return;
        }

        Game = game;
        if (Game is not Game.Unknown) GameIsUnspecified = false;

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

            unStream.IsLoading = false;
        }
        
#if WITH_FOLDER_SUPPORT
        // Read from Coalesced folder
        else if ((FileInfo.Attributes & FileAttributes.Directory) != 0)
        {
            string helperPath = Path.Combine(path, Globals.CoalescedHelperName);
            if (!File.Exists(helperPath))
            {
                // @ERROR: Coalesced helper file not found
                ErrorContext = $"'{Globals.CoalescedHelperName}' file was not found!";
                return;
            }

            // Parse the helper file
            using (var fs = File.OpenRead(helperPath))
            {
                // @ERROR: Invalid / tampered helper file
                if (fs.Length != 1)
                {
                    ErrorContext = $"Invalid '{Globals.CoalescedHelperName}' file!";
                    return;
                }

                Game = (Game)fs.ReadByte();
                if (Game is not (Game.IB1 or Game.IB2 or Game.IB3 or Game.Vote))
                {
                    ErrorContext = $"Invalid '{Globals.CoalescedHelperName}' file!";
                    return;
                }
            }

            foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).Length != 4) continue;
                Inis.Add($"..\\..{file[path.Length..]}", new Ini(file));
            }
        }
#endif
    }

    public void Save(string path="")
    {
        if (path.Length != 0) FileInfo = new FileInfo(path);

        string outPath = FileInfo.FullName;

        // Saving to a file
        if (Path.HasExtension(outPath))
        {
            using var unStream = new UnrealStream(outPath, FileMode.Create);

            unStream.IsSaving = true;

            unStream.Serialize(ref Inis);
            if (Game is not Game.IB1)
            {
                AES.CryptoECB(unStream, Game, false);
            }
        }

#if WITH_FOLDER_SUPPORT
        // Saving to a folder
        else
        {
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
                        sw.WriteLine(prop.ToString());
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
    }

    // Assumes IsLoading is true...
    private static bool IsEncrypted(UnrealStream unStream)
    {
        // This takes advantage of the fact that all coalesced files start with an int32 representing ini count.
        // We can safely assume that the last two bytes of the int32 will always be 0 in a valid coalesced, as
        // they will be unrealistic values - no sane coalesced file will have over 65k ini files.
        unStream.Position = 2;

        byte a = 0;
        byte b = 0;

        unStream.Serialize(ref a);
        unStream.Serialize(ref b);

        return a != 0 || b != 0;
    }

    private bool TryDecrypt(UnrealStream unStream)
    {
        // IB1 is the only game not to use encryption
        if (!IsEncrypted(unStream))
        {
            Game = Game.IB1;
            return true;
        }
        
#if WITH_FOLDER_SUPPORT
        // Test against each game with encryption (IB2, IB3, VOTE)
        if (GameIsUnspecified)
        {
            Game = Game.IB2;
            while (Game <= Game.IB3)
            {
                AES.CryptoECB(unStream, Game, true);
                if (!IsEncrypted(unStream)) return true;
                Game = (Game)((byte)Game << 1);
            }

            Game = Game.Unknown;
        }
#endif
        else
        {
            AES.CryptoECB(unStream, Game, true);
            if (!IsEncrypted(unStream)) return true;
        }

        // Decryption failed
        return false;
    }
}