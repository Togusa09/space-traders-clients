using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpaceTraders.Core;
using SpaceTraders.API;
using SpaceTraders.API.Models;

namespace SpaceTraders.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        // Removed AuthManager, apiClient, and apiService fields as we use Singletons

        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private TextField _agentTokenInput;
        private Button _saveButton;
        private Button _backButton;
        private Button _testAgentButton;
        private Label _statusLabel;

        // Popup elements
        private VisualElement _popupInstance;
        private VisualElement _popupOverlay;
        private VisualElement _popupDataContainer;
        private Label _popupTitle;
        private Button _popupCloseButton;

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            _agentTokenInput = root.Q<TextField>("agent-token-input");
            _saveButton = root.Q<Button>("save-button");
            _backButton = root.Q<Button>("back-button");
            _testAgentButton = root.Q<Button>("test-agent-button");
            _statusLabel = root.Q<Label>("status-label");

            // Popup
            _popupInstance = root.Q<VisualElement>("popup-instance");
            _popupOverlay = root.Q<VisualElement>("popup-overlay");
            _popupDataContainer = root.Q<VisualElement>("popup-data-container");
            _popupTitle = root.Q<Label>("popup-title");
            _popupCloseButton = root.Q<Button>("popup-close-button");

            // Populate current values from Singleton
            _agentTokenInput.value = AuthManager.Instance.AgentToken;

            _saveButton.clicked += OnSaveClicked;
            _backButton.clicked += OnBackClicked;
            _testAgentButton.clicked += OnTestAgentClicked;
            _popupCloseButton.clicked += () => {
                _popupOverlay.style.display = DisplayStyle.None;
                _popupInstance.style.display = DisplayStyle.None;
            };
        }

        private void OnSaveClicked()
        {
            AuthManager.Instance.SaveAgentToken(_agentTokenInput.value);
            _statusLabel.text = "Token saved successfully!";
        }

        private void OnBackClicked()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnTestAgentClicked()
        {
            _statusLabel.text = "Testing Agent Token...";
            SpaceTradersClient.Instance.SetToken(_agentTokenInput.value);
            APIService.Instance.GetMyAgent(
                response => ShowAgentData(response.data),
                error => ShowError("Agent Test Failed", error)
            );
        }

        private void ShowAgentData(Agent agent)
        {
            _statusLabel.text = "Agent test success.";
            _popupTitle.text = "Agent Information";
            _popupDataContainer.Clear();

            AddDataRow("Symbol", agent.symbol);
            AddDataRow("Headquarters", agent.headquarters);
            AddDataRow("Credits", agent.credits.ToString("N0"));
            AddDataRow("Starting Faction", agent.startingFaction);
            AddDataRow("Account ID", agent.accountId);

            _popupInstance.style.display = DisplayStyle.Flex;
            _popupOverlay.style.display = DisplayStyle.Flex;
        }

        private void ShowError(string title, string error)
        {
            _statusLabel.text = "Test failed.";
            _popupTitle.text = title;
            _popupDataContainer.Clear();
            
            var errorLabel = new Label(error);
            errorLabel.style.color = Color.red;
            errorLabel.style.whiteSpace = WhiteSpace.Normal;
            _popupDataContainer.Add(errorLabel);

            _popupInstance.style.display = DisplayStyle.Flex;
            _popupOverlay.style.display = DisplayStyle.Flex;
        }

        private void AddDataRow(string key, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 5;

            var keyLabel = new Label($"{key}: ");
            keyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            keyLabel.style.width = 150;
            keyLabel.style.color = Color.gray;

            var valueLabel = new Label(value);
            valueLabel.style.color = Color.white;
            valueLabel.style.flexGrow = 1;

            row.Add(keyLabel);
            row.Add(valueLabel);
            _popupDataContainer.Add(row);
        }
    }
}
