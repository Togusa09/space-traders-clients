using UnityEngine;

namespace SpaceTraders.Core
{
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
            PlayerPrefs.SetString(AgentTokenKey, token);
            PlayerPrefs.Save();
            Debug.Log($"[AuthManager] Token saved: {(string.IsNullOrEmpty(token) ? "EMPTY" : "EXISTS")}");
        }

        public void ClearTokens()
        {
            AgentToken = null;
            PlayerPrefs.DeleteKey(AgentTokenKey);
            PlayerPrefs.Save();
        }

        public void LoadTokens()
        {
            AgentToken = PlayerPrefs.GetString(AgentTokenKey, string.Empty);
            Debug.Log($"[AuthManager] Token loaded: {(string.IsNullOrEmpty(AgentToken) ? "EMPTY" : "EXISTS")}");
        }
    }
}
