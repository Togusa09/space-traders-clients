using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpaceTraders.Core;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using System;

namespace SpaceTraders.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private TextField _agentTokenInput;
        private Button _saveButton, _backButton, _testAgentButton;
        private Label _statusLabel;

        // Debug Elements
        private Button _startSyncBtn, _stopSyncBtn, _clearCacheBtn;
        private Label _syncStatus, _dbCount, _expectedCount, _syncProgress;

        // Popup elements
        private VisualElement _popupInstance, _popupOverlay, _popupDataContainer;
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

            // Debug Bindings
            _startSyncBtn = root.Q<Button>("start-sync-btn");
            _stopSyncBtn = root.Q<Button>("stop-sync-btn");
            _clearCacheBtn = root.Q<Button>("clear-cache-button");
            
            _syncStatus = root.Q<Label>("sync-status");
            _dbCount = root.Q<Label>("db-count");
            _expectedCount = root.Q<Label>("expected-count");
            _syncProgress = root.Q<Label>("sync-progress");

            // Popup
            _popupInstance = root.Q<VisualElement>("popup-instance");
            _popupOverlay = root.Q<VisualElement>("popup-overlay");
            _popupDataContainer = root.Q<VisualElement>("popup-data-container");
            _popupTitle = root.Q<Label>("popup-title");
            _popupCloseButton = root.Q<Button>("popup-close-button");

            _agentTokenInput.value = AuthManager.Instance.AgentToken;

            _saveButton.clicked += OnSaveClicked;
            _backButton.clicked += OnBackClicked;
            _testAgentButton.clicked += OnTestAgentClicked;

            _startSyncBtn.clicked += OnStartSyncClicked;
            _stopSyncBtn.clicked += OnStopSyncClicked;
            _clearCacheBtn.clicked += OnClearCacheClicked;

            _popupCloseButton.clicked += OnPopupCloseClicked;
        }

        private void OnDisable()
        {
            if (_saveButton != null) _saveButton.clicked -= OnSaveClicked;
            if (_backButton != null) _backButton.clicked -= OnBackClicked;
            if (_testAgentButton != null) _testAgentButton.clicked -= OnTestAgentClicked;
            if (_startSyncBtn != null) _startSyncBtn.clicked -= OnStartSyncClicked;
            if (_stopSyncBtn != null) _stopSyncBtn.clicked -= OnStopSyncClicked;
            if (_clearCacheBtn != null) _clearCacheBtn.clicked -= OnClearCacheClicked;
            if (_popupCloseButton != null) _popupCloseButton.clicked -= OnPopupCloseClicked;
        }

        private void OnStartSyncClicked() => UniverseSyncManager.Instance.StartSync();
        private void OnStopSyncClicked() => UniverseSyncManager.Instance.StopSync();
        private void OnClearCacheClicked()
        {
            DatabaseManager.Instance.ClearCache();
            _statusLabel.text = "Database cleared.";
        }
        private void OnPopupCloseClicked()
        {
            _popupOverlay.style.display = DisplayStyle.None;
            _popupInstance.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            // Update stats in real-time
            var sync = UniverseSyncManager.Instance;
            var db = DatabaseManager.Instance;

            _syncStatus.text = sync.IsSyncing ? "Syncing..." : "Idle";
            _syncStatus.style.color = sync.IsSyncing ? Color.green : Color.white;
            
            _dbCount.text = db.GetIndexedSystemCount().ToString("N0");
            _expectedCount.text = sync.TotalSystemsExpected > 0 ? sync.TotalSystemsExpected.ToString("N0") : "?";
            _syncProgress.text = $"{(sync.Progress * 100):F1}% (Page {sync.CurrentPage}/{sync.TotalPages})";

            _startSyncBtn.SetEnabled(!sync.IsSyncing);
            _stopSyncBtn.SetEnabled(sync.IsSyncing);
        }

        private void OnSaveClicked()
        {
            AuthManager.Instance.SaveAgentToken(_agentTokenInput.value);
            _statusLabel.text = "Token saved successfully!";
        }

        private void OnBackClicked() => SceneManager.LoadScene(mainMenuSceneName);

        private async void OnTestAgentClicked()
        {
            _statusLabel.text = "Testing Agent Token...";
            try
            {
                SpaceTradersClient.Instance.SetToken(_agentTokenInput.value);
                var response = await APIService.Instance.GetMyAgent();
                ShowAgentData(response.data);
            }
            catch (Exception e)
            {
                ShowError("Agent Test Failed", e.Message);
            }
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
            _popupInstance.style.display = DisplayStyle.Flex;
            _popupOverlay.style.display = DisplayStyle.Flex;
        }

        private void ShowError(string title, string error)
        {
            _statusLabel.text = "Test failed.";
            _popupTitle.text = title;
            _popupDataContainer.Clear();
            var errorLabel = new Label(error) { style = { color = Color.red, whiteSpace = WhiteSpace.Normal } };
            _popupDataContainer.Add(errorLabel);
            _popupInstance.style.display = DisplayStyle.Flex;
            _popupOverlay.style.display = DisplayStyle.Flex;
        }

        private void AddDataRow(string key, string value)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5 } };
            row.Add(new Label($"{key}: ") { style = { unityFontStyleAndWeight = FontStyle.Bold, width = 150, color = Color.gray } });
            row.Add(new Label(value) { style = { color = Color.white, flexGrow = 1 } });
            _popupDataContainer.Add(row);
        }
    }
}
