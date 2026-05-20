using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using VContainer;

namespace SpaceTraders.UI
{
    public class RegistrationUI : MonoBehaviour
    {
        private TextField _symbolInput;
        private TextField _factionInput;
        private Button _registerButton;
        private Label _statusLabel;

        private APIService _apiService;
        private Core.GameManager _gameManager;

        [Inject]
        public void Construct(APIService apiService, Core.GameManager gameManager)
        {
            _apiService = apiService;
            _gameManager = gameManager;
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _symbolInput = root.Q<TextField>("SymbolInput");
            _factionInput = root.Q<TextField>("FactionInput");
            _registerButton = root.Q<Button>("RegisterButton");
            _statusLabel = root.Q<Label>("StatusLabel");

            _registerButton.clicked += OnRegisterClicked;
        }

        private async void OnRegisterClicked()
        {
            string symbol = _symbolInput.value;
            string faction = _factionInput.value;

            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(faction))
            {
                _statusLabel.text = "Symbol and Faction are required.";
                return;
            }

            _registerButton.SetEnabled(false);
            _statusLabel.text = "Registering...";

            try
            {
                var response = await _apiService.Register(symbol, faction);
                _statusLabel.text = "Registration successful!";
                _gameManager.OnRegistrationSuccess(response.data.token);
            }
            catch (System.Exception ex)
            {
                _statusLabel.text = $"Error: {ex.Message}";
                _registerButton.SetEnabled(true);
            }
        }
    }
}
