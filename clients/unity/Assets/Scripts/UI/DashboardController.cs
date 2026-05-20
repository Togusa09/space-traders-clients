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

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;

            // Bind Header
            _agentNameLabel = _root.Q<Label>("AgentName");
            _creditsLabel = _root.Q<Label>("CreditsValue");
            _factionLabel = _root.Q<Label>("FactionValue");
            _hqLabel = _root.Q<Label>("HQValue");

            // Bind Sidebar Buttons
            _root.Q<Button>("BtnFactions").clicked += () => ShowPanel(_panelFactions);
            _root.Q<Button>("BtnContracts").clicked += () => ShowPanel(_panelContracts);
            _root.Q<Button>("BtnFleet").clicked += () => ShowPanel(_panelFleet);
            _root.Q<Button>("BtnSystems").clicked += () => ShowPanel(_panelSystems);
            _root.Q<Button>("BtnRefresh").clicked += RefreshData;

            // Panels
            _panelFactions = _root.Q<VisualElement>("PanelFactions");
            _panelContracts = _root.Q<VisualElement>("PanelContracts");
            _panelFleet = _root.Q<VisualElement>("PanelFleet");
            _panelSystems = _root.Q<VisualElement>("PanelSystems");

            // Default view
            ShowPanel(_panelFleet);
            
            RefreshData();
        }

        private void ShowPanel(VisualElement panel)
        {
            if (_currentPanel != null) _currentPanel.style.display = DisplayStyle.None;
            _currentPanel = panel;
            if (_currentPanel != null) _currentPanel.style.display = DisplayStyle.Flex;
        }

        private async void RefreshData()
        {
            if (!_authManager.HasAgentToken) return;

            _client.SetToken(_authManager.AgentToken);

            try
            {
                // Parallel data fetching
                var agentTask = _apiService.GetMyAgent();
                var contractTask = _apiService.GetContracts();
                var shipTask = _apiService.GetShips();

                await Task.WhenAll(agentTask, contractTask, shipTask);

                UpdateHeader(agentTask.Result.data);
                UpdateContracts(contractTask.Result.data);
                UpdateFleet(shipTask.Result.data);

                // Factions are static-ish, fetch once if needed
                var factions = await _apiService.GetFactions();
                UpdateFactions(factions.data);
            }
            catch (System.Exception e)
            {
                Log.Error("[Dashboard] Refresh failed: {Error}", e.Message);
            }
        }

        private void UpdateHeader(Agent agent)
        {
            _agentNameLabel.text = agent.symbol;
            _creditsLabel.text = agent.credits.ToString("N0");
            _factionLabel.text = agent.startingFaction;
            _hqLabel.text = agent.headquarters;
        }

        private void UpdateFactions(Faction[] factions)
        {
            var list = _panelFactions.Q<ScrollView>("FactionList");
            list.Clear();
            foreach (var f in factions)
            {
                var entry = new Label($"{f.name} ({f.symbol}) - Recruiting: {f.isRecruiting}");
                list.Add(entry);
            }
        }

        private void UpdateContracts(Contract[] contracts)
        {
            var list = _panelContracts.Q<ScrollView>("ContractList");
            list.Clear();
            var presenter = GetComponent<ContractPresenter>();
            if (presenter != null) presenter.Populate(list, contracts);
        }

        private void UpdateFleet(Ship[] ships)
        {
            var list = _panelFleet.Q<ScrollView>("ShipList");
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
            if (_currentPanel == _panelFleet)
            {
                var ships = await _apiService.GetShips();
                UpdateFleet(ships.data);
            }
            else if (_currentPanel == _panelContracts)
            {
                // In a real app, maybe only refresh if we know something changed
                // but for dashboard keeping it simple:
                try
                {
                    var agent = await _apiService.GetMyAgent();
                    UpdateHeader(agent.data);

                    var contracts = await _apiService.GetContracts();
                    UpdateContracts(contracts.data);

                    var ships = await _apiService.GetShips();
                    UpdateFleet(ships.data);
                }
                catch { /* silence background poll errors */ }
            }
            else if (_currentPanel == _panelFactions)
            {
                var factions = await _apiService.GetFactions();
                UpdateFactions(factions.data);
            }
        }
    }
}
