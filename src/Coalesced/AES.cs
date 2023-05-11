using System.Security.Cryptography;
using System.Text;

namespace UnrealLib.Coalesced
{
    public static class AES
    {
        /// <summary>
        /// AES keys for each Infinity Blade game. Infinity Blade I does not use encryption, hence its absence
        /// </summary>
        public static readonly Dictionary<Game, string> GameKeys = new Dictionary<Game, string>()
        {
            { Game.IB3, "6nHmjd:hbWNf=9|UO2:?;K0y+gZL-jP5" },
            { Game.IB2, "|FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B" },
            { Game.VOTE, "DKksEKHkldF#(WDJ#FMS7jla5f(@J12|" }
        };

        /// <summary>
        /// Takes a coalesced UnrealStream and encrypts or decrypts the data based on the given mode.
        /// </summary>
        /// <param name="unStream">The UnrealStream to perform crypto on</param>
        /// <param name="game">The Coalesced's game. Used to determine encryption key</param>
        /// <param name="modeIsDecrypt">Boolean to determine whether to encrypt or decrypt</param>
        public static void CryptoECB(UnrealStream unStream, Game game, bool modeIsDecrypt)
        {
            using Aes aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Key = Encoding.ASCII.GetBytes(GameKeys[game]);  // IB1 should NEVER trigger CryptoECB(), so no need for TryGetValue()
            aes.Padding = PaddingMode.Zeros;

            // If stream isn't a valid ECB block size (multiple of 16), pad its length to the next multiple
            int remainder = unStream.Length % 16;
            if (remainder != 0)
            {
                unStream.Position = unStream.Length;
                unStream.Write(new byte[16 - remainder]);
            }

            using ICryptoTransform crypto = modeIsDecrypt ? aes.CreateDecryptor() : aes.CreateEncryptor();
            byte[] value = crypto.TransformFinalBlock(unStream.ToArray(), 0, unStream.Length);

            unStream.Position = 0;
            unStream.Write(value);
        }
    }
}
