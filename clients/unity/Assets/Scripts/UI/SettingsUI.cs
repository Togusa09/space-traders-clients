using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpaceTraders.Core;
using SpaceTraders.API;
using VContainer;
using Unity.Logging;

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

        private void Start()
        {
            InitializeUI();
            
            if (_authManager != null)
            {
                _agentTokenInput.value = _authManager.AgentToken;
            }
        }

        private void InitializeUI()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;
            
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            _agentTokenInput = root.Q<TextField>("AgentTokenInput");
            _saveButton = root.Q<Button>("SaveButton");
            _backButton = root.Q<Button>("BackButton");
            _startSyncButton = root.Q<Button>("StartSyncButton");
            _stopSyncButton = root.Q<Button>("StopSyncButton");
            _clearCacheButton = root.Q<Button>("ClearCacheButton");
            _syncStatusLabel = root.Q<Label>("SyncStatusLabel");
            _dbStatusLabel = root.Q<Label>("DatabaseStatusLabel");

            if (_saveButton != null) _saveButton.clicked += OnSaveClicked;
            if (_backButton != null) _backButton.clicked += () => SceneManager.LoadScene("MainMenu");
            if (_startSyncButton != null) _startSyncButton.clicked += OnStartSyncClicked;
            if (_stopSyncButton != null) _stopSyncButton.clicked += OnStopSyncClicked;
            if (_clearCacheButton != null) _clearCacheButton.clicked += OnClearCacheClicked;
        }

        private void Update()
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_syncStatusLabel == null || _syncManager == null || _dbManager == null) return;

            var sync = _syncManager;
            var db = _dbManager;

            if (sync.IsSyncing)
            {
                _syncStatusLabel.text = $"Syncing: {sync.Progress:P0} (Page {sync.CurrentPage}/{sync.TotalPages})";
                if (_startSyncButton != null) _startSyncButton.SetEnabled(false);
                if (_stopSyncButton != null) _stopSyncButton.SetEnabled(true);
            }
            else
            {
                _syncStatusLabel.text = "Sync Idle";
                if (_startSyncButton != null) _startSyncButton.SetEnabled(true);
                if (_stopSyncButton != null) _stopSyncButton.SetEnabled(false);
            }

            _dbStatusLabel.text = $"Indexed Systems: {db.GetIndexedSystemCount()}";
        }

        private void OnStartSyncClicked() => _syncManager?.StartSync();
        private void OnStopSyncClicked() => _syncManager?.StopSync();
        private void OnClearCacheClicked()
        {
            _dbManager?.ClearCache();
            UpdateStatus();
        }

        private async void OnSaveClicked()
        {
            if (_authManager == null || _client == null || _apiService == null) return;

            _saveButton.SetEnabled(false);
            try
            {
                _authManager.SaveAgentToken(_agentTokenInput.value);
                
                // Validate token by fetching agent data
                _client.SetToken(_agentTokenInput.value);
                var response = await _apiService.GetMyAgent();
                
                Log.Info("[SettingsUI] Token validated for agent: {Agent}", response.data.symbol);
            }
            catch (System.Exception ex)
            {
                Log.Error("[SettingsUI] Failed to validate token: {Error}", ex.Message);
            }
            finally
            {
                _saveButton.SetEnabled(true);
            }
        }
    }
}
