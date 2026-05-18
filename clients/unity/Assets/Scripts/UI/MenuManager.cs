using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpaceTraders.Core;

namespace SpaceTraders.UI
{
    public class MenuManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        // Removed AuthManager field as we use the Singleton

        [SerializeField] private string playSceneName = "GameplayPlaceholder";
        [SerializeField] private string settingsSceneName = "Settings";

        private Button _playButton;
        private Button _settingsButton;
        private Button _exitButton;

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            _playButton = root.Q<Button>("play-button");
            _settingsButton = root.Q<Button>("settings-button");
            _exitButton = root.Q<Button>("exit-button");

            _playButton.clicked += OnPlayClicked;
            _settingsButton.clicked += OnSettingsClicked;
            _exitButton.clicked += OnExitClicked;

            // Ensure AuthManager is initialized
            AuthManager.Instance.LoadTokens();
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (_playButton != null)
            {
                // Play is disabled if agent token is missing
                _playButton.SetEnabled(AuthManager.Instance.HasAgentToken);
            }
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene(playSceneName);
        }

        private void OnSettingsClicked()
        {
            SceneManager.LoadScene(settingsSceneName);
        }

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
