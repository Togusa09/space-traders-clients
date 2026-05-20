using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using SpaceTraders.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.UI
{
    public class DashboardController : MonoBehaviour
    {
        private VisualElement _root;
        private Label _agentNameLabel;
        private Label _creditsLabel;
        private Label _factionLabel;
        private Label _hqLabel;

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
            if (uiDocument == null) return;
            
            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            // Bind Header
            _agentNameLabel = _root.Q<Label>("AgentName");
            _creditsLabel = _root.Q<Label>("CreditsValue");
            _factionLabel = _root.Q<Label>("FactionValue");
            _hqLabel = _root.Q<Label>("HQValue");

            // Bind Sidebar Buttons
            var btnFactions = _root.Q<Button>("BtnFactions");
            var btnContracts = _root.Q<Button>("BtnContracts");
            var btnFleet = _root.Q<Button>("BtnFleet");
            var btnSystems = _root.Q<Button>("BtnSystems");
            var btnRefresh = _root.Q<Button>("BtnRefresh");

            if (btnFactions != null) btnFactions.clicked += () => ShowPanel(_panelFactions);
            if (btnContracts != null) btnContracts.clicked += () => ShowPanel(_panelContracts);
            if (btnFleet != null) btnFleet.clicked += () => ShowPanel(_panelFleet);
            if (btnSystems != null) btnSystems.clicked += () => ShowPanel(_panelSystems);
            if (btnRefresh != null) btnRefresh.clicked += RefreshData;

            // Panels
            _panelFactions = _root.Q<VisualElement>("PanelFactions");
            _panelContracts = _root.Q<VisualElement>("PanelContracts");
            _panelFleet = _root.Q<VisualElement>("PanelFleet");
            _panelSystems = _root.Q<VisualElement>("PanelSystems");

            // Default view
            ShowPanel(_panelFleet);
        }

        private void ShowPanel(VisualElement panel)
        {
            if (_currentPanel != null) _currentPanel.style.display = DisplayStyle.None;
            _currentPanel = panel;
            if (_currentPanel != null) _currentPanel.style.display = DisplayStyle.Flex;
        }

        private async void RefreshData()
        {
            if (_authManager == null || !_authManager.HasAgentToken || _client == null || _apiService == null) return;

            _client.SetToken(_authManager.AgentToken);

            try
            {
                // Parallel data fetching
                var agentTask = _apiService.GetMyAgent();
                var contractTask = _apiService.GetContracts();
                var shipTask = _apiService.GetShips();

                await Task.WhenAll(agentTask, contractTask, shipTask);

                if (agentTask.Result != null) UpdateHeader(agentTask.Result.data);
                if (contractTask.Result != null) UpdateContracts(contractTask.Result.data);
                if (shipTask.Result != null) UpdateFleet(shipTask.Result.data);

                // Factions are static-ish, fetch once if needed
                var factions = await _apiService.GetFactions();
                if (factions != null) UpdateFactions(factions.data);
            }
            catch (System.Exception e)
            {
                Log.Error("[Dashboard] Refresh failed: {Error}", e.Message);
            }
        }

        private void UpdateHeader(Agent agent)
        {
            if (_agentNameLabel != null) _agentNameLabel.text = agent.symbol;
            if (_creditsLabel != null) _creditsLabel.text = agent.credits.ToString("N0");
            if (_factionLabel != null) _factionLabel.text = agent.startingFaction;
            if (_hqLabel != null) _hqLabel.text = agent.headquarters;
        }

        private void UpdateFactions(Faction[] factions)
        {
            if (_panelFactions == null) return;
            var list = _panelFactions.Q<ScrollView>("FactionList");
            if (list == null) return;

            list.Clear();
            foreach (var f in factions)
            {
                var entry = new Label($"{f.name} ({f.symbol}) - Recruiting: {f.isRecruiting}");
                list.Add(entry);
            }
        }

        private void UpdateContracts(Contract[] contracts)
        {
            if (_panelContracts == null) return;
            var list = _panelContracts.Q<ScrollView>("ContractList");
            if (list == null) return;

            list.Clear();
            var presenter = GetComponent<ContractPresenter>();
            if (presenter != null) presenter.Populate(list, contracts);
        }

        private void UpdateFleet(Ship[] ships)
        {
            if (_panelFleet == null) return;
            var list = _panelFleet.Q<ScrollView>("ShipList");
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
                if (ships != null) UpdateFleet(ships.data);
            }
            else if (_currentPanel == _panelContracts)
            {
                try
                {
                    var agent = await _apiService.GetMyAgent();
                    if (agent != null) UpdateHeader(agent.data);

                    var contracts = await _apiService.GetContracts();
                    if (contracts != null) UpdateContracts(contracts.data);

                    var ships = await _apiService.GetShips();
                    if (ships != null) UpdateFleet(ships.data);
                }
                catch { /* silence background poll errors */ }
            }
            else if (_currentPanel == _panelFactions)
            {
                var factions = await _apiService.GetFactions();
                if (factions != null) UpdateFactions(factions.data);
            }
        }
    }
}
