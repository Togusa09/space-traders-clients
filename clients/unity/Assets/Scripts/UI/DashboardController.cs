using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using SpaceTraders.Core;

namespace SpaceTraders.UI
{
    public class DashboardController : MonoBehaviour
    {
        private enum Tab { Agent, Contracts, Fleet, Systems, Factions }

        [Header("UI References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Item Templates")]
        [SerializeField] private VisualTreeAsset contractTemplate;
        [SerializeField] private VisualTreeAsset shipTemplate;
        [SerializeField] private VisualTreeAsset systemTemplate;
        [SerializeField] private VisualTreeAsset factionTemplate;
        [SerializeField] private VisualTreeAsset systemPanelTemplate;
        [SerializeField] private VisualTreeAsset waypointIconTemplate;

        private Tab _currentTab = Tab.Agent;
        private Label _tabTitle;
        private VisualElement _dataContainer;
        private Label _statusLabel;
        private Button _backButton;

        // Systems Panel References
        private VisualElement _mapContainer;
        private Label _selectedSystemTitle;
        private Label _wpSymbol, _wpType, _wpCoords, _wpInfo;
        private List<VisualElement> _systemEntries = new List<VisualElement>();
        private List<VisualElement> _waypointIcons = new List<VisualElement>();

        private Dictionary<Tab, (object data, float timestamp)> _cache = new Dictionary<Tab, (object, float)>();
        private const float CacheDuration = 60f; // 1 minute

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            _tabTitle = root.Q<Label>("tab-title");
            _dataContainer = root.Q<VisualElement>("data-container");
            _statusLabel = root.Q<Label>("status-label");
            _backButton = root.Q<Button>("back-button");

            root.Q<Button>("tab-agent").clicked += () => SwitchTab(Tab.Agent);
            root.Q<Button>("tab-contracts").clicked += () => SwitchTab(Tab.Contracts);
            root.Q<Button>("tab-fleet").clicked += () => SwitchTab(Tab.Fleet);
            root.Q<Button>("tab-systems").clicked += () => SwitchTab(Tab.Systems);
            root.Q<Button>("tab-factions").clicked += () => SwitchTab(Tab.Factions);

            _backButton.clicked += () => SceneManager.LoadScene(mainMenuSceneName);

            // Set initial token and load first tab
            SpaceTradersClient.Instance.SetToken(AuthManager.Instance.AgentToken);
            SwitchTab(Tab.Agent);
        }

        private void SwitchTab(Tab tab)
        {
            _currentTab = tab;
            _tabTitle.text = tab.ToString();
            _dataContainer.Clear();
            _statusLabel.text = string.Empty;

            if (IsCacheValid(tab))
            {
                _statusLabel.text = "(Loaded from cache)";
                DisplayCachedData(tab);
            }
            else
            {
                FetchData(tab);
            }
        }

        private bool IsCacheValid(Tab tab)
        {
            return _cache.ContainsKey(tab) && (Time.time - _cache[tab].timestamp) < CacheDuration;
        }

        private void FetchData(Tab tab)
        {
            _statusLabel.text = "Fetching data...";
            switch (tab)
            {
                case Tab.Agent:
                    APIService.Instance.GetMyAgent(res => CacheAndDisplay(Tab.Agent, res.data, DisplayAgent), OnError);
                    break;
                case Tab.Contracts:
                    APIService.Instance.GetContracts(res => CacheAndDisplay(Tab.Contracts, res.data, data => DisplayList(Tab.Contracts, (Contract[])data)), OnError);
                    break;
                case Tab.Fleet:
                    apiServiceInstance.GetShips(res => CacheAndDisplay(Tab.Fleet, res.data, data => DisplayList(Tab.Fleet, (Ship[])data)), OnError);
                    break;
                case Tab.Systems:
                    APIService.Instance.GetSystems(res => CacheAndDisplay(Tab.Systems, res.data, data => DisplayList(Tab.Systems, (SystemData[])data)), OnError);
                    break;
                case Tab.Factions:
                    APIService.Instance.GetFactions(res => CacheAndDisplay(Tab.Factions, res.data, data => DisplayList(Tab.Factions, (Faction[])data)), OnError);
                    break;
            }
        }
        
