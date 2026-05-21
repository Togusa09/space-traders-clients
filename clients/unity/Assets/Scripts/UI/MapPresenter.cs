using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;
using SpaceTraders.Core;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.UI
{
    public class MapPresenter : MonoBehaviour
    {
        [Header("Templates")]
        public VisualTreeAsset systemEntryTemplate;
        public VisualTreeAsset waypointIconTemplate;

        [Header("UI References")]
        private VisualElement _systemList;
        private VisualElement _mapContainer;
        private TextField _searchField;
        private Label _selectedSystemLabel;
        private VisualElement _systemDetailPanel;
        private ScrollView _waypointList;

        private List<DatabaseManager.IndexedSystem> _allGalaxySystems;
        private List<DatabaseManager.IndexedSystem> _filteredSystems;

        private DatabaseManager _dbManager;
        private APIService _apiService;

        [Inject]
        public void Construct(DatabaseManager dbManager, APIService apiService)
        {
            _dbManager = dbManager;
            _apiService = apiService;
        }

        private void Start()
        {
            InitializeUI();
            
            if (_dbManager != null)
            {
                _allGalaxySystems = _dbManager.GetAllSystems();
                FilterSystems("");
            }
        }

        private void InitializeUI()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;
            
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            // Mapping to Panel_Systems.uxml (kebab-case)
            _systemList = root.Q<VisualElement>("system-list");
            _mapContainer = root.Q<VisualElement>("map-container");
            _searchField = root.Q<TextField>("system-search");
            _selectedSystemLabel = root.Q<Label>("selected-system-title");
            _systemDetailPanel = root.Q<VisualElement>("waypoint-details");
            _waypointList = root.Q<ScrollView>("wp-extra-scroll");

            if (_searchField != null)
            {
                _searchField.RegisterValueChangedCallback(evt => FilterSystems(evt.newValue));
            }
            
            Log.Info("[MapPresenter] UI initialized.");
        }

        private void FilterSystems(string query)
        {
            if (_allGalaxySystems == null) return;

            _filteredSystems = string.IsNullOrEmpty(query) 
                ? _allGalaxySystems.Take(50).ToList() 
                : _allGalaxySystems.Where(s => s.Symbol.Contains(query.ToUpper())).Take(50).ToList();
            
            PopulateSystemList();
        }

        private void PopulateSystemList()
        {
            if (_systemList == null) return;
            _systemList.Clear();

            foreach (var s in _filteredSystems)
            {
                if (systemEntryTemplate == null) continue;
                var entry = systemEntryTemplate.Instantiate();
                
                var symbolLabel = entry.Q<Label>("symbol-label");
                var detailsLabel = entry.Q<Label>("details-label");
                
                if (symbolLabel != null) symbolLabel.text = s.Symbol;
                if (detailsLabel != null) detailsLabel.text = $"{s.Type} | {s.X},{s.Y}";
                
                var btn = entry.Q<Button>("view-button") ?? entry.Q<Button>();
                if (btn != null) btn.clicked += () => SelectSystem(s.Symbol);
                
                _systemList.Add(entry);
            }
        }

        private async void SelectSystem(string symbol)
        {
            if (_selectedSystemLabel != null) _selectedSystemLabel.text = $"System: {symbol}";
            if (_systemDetailPanel != null) _systemDetailPanel.style.display = DisplayStyle.Flex;
            if (_waypointList != null) _waypointList.Clear();

            try
            {
                var res = await _apiService.GetSystem(symbol);
                if (res != null && res.Data != null)
                {
                    UpdateMap(res.Data);
                    
                    var wpsRes = await _apiService.GetSystemWaypoints(symbol);
                    if (wpsRes != null && wpsRes.Data != null)
                    {
                        PopulateWaypointList(symbol, wpsRes.Data.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("[Map] Failed to select system {System}: {Error}", symbol, e.Message);
            }
        }

        private void UpdateMap(SpaceTraders.Generated.Model.System system)
        {
            if (_mapContainer == null) return;
            _mapContainer.Clear();
            
            float scale = 5f;
            float centerX = _mapContainer.layout.width / 2;
            float centerY = _mapContainer.layout.height / 2;

            foreach (var wp in system.Waypoints)
            {
                if (waypointIconTemplate == null) continue;
                var icon = waypointIconTemplate.Instantiate();
                icon.style.position = Position.Absolute;
                icon.style.left = centerX + (wp.X * scale);
                icon.style.top = centerY - (wp.Y * scale);
                
                var tooltip = icon.Q<Label>("waypoint-name") ?? icon.Q<Label>("Tooltip") ?? icon.Q<Label>("tooltip-label");
                if (tooltip != null)
                {
                    tooltip.text = wp.Symbol;
                    tooltip.style.display = DisplayStyle.None; // Hidden by default
                    icon.RegisterCallback<MouseEnterEvent>(evt => tooltip.style.display = DisplayStyle.Flex);
                    icon.RegisterCallback<MouseLeaveEvent>(evt => tooltip.style.display = DisplayStyle.None);
                }

                _mapContainer.Add(icon);
            }
        }

        private void PopulateWaypointList(string systemSymbol, Waypoint[] waypoints)
        {
            if (_waypointList == null) return;
            _waypointList.Clear();

            foreach (var wp in waypoints)
            {
                var container = new VisualElement { style = { 
                    flexDirection = FlexDirection.Row, 
                    justifyContent = Justify.SpaceBetween,
                    paddingBottom = 4,
                    borderBottomWidth = 1,
                    borderBottomColor = Color.gray
                }};

                var info = new Label($"{wp.Symbol} ({wp.Type}) @ {wp.X},{wp.Y}");
                container.Add(info);

                var actions = new VisualElement { style = { flexDirection = FlexDirection.Row }};
                
                var btnInspect = new Button { text = "INSPECT" };
                btnInspect.clicked += () => InspectWaypoint(systemSymbol, wp.Symbol);
                actions.Add(btnInspect);

                container.Add(actions);
                _waypointList.Add(container);
            }
        }

        private async void InspectWaypoint(string systemSymbol, string wpSymbol)
        {
            Log.Info("[Map] Inspecting {Waypoint}...", wpSymbol);
            try
            {
                var marketTask = _apiService.GetMarket(systemSymbol, wpSymbol);
                var shipyardTask = _apiService.GetShipyard(systemSymbol, wpSymbol);
                var constructTask = _apiService.GetConstruction(systemSymbol, wpSymbol);

                await Task.WhenAll(marketTask, shipyardTask, constructTask);

                if (marketTask.Result?.Data != null) Log.Info("[Map] [Market] {Count} exports found at {Waypoint}", marketTask.Result.Data.Exports.Count, wpSymbol);
                if (shipyardTask.Result?.Data != null) Log.Info("[Map] [Shipyard] {Count} ships available at {Waypoint}", shipyardTask.Result.Data.Ships.Count, wpSymbol);
            }
            catch { /* Not all waypoints have all services */ }
        }
    }
}
