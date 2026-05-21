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
        public VisualTreeAsset systemPanelTemplate;

        [Header("UI References")]
        private VisualElement _systemList;
        private VisualElement _mapContainer;
        private VisualElement _waypointsLayer;
        private TextField _searchField;
        private Label _selectedSystemLabel;
        private VisualElement _systemDetailPanel;
        private ScrollView _waypointList;
        private VisualElement _legendItems;

        private List<DatabaseManager.IndexedSystem> _allGalaxySystems;
        private List<DatabaseManager.IndexedSystem> _filteredSystems;

        private DatabaseManager _dbManager;
        private APIService _apiService;

        private SpaceTraders.Generated.Model.System _currentSystem;

        [Inject]
        public void Construct(DatabaseManager dbManager, APIService apiService)
        {
            _dbManager = dbManager;
            _apiService = apiService;
        }

        public void SetupMapPanel(VisualElement container)
        {
            if (systemPanelTemplate == null)
            {
                container.Add(new Label("Error: System Panel Template missing."));
                return;
            }

            container.Clear();
            var panel = systemPanelTemplate.Instantiate();
            panel.style.flexGrow = 1;
            container.Add(panel);

            // Bind references from the instantiated panel
            _systemList = panel.Q<VisualElement>("system-list");
            _mapContainer = panel.Q<VisualElement>("map-container");
            _waypointsLayer = panel.Q<VisualElement>("waypoints-layer");
            _searchField = panel.Q<TextField>("system-search");
            _selectedSystemLabel = panel.Q<Label>("selected-system-title");
            _systemDetailPanel = panel.Q<VisualElement>("waypoint-details");
            _waypointList = panel.Q<ScrollView>("wp-extra-scroll");
            _legendItems = panel.Q<VisualElement>("legend-items");

            if (_searchField != null)
            {
                _searchField.RegisterValueChangedCallback(evt => FilterSystems(evt.newValue));
            }

            // Deferred map rendering after layout is calculated
            _mapContainer?.RegisterCallback<GeometryChangedEvent>(OnMapContainerResized);

            if (_dbManager != null)
            {
                _allGalaxySystems = _dbManager.GetAllSystems();
                FilterSystems("");
            }

            PopulateLegend();

            Log.Info("[MapPresenter] Map panel setup complete.");
        }

        private void OnMapContainerResized(GeometryChangedEvent evt)
        {
            if (_currentSystem != null)
            {
                UpdateMap(_currentSystem);
            }
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
                if (btn != null)
                {
                    btn.clicked += () => SelectSystem(s.Symbol);
                }
                
                _systemList.Add(entry);
            }
        }

        private async void SelectSystem(string symbol)
        {
            Log.Info("[Map] Selecting system {Symbol}...", symbol);
            if (_selectedSystemLabel != null) _selectedSystemLabel.text = $"System: {symbol}";
            if (_systemDetailPanel != null) _systemDetailPanel.style.display = DisplayStyle.Flex;
            if (_waypointList != null) _waypointList.Clear();

            try
            {
                var res = await _apiService.GetSystem(symbol);
                if (res != null && res.Data != null)
                {
                    _currentSystem = res.Data;
                    UpdateMap(_currentSystem);
                    
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
            var targetLayer = _waypointsLayer ?? _mapContainer;
            if (targetLayer == null) return;
            
            // Check if layout is ready
            if (float.IsNaN(targetLayer.layout.width) || targetLayer.layout.width <= 0)
            {
                Log.Warning("[Map] Map layout not ready. Rendering deferred.");
                return;
            }

            targetLayer.Clear();
            
            float scale = 5f;
            float centerX = targetLayer.layout.width / 2;
            float centerY = targetLayer.layout.height / 2;

            Log.Info("[Map] Rendering {Count} waypoints for {System}", system.Waypoints.Count, system.Symbol);

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
                    tooltip.style.display = DisplayStyle.None;
                    icon.RegisterCallback<MouseEnterEvent>(evt => tooltip.style.display = DisplayStyle.Flex);
                    icon.RegisterCallback<MouseLeaveEvent>(evt => tooltip.style.display = DisplayStyle.None);
                }

                targetLayer.Add(icon);
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

        private void PopulateLegend()
        {
            if (_legendItems == null) return;
            _legendItems.Clear();

            var types = Enum.GetNames(typeof(WaypointType));
            foreach (var type in types)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 }};
                var bullet = new VisualElement { style = { width = 8, height = 8, backgroundColor = Color.cyan, marginRight = 5 }};
                bullet.style.borderTopLeftRadius = 4; bullet.style.borderTopRightRadius = 4;
                bullet.style.borderBottomLeftRadius = 4; bullet.style.borderBottomRightRadius = 4;
                
                var label = new Label(type.Replace("_", " ")) { style = { fontSize = 9, color = Color.gray }};
                row.Add(bullet);
                row.Add(label);
                _legendItems.Add(row);
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
