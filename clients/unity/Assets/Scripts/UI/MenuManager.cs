using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpaceTraders.Core;
using VContainer;

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
            var root = GetComponent<UIDocument>().rootVisualElement;

            _playButton = root.Q<Button>("PlayButton");
            _settingsButton = root.Q<Button>("SettingsButton");
            _quitButton = root.Q<Button>("QuitButton");

            if (_playButton != null) _playButton.clicked += OnPlayClicked;
            if (_settingsButton != null) _settingsButton.clicked += OnSettingsClicked;
            if (_quitButton != null) _quitButton.clicked += OnQuitClicked;

            _authManager.LoadTokens();
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_playButton != null)
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
