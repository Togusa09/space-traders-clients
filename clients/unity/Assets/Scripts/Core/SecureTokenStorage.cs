using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SpaceTraders.Core
{
    public static class SecureTokenStorage
    {
        private const string Salt = "SpaceTradersUnitySalt_4829183";

        private static byte[] GetEncryptionKey()
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            string rawKey = deviceId + Salt;
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
            }
        }

        public static string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return string.Empty;

            try
            {
                byte[] key = GetEncryptionKey();
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();
                    byte[] iv = aes.IV;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Prepend IV to the stream
                        ms.Write(iv, 0, iv.Length);

                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                            cs.Write(plaintextBytes, 0, plaintextBytes.Length);
                            cs.FlushFinalBlock();
                        }

                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SecureTokenStorage] Encryption failed: {e.Message}");
                return string.Empty;
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return string.Empty;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(encryptedText);
                byte[] key = GetEncryptionKey();

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    int ivLength = aes.BlockSize / 8; // 128 bits = 16 bytes
                    if (cipherBytes.Length < ivLength)
                    {
                        Debug.LogError("[SecureTokenStorage] Cipher text is too short to contain IV.");
                        return string.Empty;
                    }

                    byte[] iv = new byte[ivLength];
                    Array.Copy(cipherBytes, 0, iv, 0, ivLength);
                    aes.IV = iv;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, ivLength, cipherBytes.Length - ivLength);
                            cs.FlushFinalBlock();
                        }

                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SecureTokenStorage] Decryption failed: {e.Message}");
                return string.Empty;
            }
        }
    }
}
