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

    /// <summary>
    /// Returns the AES key for each game, used for either Coalesced or save file encryption.
    /// </summary>
    /// <param name="game">The game to get the corresponding key for.</param>
    /// <param name="saveGame">Whether to return the key used to encrypt save games, if applicable.</param>
    private static byte[] GetGameKey(Game game/*, bool saveGame = false*/) => game switch
    {
        Game.IB3 => "6nHmjd:hbWNf=9|UO2:?;K0y+gZL-jP5"u8.ToArray(),
        Game.IB2 => "|FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B"u8.ToArray(),         // Shared between Coalesced and save file
        // Game.IB1 when saveGame => throw new NotImplementedException("Haven't gotten around to this yet!"),
        Game.Vote => "DKksEKHkldF#(WDJ#FMS7jla5f(@J12|"u8.ToArray()         // @TODO verify whether shared between Coalesced and save file
    };

    /// <summary>
    /// Takes an UnrealArchive as input and attempts to un/encrypt the contents. Not pure.
    /// </summary>
    /// <param name="Ar">The UnrealArchive containing the coalesced data.</param>
    /// <param name="game">The Game of the corresponding coalesced data. Used to determine encryption.</param>
    /// <param name="isDecrypting">A boolean determining whether to decrypt or encrypt data.</param>
    public static void CryptoECB(UnrealArchive Ar, Game game, bool isDecrypting)
    {
        // If stream isn't a valid ECB block size (multiple of 16), pad its length to the next multiple
        int remainder = (int)Ar.Length % 16;
        if (remainder != 0)
        {
            Ar.SetLength(Ar.Length + 16 - remainder);
        }

        // Create Crypto transformer
        using var crypto = isDecrypting
            ? Aes.CreateDecryptor(GetGameKey(game), null)
            : Aes.CreateEncryptor(GetGameKey(game), null);

        // Do Crypto
        var result = crypto.TransformFinalBlock(Ar.GetBufferRaw(), 0, (int)Ar.Length);

        // Write result back to stream
        Ar.Position = 0;
        Ar.Write(result);
        Ar.SetLength(Ar.Position);
    }

    /// <summary>
    /// Attempts to decrypt a 128-bit block of memory using the specified game key.
    /// </summary>
    /// <returns>True if the block was successfully decrypted, False if not.</returns>
    public static bool TryDecryptBlock(Span<byte> block, Game game)
    {
        // Decrypt block using game key
        block = Aes.CreateDecryptor(GetGameKey(game), null).TransformFinalBlock(block.ToArray(), 0, 16);

        return BlockIsUnencrypted(block);
    }

    /// <summary>
    /// Tests whether the first compressed block of a Coalesced file is encrypted.
    /// </summary>
    /// <remarks>
    /// This takes advantage of the fact that the first four bytes in the block represent the int32 number of ini files.
    /// <br/>The upper two bytes will always be 0 for reasonable / valid Coalesced files.
    /// </remarks>
    /// <returns>True if the block is unencrypted, False if not.</returns>
    public static bool BlockIsUnencrypted(Span<byte> block) => block[2] == 0 && block[3] == 0;
}