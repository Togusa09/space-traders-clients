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

        internal enum IconShape
        {
            Circle,
            Square,
            Triangle,
            Diamond,
            Hexagon
        }

        internal struct IconStyle
        {
            public IconShape Shape;
            public float Radius;
            public Color FillColor;
            public Color StrokeColor;
            public float StrokeWidth;
            public bool HasCore;
            public float CoreRadiusFactor;
            public Color CoreColor;
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
        private DropdownField _typeFilter;
        private DropdownField _facilityFilter;

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
        private List<Waypoint> _detailedWaypoints = new List<Waypoint>();

        // Map State for Panning/Zooming
        public Vector2 MapOffset { get; set; } = Vector2.zero;
        public float MapZoom { get; set; } = 1.0f;
        private bool _mapInitialized = false;
        private float _minZoom = 0.00001f;
        private float _maxZoom = 2000f;
        private MapMode _mapMode = MapMode.Galaxy;
        private int _currentPage = 1;
        private const int PageSize = 50;
        private bool _legendExpanded = true;
        private const float GalaxyScale = 6f;
        private const float SystemScale = 5f;
        private const float SelectionScreenRadius = 14f;
        private const float GalaxyIconDetailZoomThreshold = 0.15f;

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

            // Bind references
            _systemList = panel.Q<VisualElement>("system-list");
            _mapContainer = panel.Q<VisualElement>("map-container");
            _waypointsLayer = panel.Q<VisualElement>("waypoints-layer");
            _searchField = panel.Q<TextField>("system-search");
            _selectedSystemLabel = panel.Q<Label>("selected-system-title");
            _systemDetailPanel = panel.Q<VisualElement>("waypoint-details");
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
            _typeFilter = panel.Q<DropdownField>("type-filter");
            _facilityFilter = panel.Q<DropdownField>("facility-filter");

            _labelContainer = new VisualElement { style = { position = Position.Absolute, width = Length.Percent(100), height = Length.Percent(100) }, pickingMode = PickingMode.Ignore };
            _mapContainer?.Add(_labelContainer);

            if (_searchField != null) _searchField.RegisterValueChangedCallback(_ => ApplyFilter());
            if (_typeFilter != null) _typeFilter.RegisterValueChangedCallback(_ => ApplyFilter());
            if (_facilityFilter != null) _facilityFilter.RegisterValueChangedCallback(_ => ApplyFilter());

            InitializeFilterOptions();

            if (_viewGalaxyButton != null) _viewGalaxyButton.clicked += ToggleMapMode;
            if (_prevPageButton != null) _prevPageButton.clicked += () => ChangePage(-1);
            if (_nextPageButton != null) _nextPageButton.clicked += () => ChangePage(1);
            if (_legendToggleButton != null) _legendToggleButton.clicked += ToggleLegend;

            // Map Initialization
            _mapInitialized = false;
            _mapContainer.RegisterCallback<GeometryChangedEvent>(OnMapGeometryChanged);
            _mapContainer.generateVisualContent += OnGenerateVisualContent;
            _mapContainer.AddManipulator(new MapManipulator(this));

            // Load Data
            if (_dbManager != null)
            {
                _allGalaxySystems = _dbManager.GetAllSystems();
                RebuildGalaxyLookup();
                LoadJumpGateConnections();
            }

            ApplyFilter();
            _ = EnsureGalaxySystemsLoadedAsync();

            RefreshLegend();
            UpdateModeChrome();
            
            // Fallback for initial focus
            _mapContainer.schedule.Execute(() => {
                if (!_mapInitialized) { ResetMapCamera(); RefreshMapUI(); }
            }).StartingIn(100);
            
            Log.Info("[MapPresenter] Map panel setup complete.");
        }

        private void InitializeFilterOptions()
        {
            if (_typeFilter == null || _facilityFilter == null) return;
            if (_mapMode == MapMode.Galaxy)
            {
                _typeFilter.choices = new List<string> { "ALL", "NEUTRON_STAR", "RED_STAR", "ORANGE_STAR", "BLUE_STAR", "YOUNG_STAR", "WHITE_DWARF", "BLACK_HOLE", "HYPERGIANT", "NEBULA", "UNSTABLE" };
                _facilityFilter.choices = new List<string> { "ALL", "MARKETPLACE", "SHIPYARD", "CONSTRUCTION" };
            }
            else
            {
                _typeFilter.choices = new List<string> { "ALL", "PLANET", "MOON", "ORBITAL_STATION", "JUMP_GATE", "ASTEROID_FIELD", "ASTEROID", "ENGINEERED_ASTEROID_OUTPOST", "ASTEROID_BASE", "NEBULA", "DEBRIS_FIELD", "GRAVITY_WELL", "ARTIFICIAL_GRAVITY_WELL", "FUEL_STATION" };
                _facilityFilter.choices = new List<string> { "ALL", "MARKETPLACE", "SHIPYARD", "CONSTRUCTION" };
            }
            _typeFilter.index = 0;
            _facilityFilter.index = 0;
        }

        private void OnMapGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width <= 0) return;
            if (!_mapInitialized) { ResetMapCamera(); if (_mapInitialized) RefreshMapUI(); }
        }

        private void ToggleMapMode()
        {
            _mapMode = _mapMode == MapMode.System ? MapMode.Galaxy : MapMode.System;
            _selectedSymbol = null;
            _selectedWaypoint = null;
            InitializeFilterOptions();
            UpdateModeChrome();
            PopulateSystemList();
            ResetMapCamera();
            RefreshMapUI();
        }

        private void ChangePage(int delta)
        {
            if (_filteredSystems == null || _filteredSystems.Count == 0) return;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_filteredSystems.Count / PageSize));
            _currentPage = Mathf.Clamp(_currentPage + delta, 1, totalPages);
            PopulateSystemList();
            if (_mapMode == MapMode.Galaxy) { ResetMapCamera(); RefreshMapUI(); }
        }

        private void ToggleLegend()
        {
            _legendExpanded = !_legendExpanded;
            if (_legendContent != null) _legendContent.style.display = _legendExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            if (_legendToggleButton != null) _legendToggleButton.text = _legendExpanded ? "-" : "+";
        }

        private void ApplyFilter()
        {
            if (_mapMode == MapMode.Galaxy) ApplyGalaxyFilter();
            _currentPage = 1;
            PopulateSystemList();
            RefreshLegend();
            RefreshMapUI();
        }

        private void ApplyGalaxyFilter()
        {
            if (_allGalaxySystems == null) { _filteredSystems = new List<DatabaseManager.IndexedSystem>(); return; }
            string query = _searchField?.value?.ToUpper() ?? "";
            string typeFilter = _typeFilter?.value ?? "ALL";
            string facilityFilter = _facilityFilter?.value ?? "ALL";

            _filteredSystems = _allGalaxySystems
                .Where(s => MapFilterMatcher.MatchesGalaxySystem(s, query, typeFilter, facilityFilter))
                .ToList();
        }

        private async Task EnsureGalaxySystemsLoadedAsync()
        {
            if (_allGalaxySystems != null && _allGalaxySystems.Count > 0) return;
            if (_apiService == null) return;

            var loaded = new List<DatabaseManager.IndexedSystem>();
            int page = 1;
            try
            {
                while (true)
                {
                    var res = await _apiService.GetSystems(page, 100);
                    if (res?.Data == null || res.Data.Count == 0) break;
                    loaded.AddRange(res.Data.Select(s => new DatabaseManager.IndexedSystem { Symbol = s.Symbol, SectorSymbol = s.SectorSymbol, Type = s.Type.ToString(), X = s.X, Y = s.Y, WaypointCount = s.Waypoints?.Count ?? 0 }));
                    if (res.Meta != null && loaded.Count >= res.Meta.Total) break;
                    page++;
                }
                _allGalaxySystems = loaded;
                RebuildGalaxyLookup();
                _dbManager?.StoreSystems(_allGalaxySystems);
                ApplyFilter();
                ResetMapCamera();
                RefreshMapUI();
            }
            catch (Exception e) { Log.Error("[Map] Load failed: {Error}", e.Message); }
        }

        private void RebuildGalaxyLookup() => _galaxySystemLookup = _allGalaxySystems?.ToDictionary(s => s.Symbol, s => s) ?? new Dictionary<string, DatabaseManager.IndexedSystem>();

        private void LoadJumpGateConnections()
        {
            if (_dbManager == null) return;
            var gates = _dbManager.GetAllJumpGateConnections();
            _jumpGateSystemLinks.Clear();
            var seen = new HashSet<string>();
            foreach (var gate in gates)
            {
                if (string.IsNullOrEmpty(gate.ConnectionsJson)) continue;
                foreach (var connWp in gate.ConnectionsJson.Split(','))
                {
                    string other = connWp.Split('-')[0];
                    if (other == gate.SystemSymbol) continue;
                    string pair = string.Compare(gate.SystemSymbol, other) < 0 ? $"{gate.SystemSymbol}-{other}" : $"{other}-{gate.SystemSymbol}";
                    if (seen.Add(pair)) _jumpGateSystemLinks.Add((gate.SystemSymbol, other));
                }
            }
        }

        private void ResetMapCamera()
        {
            var rect = _mapContainer?.contentRect ?? Rect.zero;
            if (rect.width <= 0) return;
            if (_mapMode == MapMode.Galaxy)
            {
                if (_filteredSystems == null || _filteredSystems.Count == 0) { MapOffset = rect.size / 2f; MapZoom = 1.0f; }
                else FitBounds(_filteredSystems.Select(GetGalaxySystemWorldPosition), rect);
            }
            else
            {
                if (_currentSystem?.Waypoints == null || _currentSystem.Waypoints.Count == 0) { MapOffset = rect.size / 2f; MapZoom = 1.0f; }
                else FitBounds(_currentSystem.Waypoints.Select(GetSystemWaypointWorldPosition), rect);
            }
            _mapInitialized = true;
        }

        private void FitBounds(IEnumerable<Vector2> points, Rect rect)
        {
            var result = MapViewportMath.FitBounds(points, rect, _minZoom, _maxZoom);
            MapOffset = result.Offset;
            MapZoom = result.Zoom;
        }

        private Vector2 GetGalaxySystemWorldPosition(DatabaseManager.IndexedSystem s) => new Vector2(s.X * GalaxyScale, s.Y * GalaxyScale);
        private Vector2 GetSystemWaypointWorldPosition(SystemWaypoint w) => new Vector2(w.X * SystemScale, w.Y * SystemScale);
        private Vector2 GetWaypointWorldPosition(Waypoint w) => new Vector2(w.X * SystemScale, w.Y * SystemScale);
        private Vector2 WorldToScreen(Vector2 wp) => MapViewportMath.WorldToScreen(wp, MapZoom, MapOffset);
        private Vector2 ScreenToWorld(Vector2 lp) => MapViewportMath.ScreenToWorld(lp, MapZoom, MapOffset);

        private void PopulateSystemList()
        {
            if (_systemList == null) return;
            _systemList.Clear(); _listEntries.Clear();
            if (_mapMode == MapMode.System) { PopulateWaypointHierarchyList(); return; }
            if (_filteredSystems == null || _filteredSystems.Count == 0) { UpdatePageInfo(0, 0); return; }

            int total = Mathf.CeilToInt((float)_filteredSystems.Count / PageSize);
            _currentPage = Mathf.Clamp(_currentPage, 1, total);
            _pagedSystems = _filteredSystems.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            UpdatePageInfo(_currentPage, total);

            foreach (var s in _pagedSystems)
            {
                if (systemEntryTemplate == null) continue;
                var entry = systemEntryTemplate.Instantiate();
                var root = entry.Q<VisualElement>(null, "dashboard-entry") ?? entry;
                root.name = $"list-{s.Symbol}"; root.AddToClassList("selectable-entry");
                entry.Q<Label>("symbol-label").text = s.Symbol;
                entry.Q<Label>("details-label").text = $"{s.Type.Replace("_", " ")} | {s.X},{s.Y}";
                if (s.Symbol == _selectedSymbol) root.AddToClassList("selected-entry");
                root.RegisterCallback<ClickEvent>(_ => SelectGalaxySystem(s, root));
                _systemList.Add(entry); _listEntries.Add(root);
            }
        }

        private void SelectGalaxySystem(DatabaseManager.IndexedSystem s, VisualElement entry = null)
        {
            _selectedSymbol = s.Symbol; _selectedSystemSymbol = s.Symbol; _selectedWaypoint = null;
            ClearListSelection();
            entry ??= _systemList?.Q<VisualElement>($"list-{s.Symbol}");
            entry?.AddToClassList("selected-entry");
            ApplyGalaxySystemSelectionDetails(s);
            CenterMapOnWorldPosition(GetGalaxySystemWorldPosition(s));
            UpdateModeChrome(); RefreshMapUI();
        }

        private void PopulateWaypointHierarchyList()
        {
            var wps = _currentSystem?.Waypoints;
            if (wps == null || wps.Count == 0) { UpdatePageInfo(0, 0); return; }
            UpdatePageInfo(1, 1);
            string search = _searchField?.value?.Trim().ToUpperInvariant() ?? "";
            string typeF = _typeFilter?.value ?? "ALL";
            string facF = _facilityFilter?.value ?? "ALL";
            var childMap = wps.GroupBy(w => w.Orbits ?? "").ToDictionary(g => g.Key, g => g.OrderBy(w => w.Symbol).ToList());
            var roots = childMap.ContainsKey("") ? childMap[""] : wps.Where(w => string.IsNullOrEmpty(w.Orbits)).OrderBy(w => w.Symbol).ToList();
            foreach (var r in roots) { if (WaypointMatches(r, childMap, search, typeF, facF)) { AddWaypointEntry(r, 0); AddWaypointChildren(r, 1, childMap, search, typeF, facF); } }
        }

        private void AddWaypointChildren(SystemWaypoint p, int ind, Dictionary<string, List<SystemWaypoint>> cm, string s, string tf, string ff)
        {
            if (!cm.TryGetValue(p.Symbol, out var children)) return;
            foreach (var c in children) { if (WaypointMatches(c, cm, s, tf, ff)) { AddWaypointEntry(c, ind); AddWaypointChildren(c, ind + 1, cm, s, tf, ff); } }
        }

        private bool WaypointMatches(SystemWaypoint w, Dictionary<string, List<SystemWaypoint>> cm, string s, string tf, string ff)
        {
            return MapWaypointTreeFilter.HasMatchInSubtree(
                w,
                cm,
                waypoint =>
                {
                    var detailed = MapWaypointDetailLookup.FindBySymbol(_detailedWaypoints, waypoint.Symbol);
                    return MapFilterMatcher.MatchesWaypoint(waypoint, detailed, s, tf, ff);
                });
        }

        private void AddWaypointEntry(SystemWaypoint w, int ind)
        {
            if (systemEntryTemplate == null) return;
            var entry = systemEntryTemplate.Instantiate();
            var root = entry.Q<VisualElement>(null, "dashboard-entry") ?? entry;
            root.name = $"list-{w.Symbol}"; root.AddToClassList("selectable-entry");
            root.style.marginLeft = ind * 12;
            entry.Q<Label>("symbol-label").text = (ind > 0 ? "↳ " : "") + w.Symbol;
            entry.Q<Label>("details-label").text = w.Type.ToString().Replace("_", " ");
            if (w.Symbol == _selectedSymbol) root.AddToClassList("selected-entry");
            root.RegisterCallback<ClickEvent>(_ => SelectSystemWaypoint(w));
            _systemList.Add(entry); _listEntries.Add(root);
        }

        private void UpdatePageInfo(int cp, int tp)
        {
            if (_pageInfoLabel != null) _pageInfoLabel.text = tp <= 0 ? "0/0" : $"{cp}/{tp}";
            _prevPageButton?.SetEnabled(cp > 1); _nextPageButton?.SetEnabled(tp > 0 && cp < tp);
        }

        private async void SelectSystem(string sym, VisualElement entry = null)
        {
            _selectedSymbol = sym; _selectedSystemSymbol = sym; ClearListSelection();
            entry ??= _systemList?.Q<VisualElement>($"list-{sym}"); entry?.AddToClassList("selected-entry");
            UpdateModeChrome();
            try
            {
                var res = await _apiService.GetSystem(sym);
                if (res?.Data != null)
                {
                    _currentSystem = res.Data; _currentSystem.Waypoints ??= new List<SystemWaypoint>();
                    _mapMode = MapMode.System; _selectedWaypoint = null;
                    if (res.Data.Waypoints?.Count > 0) SelectSystemWaypoint(res.Data.Waypoints[0]);
                    InitializeFilterOptions(); UpdateModeChrome(); PopulateSystemList(); ResetMapCamera(); RefreshMapUI();
                    var wpsRes = await _apiService.GetSystemWaypoints(sym);
                    if (wpsRes?.Data != null)
                    {
                        _detailedWaypoints = wpsRes.Data;
                        _currentSystem.Waypoints = wpsRes.Data.Select(wp => new SystemWaypoint(wp.Symbol, wp.Type, wp.X, wp.Y, wp.Orbitals ?? new List<WaypointOrbital>(), wp.Orbits)).ToList();
                        UpdateSystemFacilities(sym, wpsRes.Data);
                        if (_selectedWaypoint == null && wpsRes.Data.Count > 0) SelectWaypoint(wpsRes.Data[0]);
                        PopulateSystemList(); RefreshMapUI();
                    }
                }
            }
            catch (Exception e) { Log.Error("[Map] System load fail: {Error}", e.Message); }
        }

        private void UpdateSystemFacilities(string sym, List<Waypoint> wps)
        {
            var facs = new HashSet<string>();
            foreach (var wp in wps) { if (wp.Traits == null) continue; foreach (var t in wp.Traits) { if (t.Symbol == WaypointTraitSymbol.MARKETPLACE) facs.Add("MARKETPLACE"); if (t.Symbol == WaypointTraitSymbol.SHIPYARD) facs.Add("SHIPYARD"); if (t.Symbol == WaypointTraitSymbol.UNDERCONSTRUCTION) facs.Add("CONSTRUCTION"); } }
            if (facs.Count > 0) { var sys = _allGalaxySystems?.FirstOrDefault(s => s.Symbol == sym); if (sys != null) { sys.KnownFacilities = string.Join(",", facs); _dbManager?.StoreSystems(new[] { sys }); } }
        }

        private void SelectSystemWaypoint(SystemWaypoint w)
        {
            if (w == null) return;
            _selectedSymbol = w.Symbol; ClearListSelection();
            _systemList?.Q<VisualElement>($"list-{w.Symbol}")?.AddToClassList("selected-entry");
            ApplySystemWaypointSelectionDetails(w);
            CenterMapOnWorldPosition(GetSystemWaypointWorldPosition(w));
            RefreshMapUI();
            var d = MapWaypointDetailLookup.FindBySymbol(_detailedWaypoints, w.Symbol);
            if (d != null) SelectWaypoint(d); else _ = FetchDetailedWaypointAndSelectAsync(w.Symbol);
        }

        private async Task FetchDetailedWaypointAndSelectAsync(string ws)
        {
            if (_currentSystem == null) return;
            try { var res = await _apiService.GetSystemWaypoints(_currentSystem.Symbol); if (res?.Data != null) { _detailedWaypoints = res.Data; var d = MapWaypointDetailLookup.FindBySymbol(res.Data, ws); if (d != null) SelectWaypoint(d); } } catch { }
        }

        private void SelectWaypoint(Waypoint w) { if (w == null) return; _selectedWaypoint = w; _selectedSymbol = w.Symbol; ApplyWaypointSelectionDetails(w); RefreshMapUI(); }
        private void ClearListSelection() { foreach (var e in _listEntries) e.RemoveFromClassList("selected-entry"); }

        private void CenterMapOnWorldPosition(Vector2 wp)
        {
            var rect = _mapContainer?.contentRect ?? Rect.zero;
            if (rect.width <= 0) return;
            MapOffset = MapViewportMath.CenterOnWorldPoint(rect, wp, MapZoom);
        }

        private void ApplyGalaxySystemSelectionDetails(DatabaseManager.IndexedSystem s)
        {
            if (_wpSymbolLabel != null) _wpSymbolLabel.text = s.Symbol;
            if (_wpTypeLabel != null) _wpTypeLabel.text = s.Type.Replace("_", " ");
            if (_wpCoordsLabel != null) _wpCoordsLabel.text = $"({s.X}, {s.Y})";
            if (_wpDescLabel != null) _wpDescLabel.text = "Select 'SYSTEM' to view waypoints and details.";
            _extraContentContainer?.Clear();
            if (_extraInfoTitleLabel != null) _extraInfoTitleLabel.text = "System Info";
            if (_extraContentContainer != null) { _extraContentContainer.Add(new Label($"Sector: {s.SectorSymbol}")); _extraContentContainer.Add(new Label($"Waypoints: {s.WaypointCount}")); if (!string.IsNullOrEmpty(s.KnownFacilities)) _extraContentContainer.Add(new Label($"Facilities: {s.KnownFacilities.Replace(",", ", ")}")); }
        }

        private void ApplySystemWaypointSelectionDetails(SystemWaypoint w)
        {
            if (_wpSymbolLabel != null) _wpSymbolLabel.text = w.Symbol;
            if (_wpTypeLabel != null) _wpTypeLabel.text = w.Type.ToString().Replace("_", " ");
            if (_wpCoordsLabel != null) _wpCoordsLabel.text = $"({w.X}, {w.Y})";
            if (_wpDescLabel != null) _wpDescLabel.text = "Loading details...";
            _extraContentContainer?.Clear();
        }

        private void ApplyWaypointSelectionDetails(Waypoint w)
        {
            if (_wpSymbolLabel != null) _wpSymbolLabel.text = w.Symbol;
            if (_wpTypeLabel != null) _wpTypeLabel.text = w.Type.ToString().Replace("_", " ");
            if (_wpCoordsLabel != null) _wpCoordsLabel.text = $"({w.X}, {w.Y})";
            string d = WaypointDescriptionBuilder.Build(w);
            if (_wpDescLabel != null) _wpDescLabel.text = d;
            _ = UpdateSpecializedInfoAsync(w.Symbol, w.Type.ToString());
        }

        private async Task UpdateSpecializedInfoAsync(string ws, string wt)
        {
            if (_currentSystem == null || _extraContentContainer == null) return;
            try
            {
                var ss = _currentSystem.Symbol; _extraContentContainer.Clear();
                if (wt == "JUMP_GATE")
                {
                    if (_extraInfoTitleLabel != null) _extraInfoTitleLabel.text = "Jump Connections";
                    var res = await _apiService.GetJumpGate(ss, ws);
                    if (res?.Data?.Connections?.Count > 0) foreach (var c in res.Data.Connections) _extraContentContainer.Add(new Label($"- {c}"));
                    else _extraContentContainer.Add(new Label("No connections found."));
                }
                else
                {
                    if (_extraInfoTitleLabel != null) _extraInfoTitleLabel.text = "Services";
                    var mT = _apiService.GetMarket(ss, ws); var sT = _apiService.GetShipyard(ss, ws); var cT = _apiService.GetConstruction(ss, ws);
                    await Task.WhenAll(mT, sT, cT);
                    bool any = false;
                    if (mT.Result?.Data != null) { any = true; var b = new Button(() => Log.Info("Market")) { text = "OPEN MARKET" }; b.AddToClassList("button"); _extraContentContainer.Add(b); }
                    if (sT.Result?.Data != null) { any = true; var b = new Button(() => Log.Info("Shipyard")) { text = "OPEN SHIPYARD" }; b.AddToClassList("button"); _extraContentContainer.Add(b); }
                    if (cT.Result?.Data != null && !cT.Result.Data.IsComplete) { any = true; _extraContentContainer.Add(new Label("Construction in progress.")); }
                    if (!any) _extraContentContainer.Add(new Label("No specialized info."));
                }
            }
            catch (Exception e) { _extraContentContainer?.Clear(); _extraContentContainer?.Add(new Label($"Load error: {e.Message}")); }
        }

        private void UpdateModeChrome()
        {
            if (_viewGalaxyButton != null) { bool hs = !string.IsNullOrEmpty(_selectedSystemSymbol) || !string.IsNullOrEmpty(_selectedSymbol); _viewGalaxyButton.style.visibility = hs ? Visibility.Visible : Visibility.Hidden; _viewGalaxyButton.SetEnabled(hs); _viewGalaxyButton.text = _mapMode == MapMode.System ? "GALAXY" : "SYSTEM"; }
            if (_selectedSystemLabel != null) { var st = !string.IsNullOrEmpty(_currentSystem?.Symbol) ? _currentSystem.Symbol : (!string.IsNullOrEmpty(_selectedSystemSymbol) ? _selectedSystemSymbol : _selectedSymbol); _selectedSystemLabel.text = _mapMode == MapMode.System && !string.IsNullOrEmpty(st) ? $"System: {st}" : "Galaxy Map"; }
            if (_legendContent != null) _legendContent.style.display = _legendExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshLegend()
        {
            if (_legendItems == null) return; _legendItems.Clear();
            if (_mapMode == MapMode.Galaxy) { AddLegendItem(IconShape.Circle, Color.white, "Star (Y/O)"); AddLegendItem(IconShape.Circle, new Color(1f, 0.3f, 0.3f), "Star (R)"); AddLegendItem(IconShape.Circle, new Color(0.3f, 0.6f, 1f), "Star (B)"); AddLegendItem(IconShape.Circle, new Color(0.6f, 1f, 1f), "Young Star"); AddLegendItem(IconShape.Square, new Color(1f, 0.5f, 1f), "Nebula"); }
            else { AddLegendItem(IconShape.Circle, new Color(0, 0.6f, 1f), "Planet"); AddLegendItem(IconShape.Circle, Color.gray, "Moon"); AddLegendItem(IconShape.Square, Color.yellow, "Station"); AddLegendItem(IconShape.Diamond, new Color(0.8f, 0, 1f), "Jump Gate"); AddLegendItem(IconShape.Hexagon, new Color(0.4f, 0.3f, 0.2f), "Asteroids"); }
        }

        private void AddLegendItem(IconShape s, Color c, string d)
        {
            var r = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            var i = new VisualElement { style = { width = 10, height = 10, backgroundColor = c, marginRight = 5 } };
            if (s == IconShape.Circle) { i.style.borderTopLeftRadius = 5; i.style.borderTopRightRadius = 5; i.style.borderBottomLeftRadius = 5; i.style.borderBottomRightRadius = 5; }
            else if (s == IconShape.Diamond) i.style.rotate = new Rotate(45);
            r.Add(i); r.Add(new Label(d) { style = { fontSize = 10, color = Color.white } }); _legendItems.Add(r);
        }

        private void RefreshMapUI() { _mapContainer?.MarkDirtyRepaint(); UpdateLabels(); }

        private void UpdateLabels()
        {
            if (_labelContainer == null || _mapContainer == null) return; _labelContainer.Clear();
            var rect = _mapContainer.contentRect; if (rect.width <= 0) return;
            if (_mapMode == MapMode.Galaxy)
            {
                if (_filteredSystems == null) return;
                bool sa = MapZoom > GalaxyIconDetailZoomThreshold;
                foreach (var s in _filteredSystems) { var sp = WorldToScreen(GetGalaxySystemWorldPosition(s)); if (!rect.Contains(sp)) continue; if (s.Symbol == _selectedSymbol || sa) { var l = new Label(s.Symbol) { style = { position = Position.Absolute, left = sp.x + 10, top = sp.y - 10, color = s.Symbol == _selectedSymbol ? Color.cyan : Color.white, fontSize = 10 } }; if (s.Symbol == _selectedSymbol) l.style.unityFontStyleAndWeight = FontStyle.Bold; _labelContainer.Add(l); } }
            }
            else { if (_currentSystem?.Waypoints == null) return; foreach (var wp in _currentSystem.Waypoints) { var sp = WorldToScreen(GetSystemWaypointWorldPosition(wp)); if (!rect.Contains(sp)) continue; var l = new Label(wp.Symbol) { style = { position = Position.Absolute, left = sp.x + 10, top = sp.y - 10, color = wp.Symbol == _selectedSymbol ? Color.cyan : Color.white, fontSize = 10 } }; if (wp.Symbol == _selectedSymbol) l.style.unityFontStyleAndWeight = FontStyle.Bold; _labelContainer.Add(l); } }
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_mapContainer == null) return;
            var p = mgc.painter2D; var r = _mapContainer.contentRect;
            float lz = Mathf.Log10(50f / Mathf.Max(MapZoom, 0.000001f)), fl = Mathf.Floor(lz);
            float mas = Mathf.Pow(10, fl + 1), mis = Mathf.Pow(10, fl);
            float masi = mas * MapZoom, misi = mis * MapZoom;
            float t = (fl + 1) - lz, ma = Mathf.Clamp01(1.2f - (masi / 1000f)), mi = Mathf.Clamp01((t - 0.2f) / 0.8f);
            if (mi > 0) DrawGridLines(p, r, misi, new Color(0.3f, 0.3f, 0.3f, mi * 0.4f));
            if (ma > 0) DrawGridLines(p, r, masi, new Color(0.6f, 0.6f, 0.6f, ma * 0.6f));
            if (_mapMode == MapMode.Galaxy) { DrawJumpGateConnections(p, r); DrawGalaxySystems(p, r); }
            else DrawSystemWaypoints(p, r);
        }

        private void DrawGridLines(Painter2D p, Rect r, float s, Color c)
        {
            p.strokeColor = c; p.lineWidth = 1f; p.BeginPath();
            float sx = MapOffset.x % s; if (sx < 0) sx += s;
            for (float x = sx; x < r.width; x += s) { p.MoveTo(new Vector2(x, 0)); p.LineTo(new Vector2(x, r.height)); }
            float sy = MapOffset.y % s; if (sy < 0) sy += s;
            for (float y = sy; y < r.height; y += s) { p.MoveTo(new Vector2(0, y)); p.LineTo(new Vector2(r.width, y)); }
            p.Stroke();
        }

        private void DrawJumpGateConnections(Painter2D p, Rect r)
        {
            if (_jumpGateSystemLinks == null) return; p.strokeColor = new Color(0.3f, 0.5f, 1f, 0.2f); p.lineWidth = 1f; p.BeginPath();
            foreach (var l in _jumpGateSystemLinks) { if (!_galaxySystemLookup.TryGetValue(l.FromSystem, out var f) || !_galaxySystemLookup.TryGetValue(l.ToSystem, out var t)) continue; var p1 = WorldToScreen(GetGalaxySystemWorldPosition(f)); var p2 = WorldToScreen(GetGalaxySystemWorldPosition(t)); if (!r.Contains(p1) && !r.Contains(p2)) continue; p.MoveTo(p1); p.LineTo(p2); }
            p.Stroke();
        }

        private void DrawGalaxySystems(Painter2D p, Rect r) { if (_filteredSystems == null) return; foreach (var s in _filteredSystems) { var sp = WorldToScreen(GetGalaxySystemWorldPosition(s)); if (!r.Contains(sp)) continue; DrawIcon(p, sp, GetSystemStyle(s), s.Symbol == _selectedSymbol); } }

        private void DrawSystemWaypoints(Painter2D p, Rect r)
        {
            if (_currentSystem?.Waypoints == null) return;
            foreach (var wp in _currentSystem.Waypoints) { if (string.IsNullOrEmpty(wp.Orbits)) continue; var pa = _currentSystem.Waypoints.FirstOrDefault(x => x.Symbol == wp.Orbits); if (pa == null) continue; var p1 = WorldToScreen(GetSystemWaypointWorldPosition(pa)); var p2 = WorldToScreen(GetSystemWaypointWorldPosition(wp)); p.strokeColor = new Color(1f, 1f, 1f, 0.1f); p.lineWidth = 1f; p.BeginPath(); p.Arc(p1, Vector2.Distance(p1, p2), 0, 360); p.Stroke(); }
            foreach (var wp in _currentSystem.Waypoints) { var sp = WorldToScreen(GetSystemWaypointWorldPosition(wp)); if (!r.Contains(sp)) continue; DrawIcon(p, sp, GetWaypointStyle(wp), wp.Symbol == _selectedSymbol); }
        }

        private void DrawIcon(Painter2D p, Vector2 pos, IconStyle s, bool sel)
        {
            float r = s.Radius * (sel ? 1.5f : 1f); p.fillColor = sel ? Color.cyan : s.FillColor; p.strokeColor = s.StrokeColor; p.lineWidth = s.StrokeWidth; p.BeginPath();
            switch (s.Shape) { case IconShape.Circle: p.Arc(pos, r, 0, 360); break; case IconShape.Square: p.MoveTo(new Vector2(pos.x - r, pos.y - r)); p.LineTo(new Vector2(pos.x + r, pos.y - r)); p.LineTo(new Vector2(pos.x + r, pos.y + r)); p.LineTo(new Vector2(pos.x - r, pos.y + r)); p.ClosePath(); break; case IconShape.Triangle: p.MoveTo(new Vector2(pos.x, pos.y - r)); p.LineTo(new Vector2(pos.x + r, pos.y + r)); p.LineTo(new Vector2(pos.x - r, pos.y + r)); p.ClosePath(); break; case IconShape.Diamond: p.MoveTo(new Vector2(pos.x, pos.y - r)); p.LineTo(new Vector2(pos.x + r, pos.y)); p.LineTo(new Vector2(pos.x, pos.y + r)); p.LineTo(new Vector2(pos.x - r, pos.y)); p.ClosePath(); break; }
            p.Fill(); if (s.StrokeWidth > 0) p.Stroke(); if (s.HasCore) { p.fillColor = s.CoreColor; p.BeginPath(); p.Arc(pos, r * s.CoreRadiusFactor, 0, 360); p.Fill(); }
        }

        private IconStyle GetSystemStyle(DatabaseManager.IndexedSystem s)
        {
            return MapStyleResolver.GetSystemStyle(s);
        }

        private IconStyle GetWaypointStyle(SystemWaypoint w)
        {
            return MapStyleResolver.GetWaypointStyle(w);
        }

        private void HandleMapClick(Vector2 lp)
        {
            var wp = ScreenToWorld(lp);
            float worldThreshold = SelectionScreenRadius / Mathf.Max(MapZoom, 0.01f);
            if (_mapMode == MapMode.Galaxy)
            {
                var closestSystem = FindClosestGalaxySystem(wp, worldThreshold);
                if (closestSystem != null) SelectGalaxySystem(closestSystem);
            }
            else
            {
                var closestWaypoint = FindClosestSystemWaypoint(wp, worldThreshold);
                if (closestWaypoint != null) SelectSystemWaypoint(closestWaypoint);
            }
        }

        private DatabaseManager.IndexedSystem FindClosestGalaxySystem(Vector2 worldPoint, float worldThreshold)
        {
            return MapSelectionMath.FindClosest(
                _filteredSystems,
                worldPoint,
                worldThreshold,
                GetGalaxySystemWorldPosition);
        }

        private SystemWaypoint FindClosestSystemWaypoint(Vector2 worldPoint, float worldThreshold)
        {
            return MapSelectionMath.FindClosest(
                _currentSystem?.Waypoints,
                worldPoint,
                worldThreshold,
                GetSystemWaypointWorldPosition);
        }

        private class MapManipulator : Manipulator
        {
            private readonly MapPresenter _p; private bool _a; private Vector2 _lm;
            public MapManipulator(MapPresenter p) { _p = p; }
            protected override void RegisterCallbacksOnTarget() { target.RegisterCallback<PointerDownEvent>(OnPointerDown); target.RegisterCallback<PointerMoveEvent>(OnPointerMove); target.RegisterCallback<PointerUpEvent>(OnPointerUp); target.RegisterCallback<WheelEvent>(OnWheel); }
            protected override void UnregisterCallbacksFromTarget() { target.UnregisterCallback<PointerDownEvent>(OnPointerDown); target.UnregisterCallback<PointerMoveEvent>(OnPointerMove); target.UnregisterCallback<PointerUpEvent>(OnPointerUp); target.UnregisterCallback<WheelEvent>(OnWheel); }
            private void OnPointerDown(PointerDownEvent e) { if (e.button == 0) { _p.HandleMapClick(e.localPosition); e.StopPropagation(); return; } if (e.button == 1 || e.button == 2) { _a = true; _lm = e.localPosition; target.CapturePointer(e.pointerId); e.StopPropagation(); } }
            private void OnPointerMove(PointerMoveEvent e) { if (_a) { _p.MapOffset += (Vector2)e.localPosition - _lm; _p.RefreshMapUI(); _lm = e.localPosition; e.StopPropagation(); } }
            private void OnPointerUp(PointerUpEvent e) { if (_a && (e.button == 1 || e.button == 2)) { _a = false; target.ReleasePointer(e.pointerId); e.StopPropagation(); } }
            private void OnWheel(WheelEvent e) { float d = -e.delta.y * 0.1f; float oz = _p.MapZoom; _p.MapZoom = Mathf.Clamp(_p.MapZoom * (1f + d), _p._minZoom, _p._maxZoom); _p.MapOffset = e.localMousePosition - ((e.localMousePosition - _p.MapOffset) / oz * _p.MapZoom); _p.RefreshMapUI(); e.StopPropagation(); }
        }
    }

    internal static class MapFilterMatcher
    {
        public static bool MatchesGalaxySystem(DatabaseManager.IndexedSystem system, string query, string typeFilter, string facilityFilter)
        {
            bool matchesQuery = string.IsNullOrEmpty(query) || system.Symbol.Contains(query);
            bool matchesType = IsTypeMatch(system.Type, typeFilter);
            bool matchesFacility = IsFacilityStringMatch(system.KnownFacilities, facilityFilter);
            return matchesQuery && matchesType && matchesFacility;
        }

        public static bool MatchesWaypoint(SystemWaypoint waypoint, Waypoint detailedWaypoint, string search, string typeFilter, string facilityFilter)
        {
            bool matchesSearch = string.IsNullOrEmpty(search) || waypoint.Symbol.Contains(search);
            bool matchesType = IsTypeMatch(waypoint.Type.ToString(), typeFilter);
            bool matchesFacility = IsWaypointFacilityMatch(detailedWaypoint, facilityFilter);
            return matchesSearch && matchesType && matchesFacility;
        }

        private static bool IsTypeMatch(string candidateType, string typeFilter)
        {
            if (typeFilter == "ALL") return true;
            return NormalizeToken(candidateType) == NormalizeToken(typeFilter);
        }

        private static bool IsFacilityStringMatch(string knownFacilities, string facilityFilter)
        {
            if (facilityFilter == "ALL") return true;
            return !string.IsNullOrEmpty(knownFacilities) && knownFacilities.Contains(facilityFilter);
        }

        private static bool IsWaypointFacilityMatch(Waypoint detailedWaypoint, string facilityFilter)
        {
            if (facilityFilter == "ALL") return true;
            if (detailedWaypoint?.Traits == null) return false;

            string normalizedFacility = NormalizeToken(facilityFilter);
            return detailedWaypoint.Traits.Any(t => NormalizeToken(t.Symbol.ToString()) == normalizedFacility);
        }

        private static string NormalizeToken(string value)
        {
            return (value ?? string.Empty).Replace("_", string.Empty).ToUpperInvariant();
        }
    }

    internal static class MapSelectionMath
    {
        public static T FindClosest<T>(IEnumerable<T> items, Vector2 targetPoint, float threshold, Func<T, Vector2> getWorldPosition)
            where T : class
        {
            if (items == null || getWorldPosition == null) return null;

            return items
                .Select(item => (Item: item, Distance: Vector2.Distance(getWorldPosition(item), targetPoint)))
                .Where(x => x.Distance < threshold)
                .OrderBy(x => x.Distance)
                .Select(x => x.Item)
                .FirstOrDefault();
        }
    }

    internal static class MapWaypointTreeFilter
    {
        public static bool HasMatchInSubtree(SystemWaypoint waypoint, Dictionary<string, List<SystemWaypoint>> childMap, Func<SystemWaypoint, bool> isMatch)
        {
            if (waypoint == null || isMatch == null) return false;

            if (isMatch(waypoint)) return true;
            if (childMap == null) return false;
            if (!childMap.TryGetValue(waypoint.Symbol, out var children) || children == null) return false;

            foreach (var child in children)
            {
                if (HasMatchInSubtree(child, childMap, isMatch)) return true;
            }

            return false;
        }
    }

    internal static class MapWaypointDetailLookup
    {
        public static Waypoint FindBySymbol(IEnumerable<Waypoint> detailedWaypoints, string symbol)
        {
            if (detailedWaypoints == null || string.IsNullOrEmpty(symbol)) return null;
            return detailedWaypoints.FirstOrDefault(x => x.Symbol == symbol);
        }
    }

    internal static class MapViewportMath
    {
        public static Vector2 WorldToScreen(Vector2 worldPoint, float zoom, Vector2 offset)
        {
            return worldPoint * zoom + offset;
        }

        public static Vector2 ScreenToWorld(Vector2 localPoint, float zoom, Vector2 offset)
        {
            return (localPoint - offset) / Mathf.Max(zoom, 0.000001f);
        }

        public static Vector2 CenterOnWorldPoint(Rect rect, Vector2 worldPoint, float zoom)
        {
            return (rect.size / 2f) - (worldPoint * zoom);
        }

        public static (Vector2 Offset, float Zoom) FitBounds(IEnumerable<Vector2> points, Rect rect, float minZoom, float maxZoom)
        {
            var list = points.ToList();
            if (list.Count == 0)
            {
                return (rect.size / 2f, 1.0f);
            }

            float minX = list.Min(p => p.x);
            float maxX = list.Max(p => p.x);
            float minY = list.Min(p => p.y);
            float maxY = list.Max(p => p.y);

            float width = Mathf.Max(1f, maxX - minX);
            float height = Mathf.Max(1f, maxY - minY);
            float zoomX = (rect.width * 0.85f) / width;
            float zoomY = (rect.height * 0.85f) / height;
            float zoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), minZoom, maxZoom);

            var center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            var offset = (rect.size / 2f) - (center * zoom);
            return (offset, zoom);
        }
    }

    internal static class MapStyleResolver
    {
        public static MapPresenter.IconStyle GetSystemStyle(DatabaseManager.IndexedSystem system)
        {
            var style = new MapPresenter.IconStyle
            {
                Radius = 3f,
                StrokeWidth = 0,
                FillColor = Color.white,
                Shape = MapPresenter.IconShape.Circle
            };

            string type = (system.Type ?? string.Empty).Replace("_", string.Empty);
            if (type == "REDSTAR") style.FillColor = new Color(1f, 0.4f, 0.4f);
            else if (type == "BLUESTAR") style.FillColor = new Color(0.4f, 0.4f, 1f);
            else if (type == "YOUNGSTAR") style.FillColor = new Color(0.6f, 1f, 1f);
            else if (type == "WHITEDWARF") { style.FillColor = Color.white; style.Radius = 2f; }
            else if (type == "BLACKHOLE") { style.FillColor = Color.black; style.StrokeColor = Color.purple; style.StrokeWidth = 1f; }
            else if (type == "NEBULA") { style.FillColor = new Color(1f, 0.2f, 1f, 0.4f); style.Shape = MapPresenter.IconShape.Square; style.Radius = 5f; }

            return style;
        }

        public static MapPresenter.IconStyle GetWaypointStyle(SystemWaypoint waypoint)
        {
            var style = new MapPresenter.IconStyle
            {
                Radius = 4f,
                StrokeWidth = 0,
                FillColor = Color.white,
                Shape = MapPresenter.IconShape.Circle
            };

            switch (waypoint.Type)
            {
                case WaypointType.PLANET:
                    style.FillColor = new Color(0.2f, 0.6f, 1f);
                    break;
                case WaypointType.MOON:
                    style.FillColor = Color.gray;
                    style.Radius = 2f;
                    break;
                case WaypointType.ORBITALSTATION:
                    style.FillColor = Color.yellow;
                    style.Shape = MapPresenter.IconShape.Square;
                    style.Radius = 3f;
                    break;
                case WaypointType.JUMPGATE:
                    style.FillColor = new Color(0.7f, 0f, 1f);
                    style.Shape = MapPresenter.IconShape.Diamond;
                    break;
                case WaypointType.ASTEROIDFIELD:
                    style.FillColor = new Color(0.5f, 0.4f, 0.3f);
                    style.Shape = MapPresenter.IconShape.Hexagon;
                    break;
            }

            return style;
        }
    }

    internal static class WaypointDescriptionBuilder
    {
        public static string Build(Waypoint waypoint)
        {
            string description = waypoint.Type switch
            {
                WaypointType.PLANET => "Celestial body orbiting a star.",
                WaypointType.MOON => "Satellite orbiting a planet.",
                WaypointType.ORBITALSTATION => "Man-made orbital construct.",
                WaypointType.JUMPGATE => "Fast travel gateway.",
                WaypointType.ASTEROIDFIELD => "Mining region.",
                WaypointType.NEBULA => "Cloud of gas and dust.",
                WaypointType.GASGIANT => "Large gaseous planet.",
                _ => "Location in space."
            };

            if (waypoint.Traits?.Count > 0)
            {
                description += "\n\nTraits: " + string.Join(", ", waypoint.Traits.Select(t => t.Name));
            }

            return description;
        }
    }
}
