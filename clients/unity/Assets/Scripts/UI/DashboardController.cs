using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;
using SpaceTraders.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.UI
{
    public class DashboardController : MonoBehaviour
    {
        private VisualElement _root;
        
        // Header
        private Label _agentNameLabel;
        private Label _creditsLabel;
        private Label _factionLabel;
        private Label _hqLabel;

        // Tabs/Panels
        private VisualElement _panelFactions;
        private VisualElement _panelContracts;
        private VisualElement _panelFleet;
        private VisualElement _panelSystems;
        private VisualElement _currentPanel;

        private SpaceTradersClient _client;
        private AuthManager _authManager;
        private APIService _apiService;

        [Inject]
        public void Construct(SpaceTradersClient client, AuthManager authManager, APIService apiService)
        {
            _client = client;
            _authManager = authManager;
            _apiService = apiService;
        }

        private void Start()
        {
            InitializeUI();
            RefreshData();
        }

        private void InitializeUI()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Log.Error("[DashboardController] UIDocument component missing.");
                return;
            }
            
            _root = uiDocument.rootVisualElement;
            if (_root == null)
            {
                Log.Error("[DashboardController] UI Root is null.");
                return;
            }

            // Bind Global Buttons
            var backBtn = _root.Q<Button>("back-button");
            if (backBtn != null) backBtn.clicked += () => SceneManager.LoadScene("MainMenu");

            // Bind Sidebar/Tab Buttons (Mapping to Dashboard.uxml names)
            var btnAgent = _root.Q<Button>("tab-agent");
            var btnFactions = _root.Q<Button>("tab-factions");
            var btnContracts = _root.Q<Button>("tab-contracts");
            var btnFleet = _root.Q<Button>("tab-fleet");
            var btnMap = _root.Q<Button>("tab-map");

            if (btnAgent != null) btnAgent.clicked += () => Log.Info("[Dashboard] Agent tab clicked (Not implemented yet)");
            if (btnFactions != null) btnFactions.clicked += () => ShowPanel(_panelFactions);
            if (btnContracts != null) btnContracts.clicked += () => ShowPanel(_panelContracts);
            if (btnFleet != null) btnFleet.clicked += () => ShowPanel(_panelFleet);
            if (btnMap != null) btnMap.clicked += () => ShowPanel(_panelSystems);

            // Panels (Searching for data-container if separate panels aren't defined in this UXML)
            var dataContainer = _root.Q<VisualElement>("data-container");
            
            // Legacy fallbacks for single-UXML layout
            _panelFactions = _root.Q<VisualElement>("PanelFactions") ?? dataContainer;
            _panelContracts = _root.Q<VisualElement>("PanelContracts");
            _panelFleet = _root.Q<VisualElement>("PanelFleet");
            _panelSystems = _root.Q<VisualElement>("PanelSystems");

            // Bind Header labels
            _agentNameLabel = _root.Q<Label>("AgentName");
            _creditsLabel = _root.Q<Label>("CreditsValue");
            _factionLabel = _root.Q<Label>("FactionValue");
            _hqLabel = _root.Q<Label>("HQValue");

            // Default view
            ShowPanel(_panelFleet);
            
            Log.Info("[DashboardController] UI initialized.");
        }

        private void ShowPanel(VisualElement panel)
        {
            if (panel == null) return;
            if (_currentPanel != null) _currentPanel.style.display = DisplayStyle.None;
            _currentPanel = panel;
            _currentPanel.style.display = DisplayStyle.Flex;
        }

        private async void RefreshData()
        {
            if (_authManager == null || !_authManager.HasAgentToken || _client == null || _apiService == null)
            {
                Log.Warning("[DashboardController] Refresh aborted. Dependencies not met.");
                return;
            }

            _client.SetToken(_authManager.AgentToken);

            try
            {
                // Parallel data fetching
                var agentTask = _apiService.GetMyAgent();
                var contractTask = _apiService.GetContracts();
                var shipTask = _apiService.GetShips();

                await Task.WhenAll(agentTask, contractTask, shipTask);

                if (agentTask.Result != null) UpdateHeader(agentTask.Result.Data);
                if (contractTask.Result != null) UpdateContracts(contractTask.Result.Data.ToArray());
                if (shipTask.Result != null) UpdateFleet(shipTask.Result.Data.ToArray());

                // Factions are static-ish, fetch once if needed
                var factions = await _apiService.GetFactions();
                if (factions != null) UpdateFactions(factions.Data.ToArray());
            }
            catch (System.Exception e)
            {
                Log.Error("[Dashboard] Refresh failed: {Error}", e.Message);
            }
        }

        private void UpdateHeader(Agent agent)
        {
            if (_agentNameLabel != null) _agentNameLabel.text = agent.Symbol;
            if (_creditsLabel != null) _creditsLabel.text = agent.Credits.ToString("N0");
            if (_factionLabel != null) _factionLabel.text = agent.StartingFaction;
            if (_hqLabel != null) _hqLabel.text = agent.Headquarters;
        }

        private void UpdateFactions(Faction[] factions)
        {
            if (_panelFactions == null) return;
            var list = _panelFactions.Q<ScrollView>("FactionList") ?? _panelFactions.Q<ScrollView>();
            if (list == null) return;

            list.Clear();
            foreach (var f in factions)
            {
                var entry = new Label($"{f.Name} ({f.Symbol}) - Recruiting: {f.IsRecruiting}");
                list.Add(entry);
            }
        }

        private void UpdateContracts(Contract[] contracts)
        {
            if (_panelContracts == null) return;
            var list = _panelContracts.Q<ScrollView>("ContractList") ?? _panelContracts.Q<ScrollView>();
            if (list == null) return;

            list.Clear();
            var presenter = GetComponent<ContractPresenter>();
            if (presenter != null) presenter.Populate(list, contracts);
        }

        private void UpdateFleet(Ship[] ships)
        {
            if (_panelFleet == null) return;
            var list = _panelFleet.Q<ScrollView>("ShipList") ?? _panelFleet.Q<ScrollView>();
            if (list == null) return;

            list.Clear();
            var presenter = GetComponent<FleetPresenter>();
            if (presenter != null) presenter.Populate(list, ships);
        }

        // --- Polling for active views ---
        private float _pollTimer = 0f;
        private void Update()
        {
            _pollTimer += Time.deltaTime;
            if (_pollTimer > 5f)
            {
                _pollTimer = 0f;
                _ = PollStatusUpdates();
            }
        }

        private async Task PollStatusUpdates()
        {
            if (_apiService == null) return;

            if (_currentPanel == _panelFleet)
            {
                var ships = await _apiService.GetShips();
                if (ships != null) UpdateFleet(ships.Data.ToArray());
            }
            else if (_currentPanel == _panelContracts)
            {
                try
                {
                    var agent = await _apiService.GetMyAgent();
                    if (agent != null) UpdateHeader(agent.Data);

                    var contracts = await _apiService.GetContracts();
                    if (contracts != null) UpdateContracts(contracts.Data.ToArray());

                    var ships = await _apiService.GetShips();
                    if (ships != null) UpdateFleet(ships.Data.ToArray());
                }
                catch { /* silence background poll errors */ }
            }
            else if (_currentPanel == _panelFactions)
            {
                var factions = await _apiService.GetFactions();
                if (factions != null) UpdateFactions(factions.Data.ToArray());
            }
        }
    }
}
