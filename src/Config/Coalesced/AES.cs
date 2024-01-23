using System;
using System.IO;
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
        Game.Vote => "DKksEKHkldF#(WDJ#FMS7jla5f(@J12|"u8.ToArray()         // @TODO verify whether shared between Coalesced and save file
    };

    /// <summary>
    /// Performs encryption on inputStream, writing the results to outputStream.
    /// </summary>
    /// <param name="inputStream">The input stream containing the data to perform encryption on.</param>
    /// <param name="outputStream">The output stream reciving the transformed bytes.</param>
    /// <param name="game">The game to perform encryption for. Each game has its own AES key.</param>
    /// <param name="doEncryption">If true, encrypts the contents in inputStream. If false, decrypts the contents instead.</param>
    public static void EncryptStream(Stream inputStream, Stream outputStream, Game game, bool doEncryption)
    {
        byte[] key = GetGameKey(game);

        var transformer = doEncryption
            ? Aes.CreateEncryptor(key, null)
            : Aes.CreateDecryptor(key, null);

        byte[] buffer = new byte[16384];

        int bytesRead;
        while ((bytesRead = inputStream.Read(buffer)) != 0)
        {
            transformer.TransformBlock(buffer, 0, bytesRead, buffer, 0);
            outputStream.Write(buffer);
        }
    }

    /// <summary>
    /// Attempts to decrypt a 128-bit block of memory using the specified game key.
    /// </summary>
    /// <returns>True if the block was successfully decrypted, False if not.</returns>
    public static bool TryDecryptBlock(byte[] block, Game game)
    {
        using var decryptor = Aes.CreateDecryptor(GetGameKey(game), null);

        return BlockIsUnencrypted(decryptor.TransformFinalBlock(block, 0, 16));
    }

    /// <summary>
    /// Tests whether the first compressed block of a Coalesced file is encrypted.
    /// </summary>
    /// <remarks>
    /// This takes advantage of the fact that the first four bytes in the block represent the int32 number of ini files.
    /// <br/>The upper two bytes will always be 0 for sane Coalesced files.
    /// </remarks>
    /// <returns>True if the block is unencrypted, False if not.</returns>
    public static bool BlockIsUnencrypted(Span<byte> block) => block[2] == 0 && block[3] == 0;
}
