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
        [SerializeField] private VisualTreeAsset contractTemplate;
        [SerializeField] private VisualTreeAsset shipTemplate;
        [SerializeField] private VisualTreeAsset systemTemplate;
        [SerializeField] private VisualTreeAsset factionTemplate;
        [SerializeField] private VisualTreeAsset systemPanelTemplate;
        [SerializeField] private VisualTreeAsset waypointIconTemplate;

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
            SwitchTab(Tab.Fleet);
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
            if (backBtn != null) backBtn.clicked += () => SceneManager.LoadScene("MainMenu");

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

            if (tab == Tab.Map)
            {
                // MapPresenter logic (Placeholder for now)
                _dataContainer.Add(new Label("Galaxy Map (Use MapPresenter for detailed implementation)"));
                return;
            }

            _ = FetchAndDisplayTab(tab);
        }

        private async Task FetchAndDisplayTab(Tab tab)
        {
            if (_statusLabel != null) _statusLabel.text = "Fetching data...";
            
            try
            {
                _client.SetToken(_authManager.AgentToken);

                switch (tab)
                {
                    case Tab.Agent:
                        var agentRes = await _apiService.GetMyAgent();
                        DisplayAgent(agentRes.Data);
                        break;
                    case Tab.Contracts:
                        var contractRes = await _apiService.GetContracts();
                        DisplayList(Tab.Contracts, contractRes.Data.ToArray());
                        break;
                    case Tab.Fleet:
                        var fleetRes = await _apiService.GetShips();
                        DisplayList(Tab.Fleet, fleetRes.Data.ToArray());
                        break;
                    case Tab.Factions:
                        var factionsRes = await _apiService.GetFactions();
                        DisplayList(Tab.Factions, factionsRes.Data.ToArray());
                        break;
                }

                if (_statusLabel != null) _statusLabel.text = string.Empty;
            }
            catch (System.Exception e)
            {
                Log.Error("[Dashboard] Refresh failed: {Error}", e.Message);
                if (_statusLabel != null) _statusLabel.text = $"Error: {e.Message}";
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

        private void DisplayList<T>(Tab tab, T[] items)
        {
            if (items == null || items.Length == 0)
            {
                _dataContainer.Add(new Label("No items found."));
                return;
            }

            var scroll = (ScrollView)GetContentRoot();
            
            foreach (var item in items)
            {
                if (item is Contract c && _contractPresenter != null)
                {
                    _contractPresenter.Populate(scroll, new[] { c });
                }
                else if (item is Ship s && _fleetPresenter != null)
                {
                    _fleetPresenter.Populate(scroll, new[] { s });
                }
                else if (item is Faction f)
                {
                    scroll.Add(BindFaction(f));
                }
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
    }
}
