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
        public enum Tab { Agent, Contracts, Fleet, Map, Factions }

        private VisualElement _root;
        private VisualElement _dataContainer;
        
        // Header (Labels from Dashboard.uxml - checking if they exist)
        private Label _agentNameLabel;
        private Label _creditsLabel;
        private Label _factionLabel;
        private Label _hqLabel;

        // Sub-Presenters
        private ContractPresenter _contractPresenter;
        private FleetPresenter _fleetPresenter;
        private MapPresenter _mapPresenter;

        private SpaceTradersClient _client;
        private AuthManager _authManager;
        private APIService _apiService;

        private Tab _currentTab = Tab.Fleet;

        [Inject]
        public void Construct(SpaceTradersClient client, AuthManager authManager, APIService apiService)
        {
            _client = client;
            _authManager = authManager;
            _apiService = apiService;
        }

        private void Start()
        {
            _contractPresenter = GetComponent<ContractPresenter>();
            _fleetPresenter = GetComponent<FleetPresenter>();
            _mapPresenter = GetComponent<MapPresenter>();

            InitializeUI();
            ShowTab(Tab.Fleet);
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

            _dataContainer = _root.Q<VisualElement>("data-container");

            // Bind Global Buttons
            var backBtn = _root.Q<Button>("back-button");
            if (backBtn != null) backBtn.clicked += () => SceneManager.LoadScene("MainMenu");

            // Bind Sidebar/Tab Buttons
            _root.Q<Button>("tab-agent")?.RegisterCallback<ClickEvent>(evt => ShowTab(Tab.Agent));
            _root.Q<Button>("tab-contracts")?.RegisterCallback<ClickEvent>(evt => ShowTab(Tab.Contracts));
            _root.Q<Button>("tab-fleet")?.RegisterCallback<ClickEvent>(evt => ShowTab(Tab.Fleet));
            _root.Q<Button>("tab-map")?.RegisterCallback<ClickEvent>(evt => ShowTab(Tab.Map));
            _root.Q<Button>("tab-factions")?.RegisterCallback<ClickEvent>(evt => ShowTab(Tab.Factions));

            // Bind Header labels (if they exist in the UXML)
            _agentNameLabel = _root.Q<Label>("AgentName");
            _creditsLabel = _root.Q<Label>("CreditsValue");
            _factionLabel = _root.Q<Label>("FactionValue");
            _hqLabel = _root.Q<Label>("HQValue");

            Log.Info("[DashboardController] UI initialized.");
        }

        private void ShowTab(Tab tab)
        {
            _currentTab = tab;
            if (_dataContainer == null) return;

            _dataContainer.Clear();
            Log.Info("[Dashboard] Showing tab: {CurrentTab}", tab);

            switch (tab)
            {
                case Tab.Agent:
                    var agentLabel = new Label("Agent Info (Detailed view not implemented)");
                    _dataContainer.Add(agentLabel);
                    break;
                case Tab.Contracts:
                    var contractList = new ScrollView { name = "ContractList" };
                    _dataContainer.Add(contractList);
                    RefreshData();
                    break;
                case Tab.Fleet:
                    var fleetList = new ScrollView { name = "ShipList" };
                    _dataContainer.Add(fleetList);
                    RefreshData();
                    break;
                case Tab.Map:
                    // MapPresenter typically handles its own UI injection or binding
                    // For now, let's see if we can trigger its enable logic
                    if (_mapPresenter != null)
                    {
                        // MapPresenter might need a specific layout
                        Log.Info("[Dashboard] Map tab selected.");
                    }
                    break;
                case Tab.Factions:
                    var factionList = new ScrollView { name = "FactionList" };
                    _dataContainer.Add(factionList);
                    RefreshData();
                    break;
            }
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
                if (_currentTab == Tab.Fleet)
                {
                    var res = await _apiService.GetShips();
                    UpdateFleet(res.Data.ToArray());
                }
                else if (_currentTab == Tab.Contracts)
                {
                    var res = await _apiService.GetContracts();
                    UpdateContracts(res.Data.ToArray());
                }
                else if (_currentTab == Tab.Factions)
                {
                    var res = await _apiService.GetFactions();
                    UpdateFactions(res.Data.ToArray());
                }

                // Always update header if labels exist
                var agentRes = await _apiService.GetMyAgent();
                UpdateHeader(agentRes.Data);
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
            var list = _dataContainer?.Q<ScrollView>("FactionList");
            if (list == null) return;

            list.Clear();
            foreach (var f in factions)
            {
                var entry = new Label($"{f.Name} ({f.Symbol}) - Recruiting: {f.IsRecruiting}");
                entry.style.paddingBottom = 5;
                list.Add(entry);
            }
        }

        private void UpdateContracts(Contract[] contracts)
        {
            var list = _dataContainer?.Q<ScrollView>("ContractList");
            if (list == null || _contractPresenter == null) return;
            _contractPresenter.Populate(list, contracts);
        }

        private void UpdateFleet(Ship[] ships)
        {
            var list = _dataContainer?.Q<ScrollView>("ShipList");
            if (list == null || _fleetPresenter == null) return;
            _fleetPresenter.Populate(list, ships);
        }

        // --- Polling for active views ---
        private float _pollTimer = 0f;
        private void Update()
        {
            _pollTimer += Time.deltaTime;
            if (_pollTimer > 10f)
            {
                _pollTimer = 0f;
                RefreshData();
            }
        }
    }
}
