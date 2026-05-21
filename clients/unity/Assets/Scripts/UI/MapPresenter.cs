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
        private List<(string FromSystem, string ToSystem)> _jumpGateSystemLinks = new List<(string, string)>();
        private Dictionary<string, DatabaseManager.IndexedSystem> _galaxySystemLookup = new Dictionary<string, DatabaseManager.IndexedSystem>();
        private readonly List<VisualElement> _listEntries = new List<VisualElement>();

        private DatabaseManager _dbManager;
        private APIService _apiService;

        private SpaceTraders.Generated.Model.System _currentSystem;
        private string _selectedSymbol;
        private string _selectedSystemSymbol;
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
        private const float SelectionScreenRadius = 14f;

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
                RebuildGalaxyLookup();
                LoadJumpGateConnections();
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
                if (!string.IsNullOrEmpty(_selectedSystemSymbol))
                {
                    _selectedSymbol = _selectedSystemSymbol;
                }
                UpdateModeChrome();
                PopulateSystemList();
                ResetMapCamera();
                RefreshMapUI();
            }
            else
            {
                // Entering system mode should load the selected system immediately.
                var targetSystem = !string.IsNullOrEmpty(_selectedSystemSymbol) ? _selectedSystemSymbol : _selectedSymbol;
                if (!string.IsNullOrEmpty(targetSystem))
                {
                    SelectSystem(targetSystem);
                }
            }
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
                    RebuildGalaxyLookup();
                    LoadJumpGateConnections();
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

            var points = GetCurrentModeWorldPoints();
            if (points.Any())
            {
                FitBounds(points, rect);
            }
            else
            {
                MapOffset = new Vector2(rect.width / 2f, rect.height / 2f);
                MapZoom = Mathf.Clamp(1.0f, _minZoom, _maxZoom);
            }

            _mapInitialized = true;
        }

        private IEnumerable<Vector2> GetCurrentModeWorldPoints()
        {
            if (_mapMode == MapMode.Galaxy)
            {
                return (_filteredSystems ?? Enumerable.Empty<DatabaseManager.IndexedSystem>())
                    .Select(GetGalaxySystemWorldPosition);
            }

            return (_currentSystem?.Waypoints ?? Enumerable.Empty<SystemWaypoint>())
                .Select(GetSystemWaypointWorldPosition);
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

        private Vector2 GetGalaxySystemWorldPosition(DatabaseManager.IndexedSystem system)
        {
            return new Vector2(system.X * GalaxyScale, system.Y * GalaxyScale);
        }

        private Vector2 GetSystemWaypointWorldPosition(SystemWaypoint waypoint)
        {
            return new Vector2(waypoint.X * SystemScale, waypoint.Y * SystemScale);
        }

        private Vector2 GetWaypointWorldPosition(Waypoint waypoint)
        {
            return new Vector2(waypoint.X * SystemScale, waypoint.Y * SystemScale);
        }

        private Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return worldPosition * MapZoom + MapOffset;
        }

        private Vector2 ScreenToWorld(Vector2 localPosition)
        {
            return (localPosition - MapOffset) / Mathf.Max(MapZoom, 0.0001f);
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

                var selectedGalaxySymbol = !string.IsNullOrEmpty(_selectedSystemSymbol) ? _selectedSystemSymbol : _selectedSymbol;
                if (s.Symbol == selectedGalaxySymbol)
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
            _selectedSystemSymbol = system.Symbol;
            _selectedWaypoint = null;

            if (_filteredSystems != null && _filteredSystems.Count > 0)
            {
                int selectedIndex = _filteredSystems.FindIndex(s => s.Symbol == system.Symbol);
                if (selectedIndex >= 0)
                {
                    int selectedPage = (selectedIndex / PageSize) + 1;
                    if (_currentPage != selectedPage)
                    {
                        _currentPage = selectedPage;
                        PopulateSystemList();
                    }
                }
            }

            ClearListSelection();

            entry ??= _systemList?.Q<VisualElement>($"list-{system.Symbol}");
            entry?.AddToClassList("selected-entry");

            ApplyGalaxySystemSelectionDetails(system);

            if (_mapMode == MapMode.Galaxy)
            {
                CenterMapOnWorldPosition(GetGalaxySystemWorldPosition(system));
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
            _selectedSystemSymbol = symbol;

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
                    _currentSystem.Waypoints ??= new List<SystemWaypoint>();
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
                        _currentSystem.Waypoints = wpsRes.Data
                            .Select(wp => new SystemWaypoint(
                                symbol: wp.Symbol,
                                type: wp.Type,
                                x: wp.X,
                                y: wp.Y,
                                orbitals: wp.Orbitals ?? new List<WaypointOrbital>(),
                                orbits: wp.Orbits))
                            .ToList();

                        if (_selectedWaypoint == null && wpsRes.Data.Count > 0)
                        {
                            SelectWaypoint(wpsRes.Data[0]);
                        }

                        PopulateSystemList();
                        ResetMapCamera();
                        RefreshMapUI();
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
            RefreshMapOverlayLayers();
        }

        private void RefreshMapOverlayLayers()
        {
            var mapLayer = _waypointsLayer ?? _mapContainer;
            if (mapLayer == null || _labelContainer == null) return;
            
            mapLayer.Clear();
            _labelContainer.Clear();

            if (_mapMode == MapMode.Galaxy)
            {
                UpdateGalaxyLabels();
                return;
            }

            UpdateSystemWaypointLabels();
        }

        private void UpdateSystemWaypointLabels()
        {
            if (_labelContainer == null || _currentSystem?.Waypoints == null || _currentSystem.Waypoints.Count == 0)
            {
                return;
            }

            const float showAllThreshold = 0.55f;
            bool showAllLabels = MapZoom > showAllThreshold;

            foreach (var waypoint in _currentSystem.Waypoints)
            {
                if (!showAllLabels && waypoint.Symbol != _selectedSymbol)
                {
                    continue;
                }

                Vector2 pos = WorldToScreen(GetSystemWaypointWorldPosition(waypoint));
                var color = waypoint.Symbol == _selectedSymbol ? Color.cyan : Color.white;
                _labelContainer.Add(GetLabelFromPool(waypoint.Symbol, pos, color));
            }
        }

        private void UpdateGalaxyLabels()
        {
            if (_labelContainer == null || _filteredSystems == null || _filteredSystems.Count == 0)
            {
                return;
            }

            float showAllThreshold = 0.45f;
            bool showAllLabels = MapZoom > showAllThreshold;
            var selectedGalaxySymbol = GetSelectedGalaxySymbol();

            foreach (var system in _filteredSystems)
            {
                if (!showAllLabels && system.Symbol != selectedGalaxySymbol)
                {
                    continue;
                }

                Vector2 pos = WorldToScreen(GetGalaxySystemWorldPosition(system));
                var color = system.Symbol == selectedGalaxySymbol ? Color.cyan : Color.white;
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

            SelectWaypointCore(
                waypoint.Symbol,
                waypoint.Type.ToString(),
                waypoint.X,
                waypoint.Y,
                GetSystemWaypointWorldPosition(waypoint),
                "Waypoint selected.",
                onSelectedWaypoint: () => _selectedWaypoint = null);
        }

        private void SelectWaypoint(Waypoint waypoint)
        {
            if (waypoint == null) return;

            var traitSummary = waypoint.Traits != null && waypoint.Traits.Count > 0
                ? string.Join(", ", waypoint.Traits.Select(t => t.Symbol.ToString().Replace("_", " ")))
                : "No traits available.";

            SelectWaypointCore(
                waypoint.Symbol,
                waypoint.Type.ToString(),
                waypoint.X,
                waypoint.Y,
                GetWaypointWorldPosition(waypoint),
                traitSummary,
                onSelectedWaypoint: () => _selectedWaypoint = waypoint);
        }

        private void SelectWaypointCore(string symbol, string type, int x, int y, Vector2 worldPosition, string description, Action onSelectedWaypoint)
        {
            _selectedSymbol = symbol;
            onSelectedWaypoint?.Invoke();

            ClearListSelection();

            var selectedListEntry = _systemList?.Q<VisualElement>($"list-{symbol}");
            selectedListEntry?.AddToClassList("selected-entry");

            if (_mapMode == MapMode.System)
            {
                CenterMapOnWorldPosition(worldPosition);
            }

            ApplyWaypointSelection(symbol, type, x, y, description);
            _ = LoadSpecializedInfo(symbol, type);
            RefreshMapUI();
        }

        private void ApplyGalaxySystemSelectionDetails(DatabaseManager.IndexedSystem system)
        {
            if (_selectedSystemLabel != null)
            {
                _selectedSystemLabel.text = $"System: {system.Symbol}";
            }

            if (_wpSymbolLabel != null) _wpSymbolLabel.text = system.Symbol;
            if (_wpTypeLabel != null) _wpTypeLabel.text = system.Type;
            if (_wpCoordsLabel != null) _wpCoordsLabel.text = $"{system.X}, {system.Y}";
            if (_wpDescLabel != null) _wpDescLabel.text = "Selected system. Click SYSTEM to open.";

            if (_extraInfoTitleLabel != null) _extraInfoTitleLabel.text = "System Selection";
            if (_extraContentContainer != null)
            {
                _extraContentContainer.Clear();
                _extraContentContainer.Add(new Label("Use the SYSTEM button to switch to waypoint view."));
            }
        }

        private void ClearListSelection()
        {
            foreach (var listEntry in _listEntries)
            {
                listEntry.RemoveFromClassList("selected-entry");
            }
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

        private void CenterMapOnWorldPosition(Vector2 worldPosition)
        {
            if (_mapContainer == null)
            {
                return;
            }

            var rect = _mapContainer.contentRect;
            if (float.IsNaN(rect.width) || float.IsNaN(rect.height) || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            MapOffset = new Vector2(rect.width * 0.5f, rect.height * 0.5f) - (worldPosition * MapZoom);
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
                bool hasSelection = !string.IsNullOrEmpty(_selectedSystemSymbol) || !string.IsNullOrEmpty(_selectedSymbol);
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
                var systemTitle = !string.IsNullOrEmpty(_currentSystem?.Symbol)
                    ? _currentSystem.Symbol
                    : (!string.IsNullOrEmpty(_selectedSystemSymbol) ? _selectedSystemSymbol : _selectedSymbol);

                _selectedSystemLabel.text = _mapMode == MapMode.System && !string.IsNullOrEmpty(systemTitle)
                    ? $"System: {systemTitle}"
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

            DrawModeGeometry(painter, rect);
        }

        private void DrawModeGeometry(Painter2D painter, Rect rect)
        {
            if (_mapMode == MapMode.Galaxy)
            {
                DrawJumpGatePaths(painter, rect);
                DrawGalaxyBulk(painter, rect);
                return;
            }

            DrawSystemOrbitRings(painter, rect);
            DrawSystemWaypoints(painter, rect);
        }

        private void DrawSystemWaypoints(Painter2D painter, Rect rect)
        {
            if (_currentSystem?.Waypoints == null || _currentSystem.Waypoints.Count == 0)
            {
                return;
            }

            foreach (var waypoint in _currentSystem.Waypoints)
            {
                bool selected = waypoint.Symbol == _selectedSymbol;
                string type = waypoint.Type.ToString();
                Vector2 pos = WorldToScreen(GetSystemWaypointWorldPosition(waypoint));
                float radius = selected ? 4.5f : 3.5f;

                if (!rect.Overlaps(new Rect(pos.x - radius, pos.y - radius, radius * 2f, radius * 2f)))
                {
                    continue;
                }

                Color fillColor = selected ? Color.cyan : GetWaypointColor(type);
                Color strokeColor = selected ? Color.white : new Color(0.92f, 0.92f, 0.92f, 0.85f);
                float lineWidth = selected ? 1.5f : 1f;

                DrawWaypointShape(painter, pos, radius, type, fillColor, strokeColor, lineWidth);
            }
        }

        private void DrawWaypointShape(Painter2D painter, Vector2 pos, float radius, string type, Color fillColor, Color strokeColor, float lineWidth)
        {
            if (type == "ORBITAL_STATION")
            {
                DrawWaypointPolygon(
                    painter,
                    new[]
                    {
                        new Vector2(pos.x - radius, pos.y - radius),
                        new Vector2(pos.x + radius, pos.y - radius),
                        new Vector2(pos.x + radius, pos.y + radius),
                        new Vector2(pos.x - radius, pos.y + radius)
                    },
                    fillColor,
                    strokeColor,
                    lineWidth);
                return;
            }

            if (type == "ASTEROID_FIELD")
            {
                DrawWaypointPolygon(
                    painter,
                    new[]
                    {
                        new Vector2(pos.x, pos.y - radius),
                        new Vector2(pos.x + radius, pos.y + radius),
                        new Vector2(pos.x - radius, pos.y + radius)
                    },
                    fillColor,
                    strokeColor,
                    lineWidth);
                return;
            }

            if (type == "JUMP_GATE")
            {
                DrawWaypointPolygon(
                    painter,
                    new[]
                    {
                        new Vector2(pos.x, pos.y - radius),
                        new Vector2(pos.x + radius, pos.y),
                        new Vector2(pos.x, pos.y + radius),
                        new Vector2(pos.x - radius, pos.y)
                    },
                    fillColor,
                    strokeColor,
                    lineWidth);

                painter.fillColor = new Color(0.05f, 0.05f, 0.09f, 1f);
                painter.BeginPath();
                painter.Arc(pos, Mathf.Max(1f, radius * 0.45f), 0f, 360f);
                painter.Fill();
                return;
            }

            painter.fillColor = fillColor;
            painter.strokeColor = strokeColor;
            painter.lineWidth = lineWidth;
            painter.BeginPath();
            painter.Arc(pos, radius, 0f, 360f);
            painter.Fill();
            painter.Stroke();
        }

        private void DrawWaypointPolygon(Painter2D painter, IReadOnlyList<Vector2> points, Color fillColor, Color strokeColor, float lineWidth)
        {
            if (points == null || points.Count < 3)
            {
                return;
            }

            painter.fillColor = fillColor;
            painter.strokeColor = strokeColor;
            painter.lineWidth = lineWidth;

            painter.BeginPath();
            painter.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                painter.LineTo(points[i]);
            }
            painter.LineTo(points[0]);
            painter.Fill();
            painter.Stroke();
        }

        private void DrawSystemOrbitRings(Painter2D painter, Rect rect)
        {
            if (_currentSystem?.Waypoints == null || _currentSystem.Waypoints.Count == 0)
            {
                return;
            }

            var bySymbol = _currentSystem.Waypoints
                .Where(w => !string.IsNullOrEmpty(w.Symbol))
                .ToDictionary(w => w.Symbol, w => w);

            // Deduplicate rings by parent + radius to avoid stacking identical strokes.
            var drawn = new HashSet<string>();
            painter.strokeColor = new Color(0.55f, 0.65f, 0.8f, 0.35f);
            painter.lineWidth = 1f;

            foreach (var waypoint in _currentSystem.Waypoints)
            {
                if (waypoint == null || string.IsNullOrEmpty(waypoint.Orbits))
                {
                    continue;
                }

                if (!bySymbol.TryGetValue(waypoint.Orbits, out var parent))
                {
                    continue;
                }

                Vector2 parentWorld = new Vector2(parent.X, parent.Y) * SystemScale;
                Vector2 childWorld = new Vector2(waypoint.X, waypoint.Y) * SystemScale;
                float radiusWorld = Vector2.Distance(parentWorld, childWorld);
                if (radiusWorld <= 0.001f)
                {
                    continue;
                }

                string ringKey = $"{parent.Symbol}:{Mathf.RoundToInt(radiusWorld * 100f)}";
                if (!drawn.Add(ringKey))
                {
                    continue;
                }

                Vector2 center = WorldToScreen(parentWorld);
                float radius = radiusWorld * MapZoom;

                if (!rect.Overlaps(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f)))
                {
                    continue;
                }

                painter.BeginPath();
                painter.Arc(center, radius, 0f, 360f);
                painter.Stroke();
            }
        }

        private void RebuildGalaxyLookup()
        {
            _galaxySystemLookup = (_allGalaxySystems ?? new List<DatabaseManager.IndexedSystem>())
                .ToDictionary(s => s.Symbol, s => s);
        }

        private void LoadJumpGateConnections()
        {
            _jumpGateSystemLinks = new List<(string, string)>();
            if (_dbManager == null) return;

            var gates = _dbManager.GetAllJumpGateConnections();
            var seen = new HashSet<string>();

            foreach (var gate in gates)
            {
                if (string.IsNullOrEmpty(gate.ConnectionsJson)) continue;

                var connectedWaypoints = gate.ConnectionsJson.Split(
                    new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

                foreach (var connectedWp in connectedWaypoints)
                {
                    string toSystem = ParseSystemSymbol(connectedWp.Trim());
                    if (string.IsNullOrEmpty(toSystem) || toSystem == gate.SystemSymbol) continue;

                    // Canonical key ensures A→B and B→A are treated as the same edge.
                    string key = string.Compare(gate.SystemSymbol, toSystem, System.StringComparison.Ordinal) < 0
                        ? $"{gate.SystemSymbol}|{toSystem}"
                        : $"{toSystem}|{gate.SystemSymbol}";

                    if (seen.Add(key))
                    {
                        _jumpGateSystemLinks.Add((gate.SystemSymbol, toSystem));
                    }
                }
            }
        }

        private static string ParseSystemSymbol(string waypointSymbol)
        {
            if (string.IsNullOrEmpty(waypointSymbol)) return string.Empty;
            var parts = waypointSymbol.Split('-');
            if (parts.Length < 2) return string.Empty;
            return string.Join("-", parts, 0, parts.Length - 1);
        }

        private void DrawJumpGatePaths(Painter2D painter, Rect rect)
        {
            if (_jumpGateSystemLinks == null || _jumpGateSystemLinks.Count == 0) return;

            painter.strokeColor = new Color(0.4f, 0.7f, 1f, 0.15f);
            painter.lineWidth = 1f;
            painter.BeginPath();

            foreach (var (fromSys, toSys) in _jumpGateSystemLinks)
            {
                if (!_galaxySystemLookup.TryGetValue(fromSys, out var from)) continue;
                if (!_galaxySystemLookup.TryGetValue(toSys, out var to)) continue;

                Vector2 a = WorldToScreen(GetGalaxySystemWorldPosition(from));
                Vector2 b = WorldToScreen(GetGalaxySystemWorldPosition(to));

                // Viewport cull: skip if both endpoints are outside the rect.
                if (!rect.Contains(a) && !rect.Contains(b)) continue;

                painter.MoveTo(a);
                painter.LineTo(b);
            }

            painter.Stroke();
        }

        private void DrawGalaxyBulk(Painter2D painter, Rect rect)
        {
            if (_filteredSystems == null || _filteredSystems.Count == 0)
            {
                return;
            }

            var selectedGalaxySymbol = GetSelectedGalaxySymbol();

            foreach (var system in _filteredSystems)
            {
                Vector2 pos = WorldToScreen(GetGalaxySystemWorldPosition(system));
                if (!rect.Contains(pos))
                {
                    continue;
                }

                bool selected = system.Symbol == selectedGalaxySymbol;
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

        private string GetSelectedGalaxySymbol()
        {
            return !string.IsNullOrEmpty(_selectedSystemSymbol) ? _selectedSystemSymbol : _selectedSymbol;
        }
        private void HandleMapClick(Vector2 localPosition)
        {
            float maxWorldDistance = SelectionScreenRadius / Mathf.Max(MapZoom, 0.01f);
            Vector2 worldPosition = ScreenToWorld(localPosition);

            if (_mapMode == MapMode.Galaxy)
            {
                var closestSystem = FindClosestGalaxySystem(worldPosition, maxWorldDistance);
                if (closestSystem != null)
                {
                    var listEntry = _systemList?.Q<VisualElement>($"list-{closestSystem.Symbol}");
                    SelectGalaxySystem(closestSystem, listEntry);
                }
                return;
            }

            if (_mapMode == MapMode.System)
            {
                var closestWaypoint = FindClosestSystemWaypoint(worldPosition, maxWorldDistance);
                if (closestWaypoint != null)
                {
                    SelectSystemWaypoint(closestWaypoint);
                }
            }
        }

        private DatabaseManager.IndexedSystem FindClosestGalaxySystem(Vector2 worldPosition, float maxWorldDistance)
        {
            if (_filteredSystems == null || _filteredSystems.Count == 0)
            {
                return null;
            }

            DatabaseManager.IndexedSystem closest = null;
            float closestDistance = maxWorldDistance;

            foreach (var system in _filteredSystems)
            {
                float distance = Vector2.Distance(GetGalaxySystemWorldPosition(system), worldPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = system;
                }
            }

            return closest;
        }

        private SystemWaypoint FindClosestSystemWaypoint(Vector2 worldPosition, float maxWorldDistance)
        {
            if (_currentSystem?.Waypoints == null || _currentSystem.Waypoints.Count == 0)
            {
                return null;
            }

            SystemWaypoint closest = null;
            float closestDistance = maxWorldDistance;

            foreach (var waypoint in _currentSystem.Waypoints)
            {
                float distance = Vector2.Distance(GetSystemWaypointWorldPosition(waypoint), worldPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = waypoint;
                }
            }

            return closest;
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
