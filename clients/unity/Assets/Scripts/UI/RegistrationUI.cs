using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpaceTraders.Core;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using System;

namespace SpaceTraders.UI
{
    public class RegistrationUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private TextField _symbolInput;
        private DropdownField _factionDropdown;
        private Button _registerButton;
        private Button _backButton;
        private Label _statusLabel;

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            _symbolInput = root.Q<TextField>("symbol-input");
            _factionDropdown = root.Q<DropdownField>("faction-dropdown");
            _registerButton = root.Q<Button>("register-button");
            _backButton = root.Q<Button>("back-button");
            _statusLabel = root.Q<Label>("status-label");

            // Populate factions
            _factionDropdown.choices = new System.Collections.Generic.List<string> { "COSMIC", "VOID", "GALACTIC", "QUANTUM", "DOMINION" };
            _factionDropdown.value = "COSMIC";

            _registerButton.clicked += OnRegisterClicked;
            _backButton.clicked += () => SceneManager.LoadScene(mainMenuSceneName);
        }

        private async void OnRegisterClicked()
        {
            string symbol = _symbolInput.value;
            string faction = _factionDropdown.value;

            if (string.IsNullOrEmpty(symbol))
            {
                _statusLabel.text = "Please enter a symbol.";
                return;
            }

            _statusLabel.text = "Registering...";
            _registerButton.SetEnabled(false);

            try
            {
                var response = await APIService.Instance.Register(symbol, faction);
                _statusLabel.text = "Registration successful!";
                GameManager.Instance.OnRegistrationSuccess(response.data.token);
            }
            catch (Exception e)
            {
                _statusLabel.text = $"Registration failed: {e.Message}";
                _registerButton.SetEnabled(true);
            }
        }
    }
}
