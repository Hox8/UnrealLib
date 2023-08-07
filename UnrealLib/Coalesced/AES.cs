using System.Security.Cryptography;
using System.Text;
using UnLib.Enums;

namespace UnLib.Coalesced;

public static class AES
{
    private static string GetGameKey(Game game) => game switch
    {
        Game.IB2 => Globals.CoalescedKeyIB2,
        Game.IB3 => Globals.CoalescedKeyIB3,
        Game.Vote => Globals.CoalescedKeyVOTE
    };

    /// <summary>
    /// Takes an UnrealStream as input and attempts to un/encrypt the contents. This modifies the UnrealStream!
    /// </summary>
    /// <param name="unStream">The UnrealStream containing the coalesced data.</param>
    /// <param name="game">The game of the corresponding coalesced data. Used for encryption.</param>
    /// <param name="isDecrypting">A boolean determining whether to decrypt or encrypt data.</param>
    public static void CryptoECB(UnrealStream unStream, Game game, bool isDecrypting)
    {
        // Set up AES properties
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Key = Encoding.ASCII.GetBytes(GetGameKey(game));
        aes.Padding = PaddingMode.Zeros;

        // If stream isn't a valid ECB block size (multiple of 16), pad its length to the next multiple
        var remainder = unStream.Length % 16;
        if (remainder != 0) unStream.SetLength(unStream.Length + 16 - remainder);

        // Do de/encryption
        unStream.Position = 0;
        using var crypto = isDecrypting ? aes.CreateDecryptor() : aes.CreateEncryptor();
        var result = crypto.TransformFinalBlock(unStream.ToArray(), 0, unStream.Length);

        // Save results back to stream
        unStream.SetLength(0);
        unStream.Write(result, 0, result.Length);
    }
}