using UnLib.Enums;
using UnLib.Interfaces;

namespace UnLib.Coalesced;

public class Coalesced : IDisposable, IUnrealStreamable
{
    public Game Game;

    public Dictionary<string, Ini> Inis = new();
    internal UnrealStream UStream;

    public Coalesced(string filePath, Game game)
    {
        FilePath = filePath;
        Game = game;
    }

    /// <summary>
    /// Whether to encrypt the coalesced data using the appropriate game key when saved (IB1 ignores this setting).
    /// </summary>
    public bool DoSaveEncryption { get; set; } = true;

    /// <summary>
    ///  Whether to force Unicode encoding for strings, even when ASCII is sufficient.
    /// </summary>
    public bool ForceUnicode { get; set; } = false;
    public bool InitFailed { get; set; }
    public bool Modified { get; set; } = false;
    public string FilePath { get; set; }
    public Stream BaseStream => UStream.BaseStream;
    public int Length => UStream.Length;

    public void Write(byte[] value)
    {
        UStream.Write(value);
    }

    public void Init()
    {
        UStream = new UnrealStream(FilePath);
        UStream.IsLoading = true;

        if (TryDecrypt()) Serialize(UStream);
        else InitFailed = true;

        UStream.IsLoading = false;
    }

    /// <summary>
    /// Re-serializes, encrypts, and saves the coalesced back to its original filepath.
    /// </summary>
    public void Save()
    {
        if (InitFailed) return;

        UStream.ForceUnicode = ForceUnicode;
        UStream.SetLength(0);

        UStream.IsSaving = true;
        Serialize(UStream);

        if (DoSaveEncryption && Game != Game.IB1) AES.CryptoECB(UStream, Game, false);
        UStream.IsSaving = false;

        BaseStream.Position = 0;

        // UStream.Close();
        // Dispose();
    }

    /// <summary>
    /// If coalesced stream is encrypted, try and decrypt it before deserialization.
    /// </summary>
    /// <returns>True if resulting ini stream is unencrypted (ready for use), false otherwise.</returns>
    private bool TryDecrypt()
    {
        if (!IsEncrypted()) return true;

        if (Game is Game.IB1) return false;
        AES.CryptoECB(UStream, Game, true);

        return !IsEncrypted();
    }

    // @TODO: A static serializer would work well here. Probably something to think about moving forward.
    /// <summary>
    /// Used to split and save the coalesced to PC-equivalent ini files on disk. STUB.
    /// </summary>
    /// <param name="outDir">The output folder path to save the ini files to.</param>
    public void SaveToFolder(string outDir)
    {
    }

    public void Serialize(UnrealStream UStream)
    {
        UStream.Position = 0;
        UStream.Serialize(ref Inis);
    }

    #region Helpers

    /// <summary>
    /// Determines whether a coalesced streams in encrypted or not.
    /// </summary>
    /// <returns>True if the coalesced stream is encrypted, and false if it is not.</returns>
    private bool IsEncrypted()
    {
        // This takes advantage of the fact that all coalesced files start with an int32 representing ini count.
        // We can safely assume that the last two bytes of the int32 will always be 0 in a valid coalesced, as
        // they will be unrealistic values - no sane coalesced file will have over 65k ini files.
        UStream.Position = 2;

        byte a = 0;
        byte b = 0;

        UStream.Serialize(ref a);
        UStream.Serialize(ref b);

        return a != 0 || b != 0;
    }

    #endregion

    public void Dispose()
    {
        UStream.Dispose();
        GC.SuppressFinalize(this);
    }
}