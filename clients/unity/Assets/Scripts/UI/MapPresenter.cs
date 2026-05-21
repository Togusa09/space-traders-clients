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
        private enum MapMode
        {
            Galaxy,
            System
        }

        [Header("Templates")]
        public VisualTreeAsset systemEntryTemplate;
        public VisualTreeAsset waypointIconTemplate;
        public VisualTreeAsset systemPanelTemplate;

        [Header("UI References")]
        private VisualElement _systemList;
        private VisualElement _mapContainer;
        private VisualElement _waypointsLayer;
        private VisualElement _legendContent;
        private VisualElement _labelContainer;
        private TextField _searchField;
        private Label _selectedSystemLabel;
        private VisualElement _systemDetailPanel;
        private ScrollView _waypointList;
        private VisualElement _legendItems;
        private Button _viewGalaxyButton;
        private Button _prevPageButton;
        private Button _nextPageButton;
        private Label _pageInfoLabel;
        private Button _legendToggleButton;
        private Label _wpSymbolLabel;
        private Label _wpTypeLabel;
        private Label _wpCoordsLabel;
        private Label _wpDescLabel;
        private Label _extraInfoTitleLabel;
        private VisualElement _extraContentContainer;

        private List<DatabaseManager.IndexedSystem> _allGalaxySystems;
        private List<DatabaseManager.IndexedSystem> _filteredSystems;
        private List<DatabaseManager.IndexedSystem> _pagedSystems;
        private readonly List<VisualElement> _listEntries = new List<VisualElement>();

        private DatabaseManager _dbManager;
        private APIService _apiService;

        private SpaceTraders.Generated.Model.System _currentSystem;
        private string _selectedSymbol;
        private Waypoint _selectedWaypoint;

        // Map State for Panning/Zooming
        public Vector2 MapOffset { get; set; } = Vector2.zero;
        public float MapZoom { get; set; } = 1.0f;
        private bool _mapInitialized = false;
        private float _minZoom = 0.01f;
        private float _maxZoom = 1000f;
        private MapMode _mapMode = MapMode.Galaxy;
        private int _currentPage = 1;
        private const int PageSize = 50;
        private bool _legendExpanded = true;
        private const float GalaxyScale = 6f;
        private const float SystemScale = 5f;

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
            _legendContent = panel.Q<VisualElement>("legend-content");
            _viewGalaxyButton = panel.Q<Button>("view-galaxy-btn");
            _prevPageButton = panel.Q<Button>("prev-page");
            _nextPageButton = panel.Q<Button>("next-page");
            _pageInfoLabel = panel.Q<Label>("page-info");
            _legendToggleButton = panel.Q<Button>("legend-toggle");
            _wpSymbolLabel = panel.Q<Label>("wp-symbol");
            _wpTypeLabel = panel.Q<Label>("wp-type");
            _wpCoordsLabel = panel.Q<Label>("wp-coords");
            _wpDescLabel = panel.Q<Label>("wp-desc");
            _extraInfoTitleLabel = panel.Q<Label>("extra-info-title");
            _extraContentContainer = panel.Q<VisualElement>("extra-content-container");

            // Create Label Layer
            _labelContainer = new VisualElement { style = { position = Position.Absolute, width = Length.Percent(100), height = Length.Percent(100) }, pickingMode = PickingMode.Ignore };
            _mapContainer?.Add(_labelContainer);

            if (_searchField != null)
            {
                _searchField.RegisterValueChangedCallback(evt => ApplySystemFilter(evt.newValue));
            }

            if (_viewGalaxyButton != null)
            {
                _viewGalaxyButton.clicked += ToggleMapMode;
            }

            if (_prevPageButton != null)
            {
                _prevPageButton.clicked += () => ChangePage(-1);
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.clicked += () => ChangePage(1);
            }

            if (_legendToggleButton != null)
            {
                _legendToggleButton.clicked += ToggleLegend;
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
            }

            ApplySystemFilter(_searchField != null ? _searchField.value : string.Empty);
            _ = EnsureGalaxySystemsLoadedAsync();

            RefreshLegend();
            UpdateModeChrome();
            ResetMapCamera();
            RefreshMapUI();

            Log.Info("[MapPresenter] Map panel setup complete.");
        }

        private void OnMapContainerResized(GeometryChangedEvent evt)
        {
            if (!_mapInitialized) ResetMapCamera();
            RefreshMapUI();
        }

        private void ToggleMapMode()
        {
            if (_mapMode == MapMode.System)
            {
                _mapMode = MapMode.Galaxy;
            }
            else if (!string.IsNullOrEmpty(_selectedSymbol))
            {
                _mapMode = MapMode.System;
            }

            UpdateModeChrome();
            PopulateSystemList();
            ResetMapCamera();
            RefreshMapUI();
        }

        private void ChangePage(int delta)
        {
            if (_filteredSystems == null || _filteredSystems.Count == 0)
            {
                return;
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_filteredSystems.Count / PageSize));
            _currentPage = Mathf.Clamp(_currentPage + delta, 1, totalPages);
            PopulateSystemList();
            RefreshLegend();

            if (_mapMode == MapMode.Galaxy)
            {
                ResetMapCamera();
                RefreshMapUI();
            }
        }

        private void ToggleLegend()
        {
            _legendExpanded = !_legendExpanded;

            if (_legendContent != null)
            {
                _legendContent.style.display = _legendExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_legendToggleButton != null)
            {
                _legendToggleButton.text = _legendExpanded ? "-" : "+";
            }
        }

        private void ApplySystemFilter(string query)
        {
            if (_allGalaxySystems == null)
            {
                _filteredSystems = new List<DatabaseManager.IndexedSystem>();
                PopulateSystemList();
                return;
            }

            _filteredSystems = string.IsNullOrEmpty(query)
                ? _allGalaxySystems.ToList()
                : _allGalaxySystems.Where(s => s.Symbol.Contains(query.ToUpper())).ToList();

            _currentPage = 1;
            PopulateSystemList();
            RefreshLegend();
        }

        private async Task EnsureGalaxySystemsLoadedAsync()
        {
            if (_allGalaxySystems != null && _allGalaxySystems.Count > 0)
            {
                return;
            }

            if (_apiService == null)
            {
                return;
            }

            var loadedSystems = new List<DatabaseManager.IndexedSystem>();
            int page = 1;
            const int pageSize = 100;

            try
            {
                while (true)
                {
                    var response = await _apiService.GetSystems(page, pageSize);
                    if (response?.Data == null || response.Data.Count == 0)
                    {
                        break;
                    }

                    loadedSystems.AddRange(response.Data.Select(system => new DatabaseManager.IndexedSystem
                    {
                        Symbol = system.Symbol,
                        SectorSymbol = system.SectorSymbol,
                        Type = system.Type.ToString(),
                        X = system.X,
                        Y = system.Y,
                        WaypointCount = system.Waypoints != null ? system.Waypoints.Count : 0
                    }));

                    if (response.Meta == null || loadedSystems.Count >= response.Meta.Total || response.Data.Count < pageSize)
                    {
                        break;
                    }

                    page++;
                }

                if (loadedSystems.Count > 0)
                {
                    _allGalaxySystems = loadedSystems;
                    _dbManager?.StoreSystems(loadedSystems);
                    ApplySystemFilter(_searchField != null ? _searchField.value : string.Empty);
                    if (_mapMode == MapMode.Galaxy)
                    {
                        ResetMapCamera();
                    }
                    RefreshMapUI();
                }
            }
            catch (Exception e)
            {
                Log.Warning("[MapPresenter] Galaxy system backfill failed: {Error}", e.Message);
            }
        }

        private void ResetMapCamera()
        {
            if (_mapContainer == null) return;
            var rect = _mapContainer.contentRect;
            if (float.IsNaN(rect.width) || rect.width <= 0) return;

            ConfigureZoomLimits();

            if (_mapMode == MapMode.Galaxy)
            {
                var systems = _filteredSystems;
                if (systems != null && systems.Count > 0)
                {
                    FitBounds(systems.Select(s => new Vector2(s.X * GalaxyScale, s.Y * GalaxyScale)), rect);
                }
                else
                {
                    MapOffset = new Vector2(rect.width / 2f, rect.height / 2f);
                    MapZoom = Mathf.Clamp(1.0f, _minZoom, _maxZoom);
                }
            }
            else
            {
                if (_currentSystem?.Waypoints != null && _currentSystem.Waypoints.Count > 0)
                {
                    FitBounds(_currentSystem.Waypoints.Select(w => new Vector2(w.X * SystemScale, w.Y * SystemScale)), rect);
                }
                else
                {
                    MapOffset = new Vector2(rect.width / 2f, rect.height / 2f);
                    MapZoom = Mathf.Clamp(1.0f, _minZoom, _maxZoom);
                }
            }

            _mapInitialized = true;
        }

        private void ConfigureZoomLimits()
        {
            if (_mapMode == MapMode.Galaxy)
            {
                _minZoom = 0.001f;
                _maxZoom = 250f;
            }
            else
            {
                _minZoom = 0.02f;
                _maxZoom = 500f;
            }

            MapZoom = Mathf.Clamp(MapZoom, _minZoom, _maxZoom);
        }

        private void FitBounds(IEnumerable<Vector2> points, Rect rect)
        {
            var pointList = points.ToList();
            if (pointList.Count == 0)
            {
                MapOffset = new Vector2(rect.width / 2f, rect.height / 2f);
                MapZoom = Mathf.Clamp(1.0f, _minZoom, _maxZoom);
                return;
            }

            float minX = pointList.Min(p => p.x);
            float maxX = pointList.Max(p => p.x);
            float minY = pointList.Min(p => p.y);
            float maxY = pointList.Max(p => p.y);

            float boundsWidth = Mathf.Max(1f, maxX - minX);
            float boundsHeight = Mathf.Max(1f, maxY - minY);
            float zoomX = (rect.width * 0.8f) / boundsWidth;
            float zoomY = (rect.height * 0.8f) / boundsHeight;

            MapZoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), _minZoom, _maxZoom);

            var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            MapOffset = new Vector2(rect.width * 0.5f, rect.height * 0.5f) - (center * MapZoom);
        }

        private void PopulateSystemList()
        {
            if (_systemList == null) return;
            _systemList.Clear();
            _listEntries.Clear();

            if (_mapMode == MapMode.System)
            {
                PopulateWaypointHierarchyList();
                return;
            }

            if (_filteredSystems == null)
            {
                UpdatePageInfo(0, 0);
                return;
            }

            if (_filteredSystems.Count == 0)
            {
                _pagedSystems = new List<DatabaseManager.IndexedSystem>();
                UpdatePageInfo(0, 0);
                return;
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_filteredSystems.Count / PageSize));
            _currentPage = Mathf.Clamp(_currentPage, 1, totalPages);
            _pagedSystems = _filteredSystems.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            UpdatePageInfo(_currentPage, totalPages);

            foreach (var s in _pagedSystems)
            {
                if (systemEntryTemplate == null) continue;
                var entry = systemEntryTemplate.Instantiate();
                var root = entry.Q<VisualElement>(null, "dashboard-entry") ?? entry;
                root.name = $"list-{s.Symbol}";
                root.AddToClassList("selectable-entry");
                
                var symbolLabel = entry.Q<Label>("symbol-label");
                var detailsLabel = entry.Q<Label>("details-label");
                
                if (symbolLabel != null) symbolLabel.text = s.Symbol;
                if (detailsLabel != null) detailsLabel.text = $"{s.Type} | {s.X},{s.Y}";

                if (s.Symbol == _selectedSymbol)
                {
                    root.AddToClassList("selected-entry");
                }

                root.RegisterCallback<ClickEvent>(evt => SelectGalaxySystem(s, root));
                
                _systemList.Add(entry);
                _listEntries.Add(root);
            }
        }

        private void SelectGalaxySystem(DatabaseManager.IndexedSystem system, VisualElement entry = null)
        {
            if (system == null) return;

            _selectedSymbol = system.Symbol;
            _selectedWaypoint = null;

            foreach (var listEntry in _listEntries)
            {
                listEntry.RemoveFromClassList("selected-entry");
            }

            entry ??= _systemList?.Q<VisualElement>($"list-{system.Symbol}");
            entry?.AddToClassList("selected-entry");

            if (_selectedSystemLabel != null)
            {
                _selectedSystemLabel.text = $"System: {system.Symbol}";
            }

            if (_wpSymbolLabel != null) _wpSymbolLabel.text = system.Symbol;
            if (_wpTypeLabel != null) _wpTypeLabel.text = system.Type;
            if (_wpCoordsLabel != null) _wpCoordsLabel.text = $"{system.X}, {system.Y}";
            if (_wpDescLabel != null) _wpDescLabel.text = "Selected system. Use OPEN SYSTEM to view waypoints.";

            if (_extraInfoTitleLabel != null) _extraInfoTitleLabel.text = "Actions";
            if (_extraContentContainer != null)
            {
                _extraContentContainer.Clear();
                var openButton = new Button(() => SelectSystem(system.Symbol, entry)) { text = "OPEN SYSTEM" };
                openButton.AddToClassList("button");
                openButton.style.width = 150;
                openButton.style.height = 30;
                _extraContentContainer.Add(openButton);
            }

            UpdateModeChrome();
            RefreshMapUI();
        }

        private void PopulateWaypointHierarchyList()
        {
            var waypoints = _currentSystem?.Waypoints;
            if (waypoints == null || waypoints.Count == 0)
            {
                UpdatePageInfo(0, 0);
                return;
            }

            UpdatePageInfo(1, 1);

            string search = (_searchField?.value ?? string.Empty).Trim().ToUpperInvariant();
            var childMap = waypoints
                .GroupBy(w => string.IsNullOrEmpty(w.Orbits) ? string.Empty : w.Orbits)
                .ToDictionary(g => g.Key, g => g.OrderBy(w => w.Symbol).ToList());

            if (!childMap.TryGetValue(string.Empty, out var roots))
            {
                roots = waypoints.Where(w => string.IsNullOrEmpty(w.Orbits)).OrderBy(w => w.Symbol).ToList();
            }

            foreach (var root in roots)
            {
                if (!WaypointMatches(root, childMap, search)) continue;
                AddWaypointEntry(root, 0);
                AddWaypointChildren(root, 1, childMap, search);
            }
        }

        private void AddWaypointChildren(SystemWaypoint parent, int indent, Dictionary<string, List<SystemWaypoint>> childMap, string search)
        {
            if (parent == null || !childMap.TryGetValue(parent.Symbol, out var children))
            {
                return;
            }

            foreach (var child in children)
            {
                if (!WaypointMatches(child, childMap, search)) continue;
                AddWaypointEntry(child, indent);
                AddWaypointChildren(child, indent + 1, childMap, search);
            }
        }

        private bool WaypointMatches(SystemWaypoint waypoint, Dictionary<string, List<SystemWaypoint>> childMap, string search)
        {
            if (waypoint == null) return false;
            if (string.IsNullOrEmpty(search)) return true;
            if (!string.IsNullOrEmpty(waypoint.Symbol) && waypoint.Symbol.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;

            if (!childMap.TryGetValue(waypoint.Symbol, out var children))
            {
                return false;
            }

            return children.Any(child => WaypointMatches(child, childMap, search));
        }

        private void AddWaypointEntry(SystemWaypoint waypoint, int indent)
        {
            if (waypoint == null || systemEntryTemplate == null || _systemList == null)
            {
                return;
            }

            var entry = systemEntryTemplate.Instantiate();
            var root = entry.Q<VisualElement>(null, "dashboard-entry") ?? entry;
            root.name = $"list-{waypoint.Symbol}";
            root.AddToClassList("selectable-entry");
            root.style.marginLeft = indent * 12;

            var symbolLabel = entry.Q<Label>("symbol-label");
            var detailsLabel = entry.Q<Label>("details-label");
            if (symbolLabel != null)
            {
                symbolLabel.text = (indent > 0 ? "↳ " : string.Empty) + waypoint.Symbol;
            }

            if (detailsLabel != null)
            {
                detailsLabel.text = waypoint.Type.ToString().Replace("_", " ");
            }

            if (waypoint.Symbol == _selectedSymbol)
            {
                root.AddToClassList("selected-entry");
            }

            root.RegisterCallback<ClickEvent>(_ => SelectSystemWaypoint(waypoint));

            _systemList.Add(entry);
            _listEntries.Add(root);
        }

        private void UpdatePageInfo(int currentPage, int totalPages)
        {
            if (_pageInfoLabel != null)
            {
                _pageInfoLabel.text = totalPages <= 0 ? "0/0" : $"{currentPage}/{totalPages}";
            }

            if (_prevPageButton != null)
            {
                _prevPageButton.SetEnabled(currentPage > 1);
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.SetEnabled(totalPages > 0 && currentPage < totalPages);
            }
        }

        private async void SelectSystem(string symbol, VisualElement entry = null)
        {
            Log.Info("[Map] Selecting system {Symbol}...", symbol);
            _selectedSymbol = symbol;

            // Handle UI selection state
            foreach (var listEntry in _listEntries)
            {
                listEntry.RemoveFromClassList("selected-entry");
            }

            entry ??= _systemList?.Q<VisualElement>($"list-{symbol}");
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
                    _mapMode = MapMode.System;
                    _selectedWaypoint = null;
                    
                    if (res.Data.Waypoints != null && res.Data.Waypoints.Count > 0)
                    {
                        SelectSystemWaypoint(res.Data.Waypoints[0]);
                    }

                    UpdateModeChrome();
                    PopulateSystemList();
                    ResetMapCamera();
                    RefreshMapUI();
                    
                    var wpsRes = await _apiService.GetSystemWaypoints(symbol);
                    if (wpsRes != null && wpsRes.Data != null)
                    {
                        if (_selectedWaypoint == null && wpsRes.Data.Count > 0)
                        {
                            SelectWaypoint(wpsRes.Data[0]);
                        }
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
            if (_mapMode == MapMode.Galaxy)
            {
                UpdateGalaxySystems();
            }
            else
            {
                UpdateWaypoints();
            }
        }

        private void UpdateWaypoints()
        {
            var targetLayer = _waypointsLayer ?? _mapContainer;
            if (targetLayer == null || _labelContainer == null) return;
            
            targetLayer.Clear();
            _labelContainer.Clear();

            if (_currentSystem == null) return;

            float scale = SystemScale; // Base scale for system coordinates

            foreach (var wp in _currentSystem.Waypoints)
            {
                if (waypointIconTemplate == null) continue;

                Vector2 basePos = new Vector2(wp.X, wp.Y) * scale;
                Vector2 screenPos = basePos * MapZoom + MapOffset;

                var icon = waypointIconTemplate.Instantiate();
                var iconRoot = icon.Q<VisualElement>("waypoint-root") ?? icon;
                icon.style.position = Position.Absolute;
                icon.style.left = screenPos.x;
                icon.style.top = screenPos.y;
                
                // Waypoint Type Colors (Adding classes defined in MainStyle.uss)
                iconRoot.AddToClassList($"wp-{wp.Type.ToString().ToLower()}");
                icon.RegisterCallback<ClickEvent>(_ => SelectSystemWaypoint(wp));

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

        private void UpdateGalaxySystems()
        {
            var targetLayer = _waypointsLayer ?? _mapContainer;
            if (targetLayer == null || _labelContainer == null) return;

            targetLayer.Clear();
            _labelContainer.Clear();

            UpdateGalaxyLabels();
        }

        private void UpdateGalaxyLabels()
        {
            if (_labelContainer == null || _filteredSystems == null || _filteredSystems.Count == 0)
            {
                return;
            }

            float showAllThreshold = 0.45f;
            bool showAllLabels = MapZoom > showAllThreshold;

            foreach (var system in _filteredSystems)
            {
                if (!showAllLabels && system.Symbol != _selectedSymbol)
                {
                    continue;
                }

                Vector2 pos = new Vector2(system.X * GalaxyScale, system.Y * GalaxyScale) * MapZoom + MapOffset;
                var color = system.Symbol == _selectedSymbol ? Color.cyan : Color.white;
                _labelContainer.Add(GetLabelFromPool(system.Symbol, pos, color));
            }
        }

        private Label GetLabelFromPool(string text, Vector2 pos, Color color)
        {
            return new Label(text)
            {
                style =
                {
                    position = Position.Absolute,
                    left = pos.x + 8,
                    top = pos.y - 8,
                    color = color,
                    fontSize = 10,
                    unityFontStyleAndWeight = color == Color.cyan ? FontStyle.Bold : FontStyle.Normal
                }
            };
        }

        private void SelectSystemWaypoint(SystemWaypoint waypoint)
        {
            if (waypoint == null) return;

            _selectedSymbol = waypoint.Symbol;
            _selectedWaypoint = null;

            foreach (var listEntry in _listEntries)
            {
                listEntry.RemoveFromClassList("selected-entry");
            }

            var selectedListEntry = _systemList?.Q<VisualElement>($"list-{waypoint.Symbol}");
            selectedListEntry?.AddToClassList("selected-entry");

            ApplyWaypointSelection(waypoint.Symbol, waypoint.Type.ToString(), waypoint.X, waypoint.Y, "Waypoint selected.");
            _ = LoadSpecializedInfo(waypoint.Symbol, waypoint.Type.ToString());
            RefreshMapUI();
        }

        private void SelectWaypoint(Waypoint waypoint)
        {
            if (waypoint == null) return;

            _selectedSymbol = waypoint.Symbol;
            _selectedWaypoint = waypoint;
            foreach (var listEntry in _listEntries)
            {
                listEntry.RemoveFromClassList("selected-entry");
            }

            var selectedListEntry = _systemList?.Q<VisualElement>($"list-{waypoint.Symbol}");
            selectedListEntry?.AddToClassList("selected-entry");

            var traitSummary = waypoint.Traits != null && waypoint.Traits.Count > 0
                ? string.Join(", ", waypoint.Traits.Select(t => t.Symbol.ToString().Replace("_", " ")))
                : "No traits available.";
            ApplyWaypointSelection(waypoint.Symbol, waypoint.Type.ToString(), waypoint.X, waypoint.Y, traitSummary);
            _ = LoadSpecializedInfo(waypoint.Symbol, waypoint.Type.ToString());
            RefreshMapUI();
        }

        private void ApplyWaypointSelection(string symbol, string type, int x, int y, string description)
        {
            if (_wpSymbolLabel != null) _wpSymbolLabel.text = symbol;
            if (_wpTypeLabel != null) _wpTypeLabel.text = type;
            if (_wpCoordsLabel != null) _wpCoordsLabel.text = $"{x}, {y}";
            if (_wpDescLabel != null) _wpDescLabel.text = description;

            if (_extraInfoTitleLabel != null)
            {
                _extraInfoTitleLabel.text = $"Specialized Info: {symbol}";
            }

            _extraContentContainer?.Clear();
        }

        private async Task LoadSpecializedInfo(string waypointSymbol, string waypointType)
        {
            if (_apiService == null || _extraContentContainer == null || _currentSystem == null)
            {
                return;
            }

            _extraContentContainer.Clear();
            var status = new Label("Loading specialized info...");
            _extraContentContainer.Add(status);

            try
            {
                var systemSymbol = _currentSystem.Symbol;
                _extraContentContainer.Clear();

                if (waypointType == "JUMP_GATE")
                {
                    var jumpGate = await _apiService.GetJumpGate(systemSymbol, waypointSymbol);
                    if (jumpGate?.Data?.Connections != null && jumpGate.Data.Connections.Count > 0)
                    {
                        foreach (var connection in jumpGate.Data.Connections)
                        {
                            _extraContentContainer.Add(new Label($"- {connection}"));
                        }
                    }
                    else
                    {
                        _extraContentContainer.Add(new Label("No jump connections available."));
                    }
                }
                else
                {
                    bool anyData = false;

                    try
                    {
                        var market = await _apiService.GetMarket(systemSymbol, waypointSymbol);
                        if (market?.Data != null)
                        {
                            anyData = true;
                            _extraContentContainer.Add(new Label("Marketplace available."));
                        }
                    }
                    catch { }

                    try
                    {
                        var shipyard = await _apiService.GetShipyard(systemSymbol, waypointSymbol);
                        if (shipyard?.Data != null)
                        {
                            anyData = true;
                            _extraContentContainer.Add(new Label("Shipyard available."));
                        }
                    }
                    catch { }

                    try
                    {
                        var construction = await _apiService.GetConstruction(systemSymbol, waypointSymbol);
                        if (construction?.Data != null)
                        {
                            anyData = true;
                            _extraContentContainer.Add(new Label("Construction site data available."));
                        }
                    }
                    catch { }

                    if (!anyData)
                    {
                        _extraContentContainer.Add(new Label("No specialized info available."));
                    }
                }
            }
            catch (Exception e)
            {
                _extraContentContainer.Clear();
                _extraContentContainer.Add(new Label($"Failed to load specialized info: {e.Message}"));
            }
        }

        private void UpdateModeChrome()
        {
            if (_viewGalaxyButton != null)
            {
                bool hasSelection = !string.IsNullOrEmpty(_selectedSymbol);
                _viewGalaxyButton.style.visibility = hasSelection ? Visibility.Visible : Visibility.Hidden;
                _viewGalaxyButton.SetEnabled(hasSelection);
                _viewGalaxyButton.text = _mapMode == MapMode.System ? "GALAXY" : "SYSTEM";
            }

            if (_systemDetailPanel != null)
            {
                _systemDetailPanel.style.display = _mapMode == MapMode.System ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_selectedSystemLabel != null)
            {
                _selectedSystemLabel.text = _mapMode == MapMode.System && !string.IsNullOrEmpty(_selectedSymbol)
                    ? $"System: {_selectedSymbol}"
                    : "Galaxy Map";
            }

            if (_legendContent != null)
            {
                _legendContent.style.display = _legendExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            }

            ConfigureZoomLimits();
            RefreshLegend();
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

            if (_mapMode == MapMode.Galaxy)
            {
                DrawGalaxyBulk(painter, rect);
            }
        }

        private void DrawGalaxyBulk(Painter2D painter, Rect rect)
        {
            if (_filteredSystems == null || _filteredSystems.Count == 0)
            {
                return;
            }

            foreach (var system in _filteredSystems)
            {
                Vector2 pos = new Vector2(system.X * GalaxyScale, system.Y * GalaxyScale) * MapZoom + MapOffset;
                if (!rect.Contains(pos))
                {
                    continue;
                }

                bool selected = system.Symbol == _selectedSymbol;
                painter.fillColor = selected ? Color.cyan : GetSystemColor(system.Type);
                painter.BeginPath();
                painter.Arc(pos, selected ? 4f : 2.5f, 0, 360);
                painter.Fill();
            }
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

        private void RefreshLegend()
        {
            if (_legendItems == null) return;
            _legendItems.Clear();

            if (_mapMode == MapMode.Galaxy)
            {
                var typeSet = (_filteredSystems ?? new List<DatabaseManager.IndexedSystem>())
                    .Select(s => NormalizeSystemTypeKey(s.Type))
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .OrderBy(t => t);

                foreach (var type in typeSet)
                {
                    AddLegendRow(FormatSystemTypeLabel(type), GetSystemColor(type));
                }
            }
            else
            {
                var typeSet = (_currentSystem?.Waypoints ?? new List<SystemWaypoint>())
                    .Select(w => w.Type.ToString())
                    .Distinct()
                    .OrderBy(t => t);

                foreach (var type in typeSet)
                {
                    AddLegendRow(type, GetWaypointColor(type));
                }
            }
        }

        private void AddLegendRow(string type, Color color)
        {
            if (_legendItems == null) return;

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            var bullet = new VisualElement { style = { width = 8, height = 8, marginRight = 5 } };
            bullet.style.backgroundColor = color;
            bullet.style.borderTopLeftRadius = 4;
            bullet.style.borderTopRightRadius = 4;
            bullet.style.borderBottomLeftRadius = 4;
            bullet.style.borderBottomRightRadius = 4;

            var label = new Label(type.Replace("_", " ")) { style = { fontSize = 9, color = Color.gray } };
            row.Add(bullet);
            row.Add(label);
            _legendItems.Add(row);
        }

        private Color GetSystemColor(string type)
        {
            var key = NormalizeSystemTypeKey(type);
            return key switch
            {
                "REDSTAR" => new Color(1f, 0.3f, 0.3f),
                "BLUESTAR" => new Color(0.3f, 0.6f, 1f),
                "NEUTRONSTAR" => new Color(0.85f, 0.85f, 1f),
                "ORANGESTAR" => new Color(1f, 0.6f, 0.25f),
                "YOUNGSTAR" => new Color(0.6f, 1f, 1f),
                "WHITEDWARF" => new Color(0.95f, 0.95f, 1f),
                "UNSTABLE" => new Color(1f, 0.4f, 0.8f),
                "NEBULA" => new Color(1f, 0.5f, 1f),
                "BLACKHOLE" => new Color(0.2f, 0.2f, 0.2f),
                "HYPERGIANT" => new Color(1f, 0.6f, 0.2f),
                _ => Color.white
            };
        }


                    private void HandleMapClick(Vector2 localPosition)
                    {
                        if (_mapMode != MapMode.Galaxy || _filteredSystems == null || _filteredSystems.Count == 0)
                        {
                            return;
                        }

                        Vector2 worldPos = (localPosition - MapOffset) / MapZoom;
                        DatabaseManager.IndexedSystem closest = null;
                        float closestDistance = float.MaxValue;

                        foreach (var system in _filteredSystems)
                        {
                            float distance = Vector2.Distance(new Vector2(system.X * GalaxyScale, system.Y * GalaxyScale), worldPos);
                            if (distance < closestDistance && distance < (14f / Mathf.Max(MapZoom, 0.01f)))
                            {
                                closestDistance = distance;
                                closest = system;
                            }
                        }

                        if (closest != null)
                        {
                            var listEntry = _systemList?.Q<VisualElement>($"list-{closest.Symbol}");
                            SelectGalaxySystem(closest, listEntry);
                        }
                    }
        private string NormalizeSystemTypeKey(string type)
        {
            if (string.IsNullOrEmpty(type)) return string.Empty;
            return new string(type.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        private string FormatSystemTypeLabel(string normalizedType)
        {
            return normalizedType switch
            {
                "REDSTAR" => "RED STAR",
                "BLUESTAR" => "BLUE STAR",
                "NEUTRONSTAR" => "NEUTRON STAR",
                "ORANGESTAR" => "ORANGE STAR",
                "YOUNGSTAR" => "YOUNG STAR",
                "WHITEDWARF" => "WHITE DWARF",
                "UNSTABLE" => "UNSTABLE",
                "NEBULA" => "NEBULA",
                "BLACKHOLE" => "BLACK HOLE",
                "HYPERGIANT" => "HYPERGIANT",
                _ => normalizedType
            };
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
                if (evt.button == 0)
                {
                    _presenter.HandleMapClick(evt.localPosition);
                    evt.StopPropagation();
                    return;
                }

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
                _presenter.MapZoom = Mathf.Clamp(_presenter.MapZoom * (1f + delta), _presenter._minZoom, _presenter._maxZoom);
                
                Vector2 mousePos = evt.localMousePosition;
                Vector2 worldPos = (mousePos - _presenter.MapOffset) / oldZoom;
                _presenter.MapOffset = mousePos - (worldPos * _presenter.MapZoom);
                
                _presenter.RefreshMapUI();
                evt.StopPropagation();
            }
        }
    }
}
