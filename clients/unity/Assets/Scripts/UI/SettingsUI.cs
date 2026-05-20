using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpaceTraders.Core;
using SpaceTraders.API;
using VContainer;

namespace SpaceTraders.UI
{
    public class SettingsUI : MonoBehaviour
    {
        private TextField _agentTokenInput;
        private Button _saveButton;
        private Button _backButton;
        private Button _startSyncButton;
        private Button _stopSyncButton;
        private Button _clearCacheButton;
        private Label _syncStatusLabel;
        private Label _dbStatusLabel;

        private AuthManager _authManager;
        private SpaceTradersClient _client;
        private APIService _apiService;
        private DatabaseManager _dbManager;
        private UniverseSyncManager _syncManager;

        [Inject]
        public void Construct(
            AuthManager authManager, 
            SpaceTradersClient client, 
            APIService apiService, 
            DatabaseManager dbManager, 
            UniverseSyncManager syncManager)
        {
            _authManager = authManager;
            _client = client;
            _apiService = apiService;
            _dbManager = dbManager;
            _syncManager = syncManager;
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _agentTokenInput = root.Q<TextField>("AgentTokenInput");
            _saveButton = root.Q<Button>("SaveButton");
            _backButton = root.Q<Button>("BackButton");
            _startSyncButton = root.Q<Button>("StartSyncButton");
            _stopSyncButton = root.Q<Button>("StopSyncButton");
            _clearCacheButton = root.Q<Button>("ClearCacheButton");
            _syncStatusLabel = root.Q<Label>("SyncStatusLabel");
            _dbStatusLabel = root.Q<Label>("DatabaseStatusLabel");

            _saveButton.clicked += OnSaveClicked;
            _backButton.clicked += () => SceneManager.LoadScene("MainMenu");
            _startSyncButton.clicked += OnStartSyncClicked;
            _stopSyncButton.clicked += OnStopSyncClicked;
            _clearCacheButton.clicked += OnClearCacheClicked;

            _agentTokenInput.value = _authManager.AgentToken;
        }

        private void Update()
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_syncStatusLabel == null) return;

            var sync = _syncManager;
            var db = _dbManager;

            if (sync.IsSyncing)
            {
                _syncStatusLabel.text = $"Syncing: {sync.Progress:P0} (Page {sync.CurrentPage}/{sync.TotalPages})";
                _startSyncButton.SetEnabled(false);
                _stopSyncButton.SetEnabled(true);
            }
            else
            {
                _syncStatusLabel.text = "Sync Idle";
                _startSyncButton.SetEnabled(true);
                _stopSyncButton.SetEnabled(false);
            }

            _dbStatusLabel.text = $"Indexed Systems: {db.GetIndexedSystemCount()}";
        }

        private void OnStartSyncClicked() => _syncManager.StartSync();
        private void OnStopSyncClicked() => _syncManager.StopSync();
        private void OnClearCacheClicked()
        {
            _dbManager.ClearCache();
            UpdateStatus();
        }

        private async void OnSaveClicked()
        {
            _saveButton.SetEnabled(false);
            try
            {
                _authManager.SaveAgentToken(_agentTokenInput.value);
                
                // Validate token by fetching agent data
                _client.SetToken(_agentTokenInput.value);
                var response = await _apiService.GetMyAgent();
                
                Debug.Log($"[SettingsUI] Token validated for agent: {response.data.symbol}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SettingsUI] Failed to validate token: {ex.Message}");
            }
            finally
            {
                _saveButton.SetEnabled(true);
            }
        }
    }
}
