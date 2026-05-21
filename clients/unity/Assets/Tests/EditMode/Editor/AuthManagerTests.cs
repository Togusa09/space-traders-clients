using System;
using NUnit.Framework;
using UnityEngine;
using SpaceTraders.Core;

namespace SpaceTraders.Tests
{
    [TestFixture]
    public class AuthManagerTests
    {
        private const string TestToken = "spt-test-token-1234567890-abcdef";

        [SetUp]
        public void Setup()
        {
            // Clear PlayerPrefs before each test
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        [Test]
        public void SecureTokenStorage_EncryptDecrypt_Success()
        {
            string encrypted = SecureTokenStorage.Encrypt(TestToken);
            Assert.IsNotEmpty(encrypted);
            Assert.AreNotEqual(TestToken, encrypted);

            string decrypted = SecureTokenStorage.Decrypt(encrypted);
            Assert.AreEqual(TestToken, decrypted);
        }

        [Test]
        public void SecureTokenStorage_EncryptEmptyString_ReturnsEmpty()
        {
            string encrypted = SecureTokenStorage.Encrypt(string.Empty);
            Assert.AreEqual(string.Empty, encrypted);

            string decrypted = SecureTokenStorage.Decrypt(string.Empty);
            Assert.AreEqual(string.Empty, decrypted);
        }

        [Test]
        public void SecureTokenStorage_DecryptCorruptedString_ReturnsEmpty()
        {
            // Expect the decryption failed error log
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Decryption failed.*"));

            // Invalid base64
            string corrupted = "invalid-base64-payload!";
            string decrypted = SecureTokenStorage.Decrypt(corrupted);
            Assert.AreEqual(string.Empty, decrypted);
        }

        [Test]
        public void SecureTokenStorage_DecryptInvalidIV_ReturnsEmpty()
        {
            // Expect the too short IV error log
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Cipher text is too short.*"));

            // Base64 with less bytes than IV block size
            string shortBase64 = Convert.ToBase64String(new byte[5]);
            string decrypted = SecureTokenStorage.Decrypt(shortBase64);
            Assert.AreEqual(string.Empty, decrypted);
        }

        [Test]
        public void AuthManager_InitialState_IsUnknown()
        {
            GameObject go = new GameObject("TestAuthManager");
            var manager = go.AddComponent<AuthManager>();
            
            try
            {
                Assert.AreEqual(TokenState.Unknown, manager.CurrentTokenState);
                Assert.IsFalse(manager.HasAgentToken);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AuthManager_SaveToken_SavesAndDecrypts()
        {
            GameObject go = new GameObject("TestAuthManager");
            var manager = go.AddComponent<AuthManager>();

            try
            {
                manager.SaveAgentToken(TestToken);

                Assert.AreEqual(TestToken, manager.AgentToken);
                Assert.IsTrue(manager.HasAgentToken);
                Assert.AreEqual(TokenState.Valid, manager.CurrentTokenState);

                // Verify stored in PlayerPrefs
                string stored = PlayerPrefs.GetString("SpaceTraders_AgentToken", string.Empty);
                Assert.IsNotEmpty(stored);
                Assert.AreNotEqual(TestToken, stored);
                
                // Load back
                manager.ClearTokens();
                Assert.IsFalse(manager.HasAgentToken);

                PlayerPrefs.SetString("SpaceTraders_AgentToken", stored);
                manager.LoadTokens();
                Assert.AreEqual(TestToken, manager.AgentToken);
                Assert.AreEqual(TokenState.Valid, manager.CurrentTokenState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AuthManager_ClearTokens_ClearsState()
        {
            GameObject go = new GameObject("TestAuthManager");
            var manager = go.AddComponent<AuthManager>();

            try
            {
                manager.SaveAgentToken(TestToken);
                Assert.IsTrue(manager.HasAgentToken);

                manager.ClearTokens();
                Assert.IsFalse(manager.HasAgentToken);
                Assert.AreEqual(TokenState.Unknown, manager.CurrentTokenState);
                Assert.IsFalse(PlayerPrefs.HasKey("SpaceTraders_AgentToken"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AuthManager_HandleTokenUnauthorized_FiresEvent()
        {
            GameObject go = new GameObject("TestAuthManager");
            var manager = go.AddComponent<AuthManager>();
            bool eventFired = false;

            try
            {
                manager.SaveAgentToken(TestToken);
                
                Action handler = () => eventFired = true;
                AuthManager.OnTokenUnauthorized += handler;

                try
                {
                    manager.HandleTokenUnauthorized();
                    Assert.IsTrue(eventFired);
                    Assert.AreEqual(TokenState.Invalid, manager.CurrentTokenState);
                    Assert.IsFalse(manager.HasAgentToken);
                }
                finally
                {
                    AuthManager.OnTokenUnauthorized -= handler;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
