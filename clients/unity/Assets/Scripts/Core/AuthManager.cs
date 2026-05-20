using System;
using UnityEngine;

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

        private static AuthManager _instance;
        public static AuthManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<AuthManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AuthManager");
                        _instance = go.AddComponent<AuthManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public string AgentToken { get; private set; }
        public bool HasAgentToken => !string.IsNullOrEmpty(AgentToken);
        public TokenState CurrentTokenState { get; private set; } = TokenState.Unknown;

        public static event Action OnTokenUnauthorized;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTokens();
        }

        public void SaveAgentToken(string token)
        {
            AgentToken = token;
            string encryptedToken = SecureTokenStorage.Encrypt(token);
            PlayerPrefs.SetString(AgentTokenKey, encryptedToken);
            PlayerPrefs.Save();
            CurrentTokenState = string.IsNullOrEmpty(token) ? TokenState.Unknown : TokenState.Valid;
            Debug.Log($"[AuthManager] Token saved and encrypted: {(string.IsNullOrEmpty(token) ? "EMPTY" : "EXISTS")}");
        }

        public void ClearTokens(bool keepInvalidState = false)
        {
            AgentToken = null;
            PlayerPrefs.DeleteKey(AgentTokenKey);
            PlayerPrefs.Save();
            CurrentTokenState = keepInvalidState ? TokenState.Invalid : TokenState.Unknown;
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
            Debug.Log($"[AuthManager] Token loaded and decrypted: {(string.IsNullOrEmpty(AgentToken) ? "EMPTY" : "EXISTS")} (State: {CurrentTokenState})");
        }

        public void HandleTokenUnauthorized()
        {
            ClearTokens(keepInvalidState: true);
            OnTokenUnauthorized?.Invoke();
        }
    }
}

