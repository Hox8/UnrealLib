using System;
using System.Security.Cryptography;
using UnrealLib.Enums;

namespace UnrealLib.Config.Coalesced;

public static class AES
{
    private static readonly Aes Aes;

    static AES()
    {
        Aes = Aes.Create();
        Aes.Mode = CipherMode.ECB;
        Aes.Padding = PaddingMode.Zeros;
    }

    private static byte[] GetGameKey(Game game) => game switch
    {
        Game.IB2 => "|FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B"u8.ToArray(),
        Game.IB3 => "6nHmjd:hbWNf=9|UO2:?;K0y+gZL-jP5"u8.ToArray(),
        Game.Vote => "DKksEKHkldF#(WDJ#FMS7jla5f(@J12|"u8.ToArray()
    };

    /// <summary>
    /// Takes an UnrealStream as input and attempts to un/encrypt the contents. This modifies the UnrealStream!
    /// </summary>
    /// <param name="stream">The UnrealStream containing the coalesced data.</param>
    /// <param name="game">The game of the corresponding coalesced data. Used for encryption.</param>
    /// <param name="isDecrypting">A boolean determining whether to decrypt or encrypt data.</param>
    public static void CryptoECB(UnrealStream stream, Game game, bool isDecrypting)
    {
        // If stream isn't a valid ECB block size (multiple of 16), pad its length to the next multiple
        int remainder = stream.Length % 16;
        if (remainder != 0) stream.Length = stream.Length + 16 - remainder;

        // Create Crypto transformer
        using var crypto = isDecrypting
            ? Aes.CreateDecryptor(GetGameKey(game), null)
            : Aes.CreateEncryptor(GetGameKey(game), null);

        // Do Crypto
        stream.Position = 0;
        var result = crypto.TransformFinalBlock(stream.ToArray(), 0, stream.Length);

        // Write result back to stream
        stream.Position = 0;
        stream.Write(result);
        stream.Length = stream.Position;
    }

    /// <summary>
    /// Tries to decrypt a 128-bit block of memory using the passed game's key.
    /// </summary>
    /// <returns>True if the block was successfully decrypted.</returns>
    public static bool TryDecryptBlock(Span<byte> block, Game game)
    {
        // Decrypt block using game key
        block = Aes.CreateDecryptor(GetGameKey(game), null).TransformFinalBlock(block.ToArray(), 0, 16);

        // Return whether block decrypted successfully
        return BlockIsUnencrypted(block);
    }

    public static bool BlockIsUnencrypted(Span<byte> block) => block[2] == 0 && block[3] == 0;
}