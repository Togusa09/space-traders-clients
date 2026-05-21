using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpaceTraders.Core;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.UI
{
    public class MenuManager : MonoBehaviour
    {
        private Button _playButton;
        private Button _settingsButton;
        private Button _quitButton;

        private AuthManager _authManager;

        [Inject]
        public void Construct(AuthManager authManager)
        {
            _authManager = authManager;
            Log.Info("[MenuManager] AuthManager injected successfully.");
        }

        private void Start()
        {
            InitializeUI();
            
            if (_authManager != null)
            {
                _authManager.LoadTokens();
                UpdateUI();
            }
            else
            {
                Log.Warning("[MenuManager] AuthManager is null in Start(). Injection might have failed.");
            }
        }

        private void InitializeUI()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Log.Error("[MenuManager] UIDocument component missing on {Name}", gameObject.name);
                return;
            }

            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Log.Error("[MenuManager] UI Root Visual Element is null.");
                return;
            }

            // Corrected names from MainMenu.uxml
            _playButton = root.Q<Button>("play-button");
            _settingsButton = root.Q<Button>("settings-button");
            _quitButton = root.Q<Button>("exit-button");

            if (_playButton != null)
            {
                _playButton.clicked += OnPlayClicked;
                Log.Info("[MenuManager] Bound Play button.");
            }
            else
            {
                Log.Warning("[MenuManager] Play button ('play-button') not found in UXML.");
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked += OnSettingsClicked;
                Log.Info("[MenuManager] Bound Settings button.");
            }
            else
            {
                Log.Warning("[MenuManager] Settings button ('settings-button') not found in UXML.");
            }

            if (_quitButton != null)
            {
                _quitButton.clicked += OnQuitClicked;
                Log.Info("[MenuManager] Bound Exit button.");
            }
            else
            {
                Log.Warning("[MenuManager] Exit button ('exit-button') not found in UXML.");
            }
        }

        private void UpdateUI()
        {
            if (_playButton != null && _authManager != null)
            {
                _playButton.SetEnabled(_authManager.HasAgentToken);
                Log.Info("[MenuManager] Play button enabled: {Enabled}", _authManager.HasAgentToken);
            }
        }

        private void OnPlayClicked()
        {
            Log.Info("[MenuManager] Play clicked. Loading GameplayPlaceholder.");
            SceneManager.LoadScene("GameplayPlaceholder");
        }

        private void OnSettingsClicked()
        {
            Log.Info("[MenuManager] Settings clicked. Loading Settings.");
            SceneManager.LoadScene("Settings");
        }

        private void OnQuitClicked()
        {
            Log.Info("[MenuManager] Exit clicked. Quitting application.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
