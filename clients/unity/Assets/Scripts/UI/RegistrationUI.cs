using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;
using VContainer;
using Unity.Logging;

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

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Log.Error("[RegistrationUI] UIDocument missing.");
                return;
            }

            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Log.Error("[RegistrationUI] Root visual element null.");
                return;
            }

            _symbolInput = root.Q<TextField>("symbol-input");
            _factionInput = root.Q<TextField>("faction-input");
            _registerButton = root.Q<Button>("register-button");
            _statusLabel = root.Q<Label>("status-label");

            if (_registerButton != null) _registerButton.clicked += OnRegisterClicked;
            else Log.Warning("[RegistrationUI] 'register-button' not found.");
        }

        private async void OnRegisterClicked()
        {
            if (_apiService == null || _gameManager == null || _symbolInput == null || _factionInput == null) return;

            string symbol = _symbolInput.value;
            string faction = _factionInput.value;

            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(faction))
            {
                if (_statusLabel != null) _statusLabel.text = "Symbol and Faction are required.";
                return;
            }

            _registerButton.SetEnabled(false);
            if (_statusLabel != null) _statusLabel.text = "Registering...";

            try
            {
                var response = await _apiService.Register(symbol, faction);
                if (_statusLabel != null) _statusLabel.text = "Registration successful!";
                // response.Data.Token (Generated model naming)
                _gameManager.OnRegistrationSuccess(response.Data.Token);
            }
            catch (System.Exception ex)
            {
                if (_statusLabel != null) _statusLabel.text = $"Error: {ex.Message}";
                _registerButton.SetEnabled(true);
            }
        }
    }
}
