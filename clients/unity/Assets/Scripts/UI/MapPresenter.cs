using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;
using SpaceTraders.Core;
using SpaceTraders.UI.Map;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.UI
{
    public class MapPresenter : MonoBehaviour, IMapInteractionHost
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

        private List<IndexedSystem> _allGalaxySystems;
        private List<IndexedSystem> _filteredSystems;
        private List<IndexedSystem> _pagedSystems;
        private List<(string FromSystem, string ToSystem)> _jumpGateSystemLinks = new List<(string, string)>();
        private Dictionary<string, IndexedSystem> _galaxySystemLookup = new Dictionary<string, IndexedSystem>();
        private readonly List<VisualElement> _listEntries = new List<VisualElement>();

        private ISystemIndexRepository _systemIndexRepository;
        private IJumpGateRepository _jumpGateRepository;
        private APIService _apiService;
        private readonly MapRequestVersionGate _systemLoadGate = new MapRequestVersionGate();

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
        private string _pendingExternalSystemSymbol;
        private int _currentPage = 1;
        private const int PageSize = 50;
        private bool _legendExpanded = true;
        private const float GalaxyScale = 6f;
        private const float SystemScale = 5f;
        private const float SelectionScreenRadius = 14f;
        private const float GalaxyIconDetailZoomThreshold = 0.15f;

        [Inject]
        internal void Construct(ISystemIndexRepository systemIndexRepository, IJumpGateRepository jumpGateRepository, APIService apiService)
        {
            _systemIndexRepository = systemIndexRepository;
            _jumpGateRepository = jumpGateRepository;
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

            if (!MapPanelBindings.TryCreate(panel, out var bindings))
            {
                container.Clear();
                container.Add(new Label("Error: System Panel Template missing required elements."));
                return;
            }

            _systemList = bindings.SystemList;
            _mapContainer = bindings.MapContainer;
            _waypointsLayer = bindings.WaypointsLayer;
            _searchField = bindings.SearchField;
            _selectedSystemLabel = bindings.SelectedSystemLabel;
            _systemDetailPanel = bindings.SystemDetailPanel;
            _legendItems = bindings.LegendItems;
            _legendContent = bindings.LegendContent;
            _viewGalaxyButton = bindings.ViewGalaxyButton;
            _prevPageButton = bindings.PrevPageButton;
            _nextPageButton = bindings.NextPageButton;
            _pageInfoLabel = bindings.PageInfoLabel;
            _legendToggleButton = bindings.LegendToggleButton;
            _wpSymbolLabel = bindings.WaypointSymbolLabel;
            _wpTypeLabel = bindings.WaypointTypeLabel;
            _wpCoordsLabel = bindings.WaypointCoordsLabel;
            _wpDescLabel = bindings.WaypointDescLabel;
            _extraInfoTitleLabel = bindings.ExtraInfoTitleLabel;
            _extraContentContainer = bindings.ExtraContentContainer;
            _typeFilter = bindings.TypeFilter;
            _facilityFilter = bindings.FacilityFilter;

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
            _mapContainer.AddManipulator(new MapInteractionManipulator(this));

            // Load Data
            if (_systemIndexRepository != null)
            {
                _allGalaxySystems = _systemIndexRepository.GetAllSystems();
                RebuildGalaxyLookup();
                LoadJumpGateConnections();
            }

            ApplyFilter();
            _ = EnsureGalaxySystemsLoadedAsync();

            RefreshLegend();
            UpdateModeChrome();
            TryApplyPendingExternalSystemSelection();
            
            // Fallback for initial focus
            _mapContainer.schedule.Execute(() => {
                if (!_mapInitialized) { ResetMapCamera(); RefreshMapUI(); }
            }).StartingIn(100);
            
            Log.Info("[MapPresenter] Map panel setup complete.");
        }

        public void FocusSystemFromWaypoint(string waypointSymbol)
        {
            var systemSymbol = GetSystemSymbolFromWaypoint(waypointSymbol);
            FocusSystem(systemSymbol);
        }

        public void FocusSystem(string systemSymbol)
        {
            if (string.IsNullOrWhiteSpace(systemSymbol))
            {
                return;
            }

            _pendingExternalSystemSymbol = systemSymbol;
            TryApplyPendingExternalSystemSelection();
        }

        public static string GetSystemSymbolFromWaypoint(string waypointSymbol)
        {
            if (string.IsNullOrWhiteSpace(waypointSymbol))
            {
                return null;
            }

            var parts = waypointSymbol.Split('-');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}-{parts[1]}";
            }

            return waypointSymbol;
        }

        private void TryApplyPendingExternalSystemSelection()
        {
            if (string.IsNullOrWhiteSpace(_pendingExternalSystemSymbol))
            {
                return;
            }

            if (_mapContainer == null || _systemList == null || _apiService == null)
            {
                return;
            }

            var target = _pendingExternalSystemSymbol;
            _pendingExternalSystemSymbol = null;
            SelectSystem(target);
        }

        private void InitializeFilterOptions()
        {
            if (_typeFilter == null || _facilityFilter == null) return;
            _typeFilter.choices = MapFilterOptions.GetTypeChoices(_mapMode == MapMode.Galaxy);
            _facilityFilter.choices = MapFilterOptions.GetFacilityChoices();
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
            var previousSelectedSymbol = _selectedSymbol;
            _selectedSymbol = null;
            _selectedWaypoint = null;

            if (_mapMode == MapMode.System)
            {
                var targetSystemSymbol = MapModeTransitionResolver.GetSystemLoadTarget(_selectedSystemSymbol, previousSelectedSymbol, _currentSystem);
                if (!string.IsNullOrEmpty(targetSystemSymbol))
                {
                    SelectSystem(targetSystemSymbol);
                    return;
                }
            }

            InitializeFilterOptions();
            UpdateModeChrome();
            PopulateSystemList();
            ResetMapCamera();
            RefreshMapUI();
        }

        private void ChangePage(int delta)
        {
            if (!MapListOrchestration.TryChangePage(_currentPage, delta, _filteredSystems?.Count ?? 0, PageSize, out var nextPage, out _))
            {
                return;
            }

            _currentPage = nextPage;
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
            if (_allGalaxySystems == null) { _filteredSystems = new List<IndexedSystem>(); return; }
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

            try
            {
                _allGalaxySystems = await MapGalaxyDataLoader.LoadIndexedSystemsAsync(_apiService);
                RebuildGalaxyLookup();
                _systemIndexRepository?.StoreSystems(_allGalaxySystems);
                ApplyFilter();
                ResetMapCamera();
                RefreshMapUI();
            }
            catch (Exception e) { Log.Error("[Map] Load failed: {Error}", e.Message); }
        }

        private void RebuildGalaxyLookup() => _galaxySystemLookup = MapGalaxyDataLoader.BuildLookup(_allGalaxySystems);

        private void LoadJumpGateConnections()
        {
            if (_jumpGateRepository == null) return;
            _jumpGateSystemLinks = MapGalaxyDataLoader.BuildJumpGateSystemLinks(_jumpGateRepository.GetAllJumpGateConnections());
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

        private Vector2 GetGalaxySystemWorldPosition(IndexedSystem s) => new Vector2(s.X * GalaxyScale, s.Y * GalaxyScale);
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

            int total = MapListOrchestration.ComputeTotalPages(_filteredSystems.Count, PageSize);
            _currentPage = MapListOrchestration.ClampPage(_currentPage, total);
            _pagedSystems = MapListOrchestration.PageItems(_filteredSystems, _currentPage, PageSize);
            UpdatePageInfo(_currentPage, total);

            foreach (var s in _pagedSystems)
            {
                if (systemEntryTemplate == null) continue;
                var entry = systemEntryTemplate.Instantiate();
                var root = entry.Q<VisualElement>(null, "dashboard-entry") ?? entry;
                root.name = $"list-{s.Symbol}"; root.AddToClassList("selectable-entry");
                entry.Q<Label>("symbol-label").text = s.Symbol;
                entry.Q<Label>("details-label").text = $"{s.Type.Replace("_", " ")} ({s.WaypointCount} WP)";
                if (s.Symbol == _selectedSymbol) root.AddToClassList("selected-entry");
                root.RegisterCallback<ClickEvent>(_ => SelectGalaxySystem(s, root));
                _systemList.Add(entry); _listEntries.Add(root);
            }
        }

        private void SelectGalaxySystem(IndexedSystem s, VisualElement entry = null)
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
            var childMap = MapHierarchyProjection.BuildChildMap(wps);
            var roots = MapHierarchyProjection.BuildRootWaypoints(wps, childMap);
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
            var detailed = MapWaypointDetailLookup.FindBySymbol(_detailedWaypoints, w.Symbol);
            entry.Q<Label>("details-label").text = MapWaypointDetailsPresenter.BuildWaypointListDetails(w, detailed);
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
            int requestToken = _systemLoadGate.Begin();
            _selectedSymbol = sym; _selectedSystemSymbol = sym; ClearListSelection();
            entry ??= _systemList?.Q<VisualElement>($"list-{sym}"); entry?.AddToClassList("selected-entry");
            UpdateModeChrome();
            try
            {
                var res = await _apiService.GetSystem(sym);
                if (!_systemLoadGate.IsCurrent(requestToken)) return;
                if (res?.Data != null)
                {
                    ApplyBasicSystemLoad(res.Data);
                    var wpsRes = await _apiService.GetSystemWaypoints(sym);
                    if (!_systemLoadGate.IsCurrent(requestToken)) return;
                    if (wpsRes?.Data != null)
                    {
                        ApplyDetailedSystemLoad(sym, wpsRes.Data);
                    }
                }
            }
            catch (Exception e) { Log.Error("[Map] System load fail: {Error}", e.Message); }
        }

        private void ApplyBasicSystemLoad(SpaceTraders.Generated.Model.System system)
        {
            _currentSystem = system;
            _currentSystem.Waypoints ??= new List<SystemWaypoint>();
            _mapMode = MapMode.System;
            _selectedWaypoint = null;

            if (_currentSystem.Waypoints.Count > 0)
            {
                SelectSystemWaypoint(_currentSystem.Waypoints[0]);
            }

            InitializeFilterOptions();
            UpdateModeChrome();
            PopulateSystemList();
            ResetMapCamera();
            RefreshMapUI();
        }

        private void ApplyDetailedSystemLoad(string systemSymbol, List<Waypoint> detailedWaypoints)
        {
            _detailedWaypoints = detailedWaypoints;
            _currentSystem.Waypoints = MapDataProjection.ToSystemWaypoints(detailedWaypoints);
            UpdateSystemFacilities(systemSymbol, detailedWaypoints);

            if (_selectedWaypoint == null && detailedWaypoints.Count > 0)
            {
                SelectWaypoint(detailedWaypoints[0]);
            }

            PopulateSystemList();
            RefreshMapUI();
        }

        private void UpdateSystemFacilities(string sym, List<Waypoint> wps)
        {
            var facilitiesCsv = MapDataProjection.ExtractKnownFacilitiesCsv(wps);
            if (string.IsNullOrEmpty(facilitiesCsv)) return;

            var sys = _allGalaxySystems?.FirstOrDefault(s => s.Symbol == sym);
            if (sys == null) return;

            sys.KnownFacilities = facilitiesCsv;
            _systemIndexRepository?.StoreSystems(new[] { sys });
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

        private void ApplyGalaxySystemSelectionDetails(IndexedSystem s)
        {
            MapWaypointDetailsPresenter.ApplyGalaxySystemSelectionDetails(
                _wpSymbolLabel,
                _wpTypeLabel,
                _wpCoordsLabel,
                _wpDescLabel,
                _extraInfoTitleLabel,
                _extraContentContainer,
                s,
                () => SelectSystem(s.Symbol));
        }

        private void ApplySystemWaypointSelectionDetails(SystemWaypoint w)
        {
            MapWaypointDetailsPresenter.ApplySystemWaypointSelectionDetails(
                _wpSymbolLabel,
                _wpTypeLabel,
                _wpCoordsLabel,
                _wpDescLabel,
                _extraContentContainer,
                w);
        }

        private void ApplyWaypointSelectionDetails(Waypoint w)
        {
            MapWaypointDetailsPresenter.ApplyWaypointSelectionDetails(
                _wpSymbolLabel,
                _wpTypeLabel,
                _wpCoordsLabel,
                _wpDescLabel,
                w);
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
                    if (_extraInfoTitleLabel != null) _extraInfoTitleLabel.text = "Marketplace / Actions";

                    var detailedWaypoint = string.Equals(_selectedWaypoint?.Symbol, ws, StringComparison.OrdinalIgnoreCase)
                        ? _selectedWaypoint
                        : MapWaypointDetailLookup.FindBySymbol(_detailedWaypoints, ws);

                    bool hasMarketplace = MapWaypointServiceFacade.HasWaypointTrait(detailedWaypoint, WaypointTraitSymbol.MARKETPLACE);
                    bool hasShipyard = MapWaypointServiceFacade.HasWaypointTrait(detailedWaypoint, WaypointTraitSymbol.SHIPYARD);
                    bool hasConstruction = MapWaypointServiceFacade.HasWaypointTrait(detailedWaypoint, WaypointTraitSymbol.UNDERCONSTRUCTION);

                    var marketTask = hasMarketplace
                        ? MapWaypointServiceFacade.TryGetMarketAsync(_apiService, ss, ws)
                        : Task.FromResult<GetMarket200Response>(null);
                    var shipyardTask = hasShipyard
                        ? MapWaypointServiceFacade.TryGetShipyardAsync(_apiService, ss, ws)
                        : Task.FromResult<GetShipyard200Response>(null);
                    var constructionTask = hasConstruction
                        ? MapWaypointServiceFacade.TryGetConstructionAsync(_apiService, ss, ws)
                        : Task.FromResult<GetConstruction200Response>(null);
                    await Task.WhenAll(marketTask, shipyardTask, constructionTask);

                    var market = marketTask.Result?.Data;
                    var shipyard = shipyardTask.Result?.Data;
                    var construction = constructionTask.Result?.Data;

                    bool hasServices = false;
                    if (market != null)
                    {
                        hasServices = true;
                        MapWaypointDetailsPresenter.AddSectionTitle(_extraContentContainer, "Marketplace");
                        _extraContentContainer.Add(new Label($"Imports: {MapWaypointDetailsPresenter.SummarizeTradeGoods(market.Imports)}"));
                        _extraContentContainer.Add(new Label($"Exports: {MapWaypointDetailsPresenter.SummarizeTradeGoods(market.Exports)}"));
                        _extraContentContainer.Add(new Label($"Exchange: {MapWaypointDetailsPresenter.SummarizeTradeGoods(market.Exchange)}"));
                    }

                    if (shipyard != null || (construction != null && !construction.IsComplete))
                    {
                        hasServices = true;
                        MapWaypointDetailsPresenter.AddSectionTitle(_extraContentContainer, "Waypoint Services");
                        if (shipyard != null)
                        {
                            var openShipyardButton = new Button(() =>
                            {
                                var dashboard = GetComponent<DashboardController>();
                                if (dashboard != null) dashboard.ShowShipyard(ws);
                                else Log.Error("[MapPresenter] DashboardController not found to show shipyard.");
                            }) { text = "OPEN SHIPYARD" };
                            openShipyardButton.AddToClassList("button");
                            openShipyardButton.style.width = 160;
                            openShipyardButton.style.height = 26;
                            _extraContentContainer.Add(openShipyardButton);
                        }

                        if (construction != null && !construction.IsComplete)
                        {
                            _extraContentContainer.Add(new Label("Construction in progress."));
                        }
                    }

                    await PopulateShipInSystemActionsAsync(ss, ws);

                    if (!hasServices)
                    {
                        _extraContentContainer.Add(new Label("No specialized services at this waypoint."));
                    }
                }
            }
            catch (Exception e) { _extraContentContainer?.Clear(); _extraContentContainer?.Add(new Label($"Load error: {e.Message}")); }
        }

        private void UpdateModeChrome()
        {
            if (_viewGalaxyButton != null)
            {
                bool showBackButton = _mapMode == MapMode.System;
                _viewGalaxyButton.style.visibility = showBackButton ? Visibility.Visible : Visibility.Hidden;
                _viewGalaxyButton.SetEnabled(showBackButton);
                _viewGalaxyButton.text = "GALAXY";
            }
            if (_selectedSystemLabel != null) { var st = !string.IsNullOrEmpty(_currentSystem?.Symbol) ? _currentSystem.Symbol : (!string.IsNullOrEmpty(_selectedSystemSymbol) ? _selectedSystemSymbol : _selectedSymbol); _selectedSystemLabel.text = _mapMode == MapMode.System && !string.IsNullOrEmpty(st) ? $"System: {st}" : "Galaxy Map"; }
            if (_legendContent != null) _legendContent.style.display = _legendExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private async Task PopulateShipInSystemActionsAsync(string systemSymbol, string waypointSymbol)
        {
            var ships = await MapWaypointServiceFacade.FetchShipsInSystemAsync(_apiService, systemSymbol);

            MapWaypointDetailsPresenter.AddSectionTitle(_extraContentContainer, "Ship In-System Actions");
            if (ships == null || ships.Count == 0)
            {
                _extraContentContainer.Add(new Label("No owned ships in this system."));
                return;
            }

            foreach (var ship in ships)
            {
                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.SpaceBetween,
                        alignItems = Align.Center,
                        marginTop = 2,
                        marginBottom = 2,
                        paddingLeft = 4,
                        paddingRight = 4
                    }
                };

                string status = ship.Nav?.Status.ToString().Replace("_", " ") ?? "UNKNOWN";
                var shipLabel = new Label($"{ship.Symbol} ({status})")
                {
                    style =
                    {
                        flexGrow = 1,
                        fontSize = 10
                    }
                };
                row.Add(shipLabel);

                var navButton = new Button { text = "NAV HERE" };
                navButton.AddToClassList("button");
                navButton.style.width = 90;
                navButton.style.height = 24;
                navButton.style.fontSize = 10;

                bool inOrbit = ship.Nav?.Status == ShipNavStatus.INORBIT;
                bool alreadyThere = string.Equals(ship.Nav?.WaypointSymbol, waypointSymbol, StringComparison.OrdinalIgnoreCase);
                navButton.SetEnabled(inOrbit && !alreadyThere);
                if (alreadyThere)
                {
                    navButton.text = "HERE";
                }

                navButton.clicked += async () => await NavigateShipToWaypointAsync(ship, waypointSymbol, navButton);

                row.Add(navButton);
                _extraContentContainer.Add(row);
            }
        }

        private async Task NavigateShipToWaypointAsync(Ship ship, string waypointSymbol, Button navButton)
        {
            if (ship?.Nav?.Status != ShipNavStatus.INORBIT || string.IsNullOrWhiteSpace(waypointSymbol))
            {
                return;
            }

            try
            {
                navButton.SetEnabled(false);

                var latestShipResponse = await _apiService.GetShip(ship.Symbol);
                var latestShip = latestShipResponse?.Data;
                if (latestShip?.Nav == null)
                {
                    Log.Warning("[MapPresenter] Unable to validate latest nav state for {Ship}.", ship.Symbol);
                    return;
                }

                if (latestShip.Nav.Status != ShipNavStatus.INORBIT)
                {
                    Log.Warning("[MapPresenter] Navigation blocked for {Ship}: ship status is {Status}.", ship.Symbol, latestShip.Nav.Status);
                    return;
                }

                if (string.Equals(latestShip.Nav.WaypointSymbol, waypointSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Info("[MapPresenter] Navigation skipped for {Ship}: already at {Waypoint}.", ship.Symbol, waypointSymbol);
                    return;
                }

                var destinationSystem = GetSystemSymbolFromWaypoint(waypointSymbol);
                if (!string.Equals(latestShip.Nav.SystemSymbol, destinationSystem, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning("[MapPresenter] Navigation blocked for {Ship}: destination {Waypoint} is outside current system {System}.", ship.Symbol, waypointSymbol, latestShip.Nav.SystemSymbol);
                    return;
                }

                int availableFuel = latestShip.Fuel?.Current ?? 0;
                int estimatedFuelRequired = MapWaypointServiceFacade.EstimateFuelRequired(
                    latestShip.Nav.WaypointSymbol,
                    waypointSymbol,
                    _currentSystem?.Waypoints,
                    _detailedWaypoints);
                if (estimatedFuelRequired > 0 && availableFuel < estimatedFuelRequired)
                {
                    Log.Warning("[MapPresenter] Navigation blocked for {Ship}: requires ~{Required} fuel but only {Available} available.", ship.Symbol, estimatedFuelRequired, availableFuel);
                    return;
                }

                await _apiService.NavigateShip(ship.Symbol, waypointSymbol);
                _ = UpdateSpecializedInfoAsync(waypointSymbol, _selectedWaypoint?.Type.ToString() ?? string.Empty);
            }
            catch (SpaceTraders.Generated.Client.ApiException e)
            {
                string responseContent = e.ErrorContent?.ToString() ?? "<empty>";
                Log.Error("[MapPresenter] Failed to navigate {Ship} to {Waypoint}: {Code} {Message}. Response: {Content}", ship.Symbol, waypointSymbol, e.ErrorCode, e.Message, responseContent);
            }
            catch (Exception e)
            {
                Log.Error("[MapPresenter] Failed to navigate {Ship} to {Waypoint}: {Error}", ship.Symbol, waypointSymbol, e.Message);
            }
            finally
            {
                navButton.SetEnabled(ship?.Nav?.Status == ShipNavStatus.INORBIT);
            }
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

        private IconStyle GetSystemStyle(IndexedSystem s)
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

        private IndexedSystem FindClosestGalaxySystem(Vector2 worldPoint, float worldThreshold)
        {
            return MapSelectionMath.FindClosest(
                _filteredSystems,
                worldPoint,
                worldThreshold,
                GetGalaxySystemWorldPosition);
        }

        internal IndexedSystem FindClosestGalaxySystemForTest(Vector2 worldPoint, float worldThreshold)
        {
            return FindClosestGalaxySystem(worldPoint, worldThreshold);
        }

        private SystemWaypoint FindClosestSystemWaypoint(Vector2 worldPoint, float worldThreshold)
        {
            return MapSelectionMath.FindClosest(
                _currentSystem?.Waypoints,
                worldPoint,
                worldThreshold,
                GetSystemWaypointWorldPosition);
        }

        internal SystemWaypoint FindClosestSystemWaypointForTest(Vector2 worldPoint, float worldThreshold)
        {
            return FindClosestSystemWaypoint(worldPoint, worldThreshold);
        }

        internal void HandleMapClickForTest(Vector2 localPoint)
        {
            HandleMapClick(localPoint);
        }

        internal void SetFilteredSystemsForTest(List<IndexedSystem> systems)
        {
            _filteredSystems = systems;
        }

        internal void SetCurrentSystemForTest(SpaceTraders.Generated.Model.System system)
        {
            _currentSystem = system;
        }

        internal void SetMapModeForTest(bool systemMode)
        {
            _mapMode = systemMode ? MapMode.System : MapMode.Galaxy;
        }

        internal string GetSelectedSymbolForTest()
        {
            return _selectedSymbol;
        }

        internal string GetSelectedSystemSymbolForTest()
        {
            return _selectedSystemSymbol;
        }

        internal string GetPendingExternalSystemSymbolForTest()
        {
            return _pendingExternalSystemSymbol;
        }

        float IMapInteractionHost.MinZoom => _minZoom;

        float IMapInteractionHost.MaxZoom => _maxZoom;

        void IMapInteractionHost.HandleMapClickFromInteraction(Vector2 localPosition)
        {
            HandleMapClick(localPosition);
        }

        void IMapInteractionHost.RefreshMapFromInteraction()
        {
            RefreshMapUI();
        }
    }
}