        // Helper to fix the accidental rename during refactor
        private APIService apiServiceInstance => APIService.Instance;

        private void CacheAndDisplay<T>(Tab tab, T data, Action<T> displayAction)
        {
            _cache[tab] = (data, Time.time);
            _statusLabel.text = string.Empty;
            displayAction(data);
        }

        private void DisplayCachedData(Tab tab)
        {
            var data = _cache[tab].data;
            switch (tab)
            {
                case Tab.Agent: DisplayAgent((Agent)data); break;
                case Tab.Contracts: DisplayList(Tab.Contracts, (Contract[])data); break;
                case Tab.Fleet: DisplayList(Tab.Fleet, (Ship[])data); break;
                case Tab.Systems: DisplayList(Tab.Systems, (SystemData[])data); break;
                case Tab.Factions: DisplayList(Tab.Factions, (Faction[])data); break;
            }
        }

        private void OnError(string error)
        {
            _statusLabel.text = $"Error: {error}";
        }

        private void DisplayAgent(Agent agent)
        {
            AddRow("Symbol", agent.symbol);
            AddRow("Headquarters", agent.headquarters);
            AddRow("Credits", agent.credits.ToString("N0"));
            AddRow("Starting Faction", agent.startingFaction);
            AddRow("AccountId", agent.accountId);
        }

        private void DisplayList<T>(Tab tab, T[] items)
        {
            if (items == null || items.Length == 0)
            {
                _dataContainer.Add(new Label("No items found."));
                return;
            }

            if (tab == Tab.Systems && items is SystemData[] systems)
            {
                SetupSystemsPanel(systems);
                return;
            }

            foreach (var item in items)
            {
                VisualElement entry = null;
                if (item is Contract c) entry = BindContract(c);
                else if (item is Ship s) entry = BindShip(s);
                else if (item is Faction f) entry = BindFaction(f);

                if (entry != null) _dataContainer.Add(entry);
            }
        }

        private void SetupSystemsPanel(SystemData[] systems)
        {
            var panel = systemPanelTemplate.Instantiate();
            panel.style.flexGrow = 1;
            _dataContainer.Add(panel);

            var listContainer = panel.Q<ScrollView>("system-list");
            _mapContainer = panel.Q<VisualElement>("map-container");
            _selectedSystemTitle = panel.Q<Label>("selected-system-title");
            _wpSymbol = panel.Q<Label>("wp-symbol");
            _wpType = panel.Q<Label>("wp-type");
            _wpCoords = panel.Q<Label>("wp-coords");
            _wpInfo = panel.Q<Label>("wp-info");

            _systemEntries.Clear();

            foreach (var sys in systems)
            {
                var entry = systemTemplate.Instantiate();
                var root = entry.Q<VisualElement>(null, "dashboard-entry");
                root.AddToClassList("selectable-entry");
                
                entry.Q<Label>("symbol-label").text = sys.symbol;
                entry.Q<Label>("details-label").text = $"{sys.type} ({sys.waypoints.Length} WP)";

                root.RegisterCallback<ClickEvent>(evt => SelectSystem(sys, root));
                listContainer.Add(entry);
                _systemEntries.Add(root);
            }

            if (systems.Length > 0) SelectSystem(systems[0], _systemEntries[0]);
        }

        private void SelectSystem(SystemData sys, VisualElement entryRoot)
        {
            foreach (var e in _systemEntries) e.RemoveFromClassList("selected-entry");
            entryRoot.AddToClassList("selected-entry");

            _selectedSystemTitle.text = $"System: {sys.symbol} ({sys.type})";
            RenderMap(sys);
        }

        private void RenderMap(SystemData sys)
        {
            _mapContainer.Clear();
            _waypointIcons.Clear();

            if (sys.waypoints.Length == 0) return;

            // Calculate bounds
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var wp in sys.waypoints)
            {
                minX = Math.Min(minX, wp.x); maxX = Math.Max(maxX, wp.x);
                minY = Math.Min(minY, wp.y); maxY = Math.Max(maxY, wp.y);
            }

