using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SpaceTraders.API;
using SpaceTraders.UI;

namespace SpaceTraders.Core
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize Core Managers
            _ = DatabaseManager.Instance;
            _ = AuthManager.Instance;
            _ = SpaceTradersClient.Instance;
            _ = UniverseSyncManager.Instance;
        }

        public void OnRegistrationSuccess(string token)
        {
            AuthManager.Instance.SaveAgentToken(token);
            SceneManager.LoadScene("MainMenu");
        }
    }
}
