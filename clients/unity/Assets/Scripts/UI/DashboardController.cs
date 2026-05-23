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

        [Header("Item Templates")]
        [SerializeField] private VisualTreeAsset factionTemplate;

        private VisualElement _root;
        private VisualElement _dataContainer;
        private Label _statusLabel;
        
        // Presenters
        private ContractPresenter _contractPresenter;
        private FleetPresenter _fleetPresenter;
        private MapPresenter _mapPresenter;

        private SpaceTradersClient _client;
        private AuthManager _authManager;
        private APIService _apiService;

        private Tab _currentTab = Tab.Agent;

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
            SwitchTab(Tab.Agent);
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
            _statusLabel = _root.Q<Label>("status-label");

            // Bind Global Buttons
            var backBtn = _root.Q<Button>("back-button");
            if (backBtn != null) backBtn.clicked += () => SceneManager.LoadScene(SceneNames.MainMenu);

            // Bind Sidebar/Tab Buttons
            _root.Q<Button>("tab-agent")?.RegisterCallback<ClickEvent>(evt => SwitchTab(Tab.Agent));
            _root.Q<Button>("tab-contracts")?.RegisterCallback<ClickEvent>(evt => SwitchTab(Tab.Contracts));
            _root.Q<Button>("tab-fleet")?.RegisterCallback<ClickEvent>(evt => SwitchTab(Tab.Fleet));
            _root.Q<Button>("tab-map")?.RegisterCallback<ClickEvent>(evt => SwitchTab(Tab.Map));
            _root.Q<Button>("tab-factions")?.RegisterCallback<ClickEvent>(evt => SwitchTab(Tab.Factions));

            Log.Info("[DashboardController] UI initialized.");
        }

        public void SwitchTab(Tab tab)
        {
            _currentTab = tab;
            if (_dataContainer == null) return;

            _dataContainer.Clear();
            if (_statusLabel != null) _statusLabel.text = string.Empty;
            
            Log.Info("[Dashboard] Switching to tab: {Tab}", tab);

            if (DashboardTabBehavior.IsPresenterManagedTab(tab))
            {
                DisplayMapTab();
                return;
            }

            _ = FetchAndDisplayTab(tab);
        }

        private void DisplayMapTab()
        {
            if (_mapPresenter != null)
            {
                _mapPresenter.SetupMapPanel(_dataContainer);
            }
            else
            {
                _dataContainer.Add(new Label("MapPresenter component not found on Dashboard GameObject."));
            }
        }

        private async Task FetchAndDisplayTab(Tab tab)
        {
            if (_statusLabel != null) _statusLabel.text = "Fetching data...";
            
            try
            {
                _client.SetToken(_authManager.AgentToken);

                await FetchAndRenderTabData(tab);

                if (_statusLabel != null) _statusLabel.text = string.Empty;
            }
            catch (System.Exception e)
            {
                Log.Error("[Dashboard] Refresh failed: {Error}", e.Message);
                if (_statusLabel != null) _statusLabel.text = $"Error: {e.Message}";
            }
        }

        private async Task FetchAndRenderTabData(Tab tab)
        {
            var payload = await DashboardTabDataRouter.FetchAsync(tab, _apiService);
            RenderTabPayload(tab, payload);
        }

        private void RenderTabPayload(Tab tab, DashboardTabPayload payload)
        {
            switch (tab)
            {
                case Tab.Agent:
                    DisplayAgent(payload.Agent);
                    break;
                case Tab.Contracts:
                    DisplayContracts(payload.Contracts);
                    break;
                case Tab.Fleet:
                    DisplayFleet(payload.Fleet);
                    break;
                case Tab.Factions:
                    DisplayFactions(payload.Factions);
                    break;
            }
        }

        private VisualElement GetContentRoot()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            _dataContainer.Add(scroll);
            return scroll;
        }

        private void DisplayAgent(Agent agent)
        {
            var root = GetContentRoot();
            AddRow(root, "Symbol", agent.Symbol);
            AddRow(root, "Headquarters", agent.Headquarters);
            AddRow(root, "Credits", agent.Credits.ToString("N0"));
            AddRow(root, "Starting Faction", agent.StartingFaction);
            AddRow(root, "AccountId", agent.AccountId);
        }

        private void DisplayContracts(Contract[] contracts)
        {
            if (contracts == null || contracts.Length == 0)
            {
                _dataContainer.Add(new Label("No contracts found."));
                return;
            }

            var scroll = (ScrollView)GetContentRoot();
            if (_contractPresenter != null)
            {
                _contractPresenter.Populate(scroll, contracts);
            }
            else
            {
                scroll.Add(new Label("ContractPresenter missing."));
            }
        }

        private void DisplayFleet(Ship[] ships)
        {
            if (ships == null || ships.Length == 0)
            {
                _dataContainer.Add(new Label("No ships found."));
                return;
            }

            var scroll = (ScrollView)GetContentRoot();
            if (_fleetPresenter != null)
            {
                _fleetPresenter.Populate(scroll, ships);
            }
            else
            {
                scroll.Add(new Label("FleetPresenter missing."));
            }
        }

        private void DisplayFactions(Faction[] factions)
        {
            if (factions == null || factions.Length == 0)
            {
                _dataContainer.Add(new Label("No factions found."));
                return;
            }

            var scroll = (ScrollView)GetContentRoot();
            foreach (var f in factions)
            {
                scroll.Add(BindFaction(f));
            }
        }

        private VisualElement BindFaction(Faction f)
        {
            if (factionTemplate == null) return new Label($"{f.Name} ({f.Symbol})");
            
            var element = factionTemplate.Instantiate();
            element.Q<Label>("name-label").text = $"Name: {f.Name}";
            element.Q<Label>("details-label").text = $"Symbol: {f.Symbol} | HQ: {f.Headquarters}";
            element.Q<Label>("description-label").text = f.Description;
            return element;
        }

        private void AddRow(VisualElement root, string key, string value)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5 } };    
            row.Add(new Label($"{key}: ") { style = { unityFontStyleAndWeight = FontStyle.Bold, width = 150, color = Color.gray } });
            row.Add(new Label(value) { style = { color = Color.white, flexGrow = 1 } });
            root.Add(row);
        }

        // --- Polling for active views ---
        private readonly DashboardPollingScheduler _pollingScheduler = new DashboardPollingScheduler(15f);
        private void Update()
        {
            if (_pollingScheduler.ShouldPoll(Time.deltaTime, _currentTab))
            {
                _ = FetchAndDisplayTab(_currentTab);
            }
        }
    }

    internal static class DashboardTabBehavior
    {
        public static bool IsPresenterManagedTab(DashboardController.Tab tab)
        {
            return tab == DashboardController.Tab.Map;
        }

        public static bool ShouldPoll(DashboardController.Tab tab)
        {
            return tab != DashboardController.Tab.Map && tab != DashboardController.Tab.Agent;
        }
    }

    internal sealed class DashboardPollingScheduler
    {
        private readonly float _intervalSeconds;
        private float _elapsedSeconds;

        public DashboardPollingScheduler(float intervalSeconds)
        {
            _intervalSeconds = intervalSeconds;
        }

        public bool ShouldPoll(float deltaTime, DashboardController.Tab currentTab)
        {
            _elapsedSeconds += deltaTime;
            if (_elapsedSeconds <= _intervalSeconds) return false;

            _elapsedSeconds = 0f;
            return DashboardTabBehavior.ShouldPoll(currentTab);
        }
    }

    internal sealed class DashboardTabPayload
    {
        public Agent Agent { get; set; }
        public Contract[] Contracts { get; set; }
        public Ship[] Fleet { get; set; }
        public Faction[] Factions { get; set; }
    }

    internal static class DashboardTabDataRouter
    {
        public static async Task<DashboardTabPayload> FetchAsync(DashboardController.Tab tab, APIService apiService)
        {
            var payload = new DashboardTabPayload();

            switch (tab)
            {
                case DashboardController.Tab.Agent:
                    payload.Agent = (await apiService.GetMyAgent())?.Data;
                    break;
                case DashboardController.Tab.Contracts:
                    payload.Contracts = (await apiService.GetContracts())?.Data?.ToArray();
                    break;
                case DashboardController.Tab.Fleet:
                    payload.Fleet = (await apiService.GetShips())?.Data?.ToArray();
                    break;
                case DashboardController.Tab.Factions:
                    payload.Factions = (await apiService.GetFactions())?.Data?.ToArray();
                    break;
            }

            return payload;
        }
    }
}
