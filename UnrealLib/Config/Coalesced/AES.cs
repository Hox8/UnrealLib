using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnrealLib.Core;
using static UnrealLib.Core.Globals;

namespace UnrealLib.Config.Crypto;

public static class AES
{
    public const int BlockSize = 16;

    // 256-bit AES keys for each game.
    private static readonly byte[] KeyIB3 = "6nHmjd:hbWNf=9|UO2:?;K0y+gZL-jP5"u8.ToArray();
    private static readonly byte[] KeyIB2 = "|FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B"u8.ToArray();
    private static readonly byte[] KeyIB1 = "NoBwPWDkRqFMTaHeVCJkXmLSZoNoBIPm"u8.ToArray();
    private static readonly byte[] KeyVOTE = "DKksEKHkldF#(WDJ#FMS7jla5f(@J12|"u8.ToArray();

    /// <summary>
    /// Returns the AES key for each game, used for Coalesced and/or save file encryption.
    /// </summary>
    /// <param name="game">The game to get the corresponding key for.</param>
    /// <remarks>
    /// - IB1 encrypts its save games using static keys and does not encrypt its coalesced inis.<br/>
    /// - IB2 encrypts its save games and coalesced inis using static keys.<br/>
    /// - IB3 and VOTE encrypt their save games using dynamic keys*, and their coalesced inis using static keys.<br/><br/>
    /// *IB3 and VOTE will attempt to decrypt save games using static keys under certain circumstances.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] GetGameKey(Game game)
    {
        UDebug.Assert(game >= Game.IB1 && game < Game.MAX);

        return game switch
        {
            Game.IB1 => KeyIB1,
            Game.IB2 => KeyIB2,
            Game.IB3 => KeyIB3,
            Game.VOTE or _ => KeyVOTE
        };
    }

    private static Aes CreateAesForGame(Game game)
    {
        var crypto = Aes.Create();

        crypto.Padding = PaddingMode.Zeros;

        crypto.Key = GetGameKey(game);

        // IB1 engine branch uses CBC, newer branches use simpler ECB
        if (game == Game.IB1)
        {
            crypto.Mode = CipherMode.CBC;

            // IB1 (CBC) needs to start with an empty IV
#if NET10_0_OR_GREATER
            crypto.SetKey(stackalloc byte[BlockSize]);
#else
            crypto.IV = new byte[BlockSize];
#endif
        }
        else
        {
            crypto.Mode = CipherMode.ECB;
        }

        return crypto;
    }

    /// <summary>
    /// Performs encryption on inputStream, writing the results to outputStream.
    /// </summary>
    /// <param name="inputStream">The input stream containing the data to perform encryption on.</param>
    /// <param name="outputStream">The output stream receiving the transformed bytes.</param>
    /// <param name="game">The game to perform encryption for. Each game has its own AES key.</param>
    /// <param name="bDoEncryption">If true, encrypts the contents in inputStream. If false, decrypts the contents instead.</param>
    public static void EncryptStream(Stream inputStream, FileStream outputStream, Game game, bool bDoEncryption)
    {
        using var aes = CreateAesForGame(game);

        using var transformer = bDoEncryption
            ? aes.CreateEncryptor()
            : aes.CreateDecryptor();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(16384);

        int bytesRead;
        while ((bytesRead = inputStream.Read(buffer)) != 0)
        {
            transformer.TransformBlock(buffer, 0, bytesRead, buffer, 0);
            outputStream.Write(buffer);
        }

        ArrayPool<byte>.Shared.Return(buffer);
    }

    /// <summary>
    /// Attempts to decrypt a 128-bit block of memory using the specified game key.
    /// </summary>
    /// <remarks>This should only be used against the first 16 bytes of coalesced ini data.</remarks>
    /// <returns>True if the block was successfully decrypted, false if not.</returns>
    public static bool TryDecryptBlock(byte[] block, Game game)
    {
        using var aes = CreateAesForGame(game);
        using var transformer = aes.CreateDecryptor();

        return IsBlockUnencrypted(transformer.TransformFinalBlock(block, 0, BlockSize));
    }

    /// <summary>
    /// Tests whether the first compressed block of a Coalesced file is encrypted.
    /// This can only be used on the first block in a Coalesced!
    /// </summary>
    /// <remarks>
    /// This takes advantage of the fact that the first four bytes in the block represent the int32 number of ini files.
    /// <br/>The upper two bytes will always be 0 for sane Coalesced files.
    /// </remarks>
    /// <returns>True if the block is unencrypted, false if not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBlockUnencrypted(ReadOnlySpan<byte> block) => block[2] == 0 && block[3] == 0;
}
