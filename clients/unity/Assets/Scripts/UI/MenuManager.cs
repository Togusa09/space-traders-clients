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
        }

        private void OnEnable()
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

            _playButton = root.Q<Button>("PlayButton");
            _settingsButton = root.Q<Button>("SettingsButton");
            _quitButton = root.Q<Button>("QuitButton");

            if (_playButton != null) _playButton.clicked += OnPlayClicked;
            if (_settingsButton != null) _settingsButton.clicked += OnSettingsClicked;
            if (_quitButton != null) _quitButton.clicked += OnQuitClicked;

            if (_authManager != null)
            {
                _authManager.LoadTokens();
                UpdateUI();
            }
            else
            {
                Log.Warning("[MenuManager] AuthManager not injected. Dependency Injection might not be configured correctly in this scene.");
            }
        }

        private void UpdateUI()
        {
            if (_playButton != null && _authManager != null)
            {
                _playButton.SetEnabled(_authManager.HasAgentToken);
            }
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene("GameplayPlaceholder");
        }

        private void OnSettingsClicked()
        {
            SceneManager.LoadScene("Settings");
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
