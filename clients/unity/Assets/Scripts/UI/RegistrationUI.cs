using SpaceTraders.API;
using SpaceTraders.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceTraders.UI
{
    public class RegistrationUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private APIService apiService;
        [SerializeField] private GameManager gameManager;
        
        private TextField _symbolInput;
        private TextField _factionInput;
        private Button _registerButton;
        private Label _statusLabel;

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            _symbolInput = root.Q<TextField>("symbol-input");
            _factionInput = root.Q<TextField>("faction-input");
            _registerButton = root.Q<Button>("register-button");
            _statusLabel = root.Q<Label>("status-label");

            _registerButton.clicked += OnRegisterClicked;
        }

        private void OnRegisterClicked()
        {
            string symbol = _symbolInput.value;
            string faction = _factionInput.value;

            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(faction))
            {
                _statusLabel.text = "Symbol and Faction are required.";
                return;
            }

            _statusLabel.text = "Registering...";
            apiService.Register(symbol, faction, 
                response => {
                    _statusLabel.text = "Registration successful!";
                    gameManager.OnRegistrationSuccess(response.data.token);
                },
                error => {
                    _statusLabel.text = $"Error: {error}";
                }
            );
        }
    }
}
