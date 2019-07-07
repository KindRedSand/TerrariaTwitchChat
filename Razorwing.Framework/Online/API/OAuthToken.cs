//using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

namespace TwitchChat.Razorwing.Framework.Online.API
{
    /// <summary>
    /// Cutted from my API. Twitch IRC Chat auth use only OAuth 1.0 token, that means token never expire before new was generated
    /// </summary>
    [Serializable]
    internal class OAuthToken
    {
        /// <summary>
        /// OAuth 2.0 access token.
        /// </summary>
        //[JsonProperty(@"access_token")]
        public string AccessToken;

        //[JsonProperty(@"expires_in")]
        public long ExpiresIn
        {
            get
            {
                return AccessTokenExpiry - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            set
            {
                AccessTokenExpiry = DateTimeOffset.Now.AddSeconds(value).ToUnixTimeSeconds();
            }
        }

        public bool IsValid => !string.IsNullOrEmpty(AccessToken) && ExpiresIn > 30;

        public long AccessTokenExpiry;

        /// <summary>
        /// OAuth 2.0 refresh token.
        /// </summary>
        //[JsonProperty(@"refresh_token")]
        public string RefreshToken;

        public override string ToString() => Convert.ToBase64String(EncryptStringToBytes_Aes($@"{AccessToken}|{AccessTokenExpiry.ToString(NumberFormatInfo.InvariantInfo)}|{RefreshToken}", key));

        public static OAuthToken Parse(string value)
        {
            try
            {
                value = DecryptStringFromBytes_Aes(Convert.FromBase64String(value), key);
                string[] parts = value.Split('|');
                return new OAuthToken
                {
                    AccessToken = parts[0],
                    AccessTokenExpiry = long.Parse(parts[1], NumberFormatInfo.InvariantInfo),
                    RefreshToken = parts[2]
                };
            }
            catch
            {

            }
            return null;
        }

        private static byte[] key = new byte[] { 255, 35, 62, 46, 69, 127, 230, 57, 255, 35, 62, 46, 69, 127, 230, 57 };

        public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key)
        {
            byte[] encrypted;
            byte[] IV;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;

                aesAlg.GenerateIV();
                IV = aesAlg.IV;

                aesAlg.Mode = CipherMode.CBC;

                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption. 
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            var combinedIvCt = new byte[IV.Length + encrypted.Length];
            Array.Copy(IV, 0, combinedIvCt, 0, IV.Length);
            Array.Copy(encrypted, 0, combinedIvCt, IV.Length, encrypted.Length);

            // Return the encrypted bytes from the memory stream. 
            return combinedIvCt;

        }

        public static string DecryptStringFromBytes_Aes(byte[] cipherTextCombined, byte[] Key)
        {

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an Aes object 
            // with the specified key and IV. 
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;

                byte[] IV = new byte[aesAlg.BlockSize / 8];
                byte[] cipherText = new byte[cipherTextCombined.Length - IV.Length];

                Array.Copy(cipherTextCombined, IV, IV.Length);
                Array.Copy(cipherTextCombined, IV.Length, cipherText, 0, cipherText.Length);

                aesAlg.IV = IV;

                aesAlg.Mode = CipherMode.CBC;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption. 
                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
    }
}