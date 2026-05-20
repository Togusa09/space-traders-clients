using System;
using UnityEngine;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.Core
{
    public enum TokenState
    {
        Unknown,
        Valid,
        Invalid
    }

    public class AuthManager : MonoBehaviour
    {
        private const string AgentTokenKey = "SpaceTraders_AgentToken";

        public string AgentToken { get; private set; }
        public bool HasAgentToken => !string.IsNullOrEmpty(AgentToken);
        public TokenState CurrentTokenState { get; private set; } = TokenState.Unknown;

        public static event Action OnTokenUnauthorized;

        private void Awake()
        {
            LoadTokens();
        }

        public void SaveAgentToken(string token)
        {
            AgentToken = token;
            string encryptedToken = SecureTokenStorage.Encrypt(token);
            PlayerPrefs.SetString(AgentTokenKey, encryptedToken);
            PlayerPrefs.Save();
            CurrentTokenState = string.IsNullOrEmpty(token) ? TokenState.Unknown : TokenState.Valid;
            Log.Info("[AuthManager] Token saved and encrypted: {State}", (string.IsNullOrEmpty(token) ? "EMPTY" : "EXISTS"));
        }

        public void ClearTokens(bool keepInvalidState = false)
        {
            AgentToken = null;
            PlayerPrefs.DeleteKey(AgentTokenKey);
            PlayerPrefs.Save();
            CurrentTokenState = keepInvalidState ? TokenState.Invalid : TokenState.Unknown;
            Log.Info("[AuthManager] Tokens cleared. State: {State}", CurrentTokenState);
        }

        public void LoadTokens()
        {
            string encryptedToken = PlayerPrefs.GetString(AgentTokenKey, string.Empty);
            if (!string.IsNullOrEmpty(encryptedToken))
            {
                AgentToken = SecureTokenStorage.Decrypt(encryptedToken);
                CurrentTokenState = string.IsNullOrEmpty(AgentToken) ? TokenState.Invalid : TokenState.Valid;
            }
            else
            {
                AgentToken = string.Empty;
                CurrentTokenState = TokenState.Unknown;
            }
            Log.Info("[AuthManager] Token loaded and decrypted: {State} (State: {TokenState})", (string.IsNullOrEmpty(AgentToken) ? "EMPTY" : "EXISTS"), CurrentTokenState);
        }

        public void HandleTokenUnauthorized()
        {
            Log.Warning("[AuthManager] Handling unauthorized token (401).");
            ClearTokens(keepInvalidState: true);
            OnTokenUnauthorized?.Invoke();
        }
    }
}
