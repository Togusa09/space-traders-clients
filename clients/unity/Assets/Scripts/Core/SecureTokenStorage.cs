using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Unity.Logging;

namespace SpaceTraders.Core
{
    public static class SecureTokenStorage
    {
        // This is a simplified encryption helper for the prototype.
        // In a production environment, you would use platform-specific 
        // secure storage (like Keychain on iOS or Keystore on Android).

        private static readonly byte[] Key = Encoding.UTF8.GetBytes("s3cr3t_p4ss_sp4cetr4ders_1234567"); // Must be 32 bytes for AES-256

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.GenerateIV();
                    byte[] iv = aes.IV;

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(iv, 0, iv.Length); // Prepend IV
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("[SecureTokenStorage] Encryption failed: {Error}", e.Message);
                return string.Empty;
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    byte[] iv = new byte[aes.BlockSize / 8];
                    if (fullCipher.Length < iv.Length)
                    {
                        Debug.LogError("[SecureTokenStorage] Cipher text is too short to contain IV.");
                        return string.Empty;
                    }

                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
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
