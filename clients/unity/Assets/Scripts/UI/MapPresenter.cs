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
        private VisualElement _labelContainer;
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
        private string _selectedSymbol;

        // Map State for Panning/Zooming
        public Vector2 MapOffset { get; set; } = Vector2.zero;
        public float MapZoom { get; set; } = 1.0f;
        private bool _mapInitialized = false;

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

            // Create Label Layer
            _labelContainer = new VisualElement { style = { position = Position.Absolute, width = Length.Percent(100), height = Length.Percent(100) }, pickingMode = PickingMode.Ignore };
            _mapContainer?.Add(_labelContainer);

            if (_searchField != null)
            {
                _searchField.RegisterValueChangedCallback(evt => FilterSystems(evt.newValue));
            }

            // Register for Vector Content Generation (Grid Rendering)
            if (_mapContainer != null)
            {
                _mapContainer.generateVisualContent += OnGenerateVisualContent;
                _mapContainer.AddManipulator(new MapManipulator(this));
                _mapContainer.RegisterCallback<GeometryChangedEvent>(OnMapContainerResized);
            }

            if (_dbManager != null)
            {
                _allGalaxySystems = _dbManager.GetAllSystems();
                FilterSystems("");
            }

            PopulateLegend();
            ResetMapCamera();

            Log.Info("[MapPresenter] Map panel setup complete.");
        }

        private void OnMapContainerResized(GeometryChangedEvent evt)
        {
            if (!_mapInitialized) ResetMapCamera();
            RefreshMapUI();
        }

        private void ResetMapCamera()
        {
            if (_mapContainer == null) return;
            var rect = _mapContainer.layout;
            if (float.IsNaN(rect.width) || rect.width <= 0) return;

            MapOffset = new Vector2(rect.width / 2f, rect.height / 2f);
            MapZoom = 1.0f;
            _mapInitialized = true;
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
                entry.name = $"list-{s.Symbol}";
                entry.AddToClassList("selectable-entry");
                
                var symbolLabel = entry.Q<Label>("symbol-label");
                var detailsLabel = entry.Q<Label>("details-label");
                
                if (symbolLabel != null) symbolLabel.text = s.Symbol;
                if (detailsLabel != null) detailsLabel.text = $"{s.Type} | {s.X},{s.Y}";
                
                // Remove the "VIEW" button if it exists, and make the whole entry clickable
                var btn = entry.Q<Button>("view-button");
                if (btn != null) btn.style.display = DisplayStyle.None;

                entry.RegisterCallback<ClickEvent>(evt => SelectSystem(s.Symbol, entry));
                
                _systemList.Add(entry);
            }
        }

        private async void SelectSystem(string symbol, VisualElement entry = null)
        {
            Log.Info("[Map] Selecting system {Symbol}...", symbol);
            _selectedSymbol = symbol;

            // Handle UI selection state
            if (_systemList != null)
            {
                foreach (var child in _systemList.Children()) child.RemoveFromClassList("selected-entry");
            }
            entry?.AddToClassList("selected-entry");

            if (_selectedSystemLabel != null) _selectedSystemLabel.text = $"System: {symbol}";
            if (_systemDetailPanel != null) _systemDetailPanel.style.display = DisplayStyle.Flex;
            if (_waypointList != null) _waypointList.Clear();

            try
            {
                var res = await _apiService.GetSystem(symbol);
                if (res != null && res.Data != null)
                {
                    _currentSystem = res.Data;
                    
                    // Focus camera on system (Center system is at its own X,Y but system map view is relative)
                    // For System Map, we usually want to center on (0,0) or the average of waypoints.
                    ResetMapCamera();
                    RefreshMapUI();
                    
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

        public void RefreshMapUI()
        {
            if (_mapContainer == null) return;
            _mapContainer.MarkDirtyRepaint();
            UpdateWaypoints();
        }

        private void UpdateWaypoints()
        {
            var targetLayer = _waypointsLayer ?? _mapContainer;
            if (targetLayer == null || _labelContainer == null) return;
            
            targetLayer.Clear();
            _labelContainer.Clear();

            if (_currentSystem == null) return;

            float scale = 5f; // Base scale for system coordinates

            foreach (var wp in _currentSystem.Waypoints)
            {
                if (waypointIconTemplate == null) continue;

                Vector2 basePos = new Vector2(wp.X, wp.Y) * scale;
                Vector2 screenPos = basePos * MapZoom + MapOffset;

                var icon = waypointIconTemplate.Instantiate();
                icon.style.position = Position.Absolute;
                icon.style.left = screenPos.x;
                icon.style.top = screenPos.y;
                
                // Waypoint Type Colors (Adding classes defined in MainStyle.uss)
                icon.AddToClassList($"wp-{wp.Type.ToString().ToLower()}");

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

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            var rect = _mapContainer.contentRect;

            // Grid Rendering Logic
            float logZoom = Mathf.Log10(50f / MapZoom);
            float floorLog = Mathf.Floor(logZoom);
            float majorScale = Mathf.Pow(10, floorLog + 1);
            float minorScale = Mathf.Pow(10, floorLog);
            
            float majorSize = majorScale * MapZoom;
            float minorSize = minorScale * MapZoom;

            // Draw Minor Grid
            DrawLines(painter, rect, minorSize, new Color(0.2f, 0.2f, 0.2f, 0.1f));
            // Draw Major Grid
            DrawLines(painter, rect, majorSize, new Color(0.4f, 0.4f, 0.4f, 0.2f));
        }

        private void DrawLines(Painter2D painter, Rect rect, float size, Color color)
        {
            if (size <= 1f) return;

            painter.strokeColor = color;
            painter.lineWidth = 1f;
            painter.BeginPath();

            float startX = MapOffset.x % size;
            if (startX < 0) startX += size;
            for (float x = startX; x < rect.width; x += size)
            {
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, rect.height));
            }

            float startY = MapOffset.y % size;
            if (startY < 0) startY += size;
            for (float y = startY; y < rect.height; y += size)
            {
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(rect.width, y));
            }
            painter.Stroke();
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
                
                // Approximate color based on type (Logic from original MapPresenter)
                bullet.style.backgroundColor = GetWaypointColor(type);
                bullet.style.borderTopLeftRadius = 4; bullet.style.borderTopRightRadius = 4;
                bullet.style.borderBottomLeftRadius = 4; bullet.style.borderBottomRightRadius = 4;
                
                var label = new Label(type.Replace("_", " ")) { style = { fontSize = 9, color = Color.gray }};
                row.Add(bullet);
                row.Add(label);
                _legendItems.Add(row);
            }
        }

        private Color GetWaypointColor(string type) => type switch {
            "PLANET" => new Color(0, 0.6f, 1f),
            "MOON" => Color.gray,
            "ORBITAL_STATION" => Color.yellow,
            "JUMP_GATE" => new Color(0.8f, 0, 1f),
            "ASTEROID_FIELD" => new Color(0.4f, 0.3f, 0.2f),
            _ => Color.white
        };

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

        // --- Inner Classes ---

        private class MapManipulator : Manipulator
        {
            private readonly MapPresenter _presenter;
            private bool _active;
            private Vector2 _lastMousePos;

            public MapManipulator(MapPresenter presenter)
            {
                _presenter = presenter;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerDownEvent>(OnPointerDown);
                target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                target.RegisterCallback<PointerUpEvent>(OnPointerUp);
                target.RegisterCallback<WheelEvent>(OnWheel);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                target.UnregisterCallback<WheelEvent>(OnWheel);
            }

            private void OnPointerDown(PointerDownEvent evt)
            {
                if (evt.button == 1 || evt.button == 2) // Right or Middle click for pan
                {
                    _active = true;
                    _lastMousePos = evt.localPosition;
                    target.CapturePointer(evt.pointerId);
                    evt.StopPropagation();
                }
            }

            private void OnPointerMove(PointerMoveEvent evt)
            {
                if (_active)
                {
                    Vector2 delta = (Vector2)evt.localPosition - _lastMousePos;
                    _presenter.MapOffset += delta;
                    _presenter.RefreshMapUI();
                    _lastMousePos = evt.localPosition;
                    evt.StopPropagation();
                }
            }

            private void OnPointerUp(PointerUpEvent evt)
            {
                if (_active && (evt.button == 1 || evt.button == 2))
                {
                    _active = false;
                    target.ReleasePointer(evt.pointerId);
                    evt.StopPropagation();
                }
            }

            private void OnWheel(WheelEvent evt)
            {
                float delta = -evt.delta.y * 0.1f;
                float oldZoom = _presenter.MapZoom;
                _presenter.MapZoom = Mathf.Clamp(_presenter.MapZoom * (1f + delta), 0.1f, 10f);
                
                Vector2 mousePos = evt.localMousePosition;
                Vector2 worldPos = (mousePos - _presenter.MapOffset) / oldZoom;
                _presenter.MapOffset = mousePos - (worldPos * _presenter.MapZoom);
                
                _presenter.RefreshMapUI();
                evt.StopPropagation();
            }
        }
    }
}
