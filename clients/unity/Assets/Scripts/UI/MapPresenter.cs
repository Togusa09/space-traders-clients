using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using SpaceTraders.Core;
using VContainer;

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

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _systemList = root.Q<VisualElement>("SystemList");
            _mapContainer = root.Q<VisualElement>("MapVisualContainer");
            _searchField = root.Q<TextField>("SystemSearch");
            _selectedSystemLabel = root.Q<Label>("SelectedSystemName");
            _systemDetailPanel = root.Q<VisualElement>("SystemDetailPanel");
            _waypointList = root.Q<ScrollView>("WaypointList");

            _searchField.RegisterValueChangedCallback(evt => FilterSystems(evt.newValue));

            _allGalaxySystems = _dbManager.GetAllSystems();
            FilterSystems("");
        }

        private void FilterSystems(string query)
        {
            _filteredSystems = string.IsNullOrEmpty(query) 
                ? _allGalaxySystems.Take(50).ToList() 
                : _allGalaxySystems.Where(s => s.Symbol.Contains(query.ToUpper())).Take(50).ToList();
            
            PopulateSystemList();
        }

        private void PopulateSystemList()
        {
            _systemList.Clear();
            foreach (var s in _filteredSystems)
            {
                var entry = systemEntryTemplate.Instantiate();
                entry.Q<Label>("SysSymbol").text = s.Symbol;
                entry.Q<Label>("SysDetails").text = $"{s.Type} | {s.X},{s.Y}";
                
                var btn = entry.Q<Button>("BtnView");
                btn.clicked += () => SelectSystem(s.Symbol);
                
                _systemList.Add(entry);
            }
        }

        private async void SelectSystem(string symbol)
        {
            _selectedSystemLabel.text = $"System: {symbol}";
            _systemDetailPanel.style.display = DisplayStyle.Flex;
            _waypointList.Clear();

            try
            {
                var res = await _apiService.GetSystem(symbol);
                if (res != null)
                {
                    UpdateMap(res.data);
                    
                    var wpsRes = await _apiService.GetSystemWaypoints(symbol);
                    if (wpsRes != null)
                    {
                        PopulateWaypointList(symbol, wpsRes.data);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Map] Failed to select system {symbol}: {e.Message}");
            }
        }

        private void UpdateMap(SystemData system)
        {
            _mapContainer.Clear();
            
            // Normalize coordinates for the container
            // (Rough implementation: find min/max or use a fixed scale)
            float scale = 5f;
            float centerX = _mapContainer.layout.width / 2;
            float centerY = _mapContainer.layout.height / 2;

            foreach (var wp in system.waypoints)
            {
                var icon = waypointIconTemplate.Instantiate();
                icon.style.position = Position.Absolute;
                icon.style.left = centerX + (wp.x * scale);
                icon.style.top = centerY - (wp.y * scale);
                
                var tooltip = icon.Q<Label>("Tooltip");
                tooltip.text = wp.symbol;
                icon.RegisterCallback<MouseEnterEvent>(evt => tooltip.style.display = DisplayStyle.Flex);
                icon.RegisterCallback<MouseLeaveEvent>(evt => tooltip.style.display = DisplayStyle.None);

                _mapContainer.Add(icon);
            }
        }

        private void PopulateWaypointList(string systemSymbol, SystemWaypoint[] waypoints)
        {
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

                var info = new Label($"{wp.symbol} ({wp.type}) @ {wp.x},{wp.y}");
                container.Add(info);

                var actions = new VisualElement { style = { flexDirection = FlexDirection.Row }};
                
                var btnInspect = new Button { text = "Inspect" };
                btnInspect.clicked += () => InspectWaypoint(systemSymbol, wp.symbol);
                actions.Add(btnInspect);

                container.Add(actions);
                _waypointList.Add(container);
            }
        }

        private async void InspectWaypoint(string systemSymbol, string wpSymbol)
        {
            // Implementation for Market/Shipyard/Construction inspection
            Debug.Log($"Inspecting {wpSymbol}...");
            try
            {
                // Parallel fetch of available data
                var marketTask = _apiService.GetMarket(systemSymbol, wpSymbol);
                var shipyardTask = _apiService.GetShipyard(systemSymbol, wpSymbol);
                var constructTask = _apiService.GetConstruction(systemSymbol, wpSymbol);

                await Task.WhenAll(marketTask, shipyardTask, constructTask);

                // For simplicity, just log what we found
                if (marketTask.Result?.data != null) Debug.Log($"[Market] {marketTask.Result.data.exports.Length} exports");
                if (shipyardTask.Result?.data != null) Debug.Log($"[Shipyard] {shipyardTask.Result.data.ships.Length} ships available");
            }
            catch { /* Not all waypoints have all services */ }
        }
    }
}