            float rangeX = Math.Max(1, maxX - minX);
            float rangeY = Math.Max(1, maxY - minY);
            float padding = 40f;

            foreach (var wp in sys.waypoints)
            {
                var iconRoot = waypointIconTemplate.Instantiate();
                var icon = iconRoot.Q<VisualElement>("waypoint-root");
                var label = iconRoot.Q<Label>("waypoint-name");

                label.text = wp.symbol;
                icon.AddToClassList($"wp-{wp.type.ToLower()}");

                // Position calculation (Normalized 0-1 then scaled to container)
                // We use schedule to wait for layout if needed, but since it's absolute we can just set percentages
                float posX = ((wp.x - minX) / rangeX) * 90f + 5f; // 5-95% range
                float posY = ((wp.y - minY) / rangeY) * 90f + 5f;

                icon.style.left = Length.Percent(posX);
                icon.style.top = Length.Percent(posY);

                icon.RegisterCallback<ClickEvent>(evt => SelectWaypoint(wp, icon));
                _mapContainer.Add(icon);
                _waypointIcons.Add(icon);
            }
        }

        private void SelectWaypoint(SystemWaypoint wp, VisualElement icon)
        {
            foreach (var i in _waypointIcons) i.RemoveFromClassList("waypoint-selected");
            icon.AddToClassList("waypoint-selected");

            _wpSymbol.text = $"Symbol: {wp.symbol}";
            _wpType.text = $"Type: {wp.type}";
            _wpCoords.text = $"Coordinates: ({wp.x}, {wp.y})";
            _wpInfo.text = GetWaypointDescription(wp.type);
        }

        private string GetWaypointDescription(string type)
        {
            return type switch
            {
                "PLANET" => "A large celestial body orbiting a star.",
                "MOON" => "A natural satellite orbiting a planet.",
                "ORBITAL_STATION" => "A man-made structure in orbit.",
                "JUMP_GATE" => "A fast-travel gateway to other systems.",
                "ASTEROID_FIELD" => "A region dense with space rocks, rich in minerals.",
                "GAS_GIANT" => "A massive planet composed mostly of gases.",
                _ => "Information restricted or unknown."
            };
        }

        private VisualElement BindContract(Contract c)
        {
            var element = contractTemplate.Instantiate();
            element.Q<Label>("id-label").text = $"ID: {c.id}";
            element.Q<Label>("type-label").text = $"Type: {c.type} | Faction: {c.factionSymbol}";
            element.Q<Label>("status-label").text = $"Accepted: {c.accepted} | Fulfilled: {c.fulfilled}";
            return element;
        }

        private VisualElement BindShip(Ship s)
        {
            var element = shipTemplate.Instantiate();
            element.Q<Label>("symbol-label").text = $"Symbol: {s.symbol}";
            element.Q<Label>("details-label").text = $"Role: {s.registration.role} | System: {s.nav.systemSymbol}";
            element.Q<Label>("status-label").text = $"Status: {s.nav.status} | Fuel: {s.fuel.current}/{s.fuel.capacity}";
            return element;
        }

        private VisualElement BindSystem(SystemData sys)
        {
            var element = systemTemplate.Instantiate();
            element.Q<Label>("symbol-label").text = $"Symbol: {sys.symbol}";
            element.Q<Label>("details-label").text = $"Type: {sys.type} | Waypoints: {sys.waypoints.Length}";
            return element;
        }

        private VisualElement BindFaction(Faction f)
        {
            var element = factionTemplate.Instantiate();
            element.Q<Label>("name-label").text = $"Name: {f.name}";
            element.Q<Label>("details-label").text = $"Symbol: {f.symbol} | HQ: {f.headquarters}";
            element.Q<Label>("description-label").text = f.description;
            return element;
        }

        private void AddRow(string key, string value)
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
            _dataContainer.Add(row);
        }
    }
}
