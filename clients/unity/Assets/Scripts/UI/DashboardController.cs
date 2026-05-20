using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private enum Tab { Agent, Contracts, Fleet, Map, Factions }
        private enum MapMode { Galaxy, System }

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
        private MapMode _mapMode = MapMode.Galaxy;
        private VisualElement _dataContainer;
        private Label _statusLabel;
        private Dictionary<Tab, Button> _tabButtons = new Dictionary<Tab, Button>();

        // Map Panel References
        private VisualElement _mapContainer, _labelContainer, _legendItems;
        private VisualElement _legendContent;
        private Button _legendToggle;
        private Label _selectedSystemTitle, _selectedSystemSubtitle, _mapHeading;
        private Label _wpSymbol, _wpType, _wpCoords, _wpDesc, _extraInfoTitle;
        private VisualElement _extraContentContainer;
        private List<VisualElement> _listEntries = new List<VisualElement>();
        private Button _prevPageBtn, _nextPageBtn, _viewGalaxyBtn;
        private Label _pageInfoLabel;
        private TextField _systemSearch;
        
        // Map State
        private Vector2 _mapOffset = Vector2.zero;
        private float _mapZoom = 1.0f;
        private SystemData _currentSystem;
        private List<DatabaseManager.IndexedSystem> _allGalaxySystems = new List<DatabaseManager.IndexedSystem>();
        private List<DatabaseManager.IndexedSystem> _filteredGalaxySystems = new List<DatabaseManager.IndexedSystem>();
        private List<DatabaseManager.IndexedSystem> _pagedGalaxySystems = new List<DatabaseManager.IndexedSystem>();
        private List<Ship> _playerShips = new List<Ship>();
        private bool _mapInitialized = false;
        private bool _showRoutes = true;
        private bool _legendExpanded = true;

        private int _currentSystemsPage = 1;
        private int _totalSystemsPages = 1;
        private const int SystemsPerPage = 20;

        // Selection State
        private string _selectedSymbol = null;

        // Async Safety
        private int _requestSequence = 0;

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;
            _dataContainer = root.Q<VisualElement>("data-container");
            _statusLabel = root.Q<Label>("status-label");

            RegisterTab(Tab.Agent, root.Q<Button>("tab-agent"));
            RegisterTab(Tab.Contracts, root.Q<Button>("tab-contracts"));
            RegisterTab(Tab.Fleet, root.Q<Button>("tab-fleet"));
            RegisterTab(Tab.Map, root.Q<Button>("tab-map"));
            RegisterTab(Tab.Factions, root.Q<Button>("tab-factions"));

            root.Q<Button>("back-button").clicked += () => SceneManager.LoadScene(mainMenuSceneName);
            SpaceTradersClient.Instance.SetToken(AuthManager.Instance.AgentToken);
            SwitchTab(Tab.Agent);
        }

        private void RegisterTab(Tab tab, Button button)
        {
            if (button == null) return;
            _tabButtons[tab] = button;
            button.clicked += () => SwitchTab(tab);
        }

        private void SwitchTab(Tab tab)
        {
            _currentTab = tab;
            _requestSequence++; 
            int sequence = _requestSequence;
            
            foreach (var kvp in _tabButtons)
            {
                if (kvp.Key == tab) kvp.Value.AddToClassList("tab-button--selected");
                else kvp.Value.RemoveFromClassList("tab-button--selected");
            }

            _dataContainer.Clear();
            _statusLabel.text = string.Empty;

            if (tab == Tab.Map)
            {
                _allGalaxySystems = DatabaseManager.Instance.GetAllSystems();
                _ = SetupMapPanelAsync();
                return;
            }

            _ = FetchAndDisplayTab(tab, sequence);
        }

        private async Task FetchAndDisplayTab(Tab tab, int sequence)
        {
            _statusLabel.text = "Fetching data...";
            try
            {
                switch (tab)
                {
                    case Tab.Agent:
                        var agent = await APIService.Instance.GetMyAgent();
                        if (sequence == _requestSequence) DisplayAgent(agent.data);
                        break;
                    case Tab.Contracts:
                        var contracts = await APIService.Instance.GetContracts();
                        if (sequence == _requestSequence) DisplayList(Tab.Contracts, contracts.data);
                        break;
                    case Tab.Fleet:
                        var ships = await APIService.Instance.GetShips();
                        if (sequence == _requestSequence) {
                            _playerShips = ships.data.ToList();
                            DisplayList(Tab.Fleet, ships.data);
                        }
                        break;
                    case Tab.Factions:
                        var factions = await APIService.Instance.GetFactions();
                        if (sequence == _requestSequence) DisplayList(Tab.Factions, factions.data);
                        break;
                }
                if (sequence == _requestSequence) _statusLabel.text = string.Empty;
            }
            catch (Exception e)
            {
                if (sequence == _requestSequence) _statusLabel.text = $"Error: {e.Message}";
            }
        }

        private VisualElement GetContentRoot()
        {
            if (_currentTab == Tab.Map) return _dataContainer;
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            _dataContainer.Add(scroll);
            return scroll;
        }

        private void DisplayAgent(Agent agent)
        {
            var root = GetContentRoot();
            AddRow(root, "Symbol", agent.symbol);
            AddRow(root, "Headquarters", agent.headquarters);
            AddRow(root, "Credits", agent.credits.ToString("N0"));
            AddRow(root, "Starting Faction", agent.startingFaction);
            AddRow(root, "AccountId", agent.accountId);
        }

        private void DisplayList<T>(Tab tab, T[] items)
        {
            if (items == null || items.Length == 0)
            {
                _dataContainer.Add(new Label("No items found."));
                return;
            }

            var root = GetContentRoot();
            foreach (var item in items)
            {
                VisualElement entry = null;
                if (item is Contract c) entry = BindContract(c);
                else if (item is Ship s) entry = BindShip(s);
                else if (item is Faction f) entry = BindFaction(f);
                if (entry != null) root.Add(entry);
            }
        }

        private async Task SetupMapPanelAsync()
        {
            _mapInitialized = false;
            if (_playerShips.Count == 0)
            {
                try { var ships = await APIService.Instance.GetShips(); _playerShips = ships.data.ToList(); } catch { }
            }

            var panel = systemPanelTemplate.Instantiate();
            panel.style.flexGrow = 1;
            _dataContainer.Add(panel);

            _mapHeading = panel.Q<Label>("map-heading");
            _mapContainer = panel.Q<VisualElement>("map-container");
            _labelContainer = new VisualElement { style = { position = Position.Absolute, width = Length.Percent(100), height = Length.Percent(100) }, pickingMode = PickingMode.Ignore };
            _mapContainer.Add(_labelContainer);

            _legendContent = panel.Q<VisualElement>("legend-content");
            _legendToggle = panel.Q<Button>("legend-toggle");
            _legendItems = panel.Q<VisualElement>("legend-items");
            _legendToggle.clicked += ToggleLegend;

            _selectedSystemTitle = panel.Q<Label>("selected-system-title");
            _selectedSystemSubtitle = new Label("-") { style = { fontSize = 14, color = Color.gray, marginTop = -5 } };
            _selectedSystemTitle.parent.Add(_selectedSystemSubtitle);
            _selectedSystemSubtitle.SendToBack(); _selectedSystemTitle.SendToBack();

            _wpSymbol = panel.Q<Label>("wp-symbol");
            _wpType = panel.Q<Label>("wp-type");
            _wpCoords = panel.Q<Label>("wp-coords");
            _wpDesc = panel.Q<Label>("wp-desc");
            _extraInfoTitle = panel.Q<Label>("extra-info-title");
            _extraContentContainer = panel.Q<VisualElement>("extra-content-container");
            
            _prevPageBtn = panel.Q<Button>("prev-page");
            _nextPageBtn = panel.Q<Button>("next-page");
            _viewGalaxyBtn = panel.Q<Button>("view-galaxy-btn");
            _pageInfoLabel = panel.Q<Label>("page-info");
            _systemSearch = panel.Q<TextField>("system-search");

            _viewGalaxyBtn.clicked += () => SwitchMapMode(MapMode.Galaxy);
            _systemSearch.RegisterValueChangedCallback(evt => { _currentSystemsPage = 1; RefreshMapList(true); });

            _prevPageBtn.clicked += () => { _currentSystemsPage--; RefreshMapList(false); };
            _nextPageBtn.clicked += () => { _currentSystemsPage++; RefreshMapList(false); };

            _mapContainer.generateVisualContent += OnGenerateVisualContent;
            _mapContainer.AddManipulator(new MapManipulator(this));
            _mapContainer.RegisterCallback<GeometryChangedEvent>(OnMapGeometryChanged);
            _mapContainer.RegisterCallback<PointerDownEvent>(OnMapClick);

            UpdateLegend();
            
            // If entering from a specific system (e.g. via ship), don't reset view to galaxy
            if (_mapMode == MapMode.System && _currentSystem != null)
            {
                _mapHeading.text = "System Map";
                _viewGalaxyBtn.style.display = DisplayStyle.Flex;
                RefreshMapList(true);
            }
            else
            {
                _mapMode = MapMode.Galaxy;
                RefreshMapList(true);
            }
        }

        private void ToggleLegend()
        {
            _legendExpanded = !_legendExpanded;
            _legendContent.style.display = _legendExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            _legendToggle.text = _legendExpanded ? "-" : "+";
        }

        private void UpdateLegend()
        {
            if (_legendItems == null) return;
            _legendItems.Clear();
            if (_mapMode == MapMode.Galaxy)
            {
                AddLegendItem("ORANGE_STAR", Color.white, "Star (Yellow/Orange)");
                AddLegendItem("RED_STAR", new Color(1f, 0.3f, 0.3f), "Star (Red)");
                AddLegendItem("BLUE_STAR", new Color(0.3f, 0.6f, 1f), "Star (Blue)");
                AddLegendItem("YOUNG_STAR", new Color(0.6f, 1f, 1f), "Young Star");
                AddLegendItem("NEBULA", new Color(1f, 0.5f, 1f), "Nebula");
            }
            else
            {
                AddLegendItem("PLANET", new Color(0, 0.6f, 1f), "Planet");
                AddLegendItem("MOON", Color.gray, "Moon");
                AddLegendItem("STATION", Color.yellow, "Station");
                AddLegendItem("GATE", new Color(0.8f, 0, 1f), "Jump Gate");
                AddLegendItem("FIELD", new Color(0.4f, 0.3f, 0.2f), "Asteroids");
            }
            AddLegendItem("SELECTED", Color.cyan, "Selected");
        }

        private void AddLegendItem(string label, Color color, string desc)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            var box = new VisualElement { style = { width = 8, height = 8, backgroundColor = color, marginRight = 5 } };
            float radius = (label == "STATION") ? 0 : 4;
            box.style.borderTopLeftRadius = radius; box.style.borderTopRightRadius = radius;
            box.style.borderBottomLeftRadius = radius; box.style.borderBottomRightRadius = radius;
            if (label == "GATE") {
                box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1; box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1;
                box.style.borderLeftColor = Color.white; box.style.borderRightColor = Color.white;
                box.style.borderTopColor = Color.white; box.style.borderBottomColor = Color.white;
            }
            row.Add(box);
            row.Add(new Label(desc) { style = { fontSize = 9, color = Color.white } });
            _legendItems.Add(row);
        }

        private void SwitchMapMode(MapMode mode)
        {
            _mapMode = mode;
            _mapInitialized = false;
            _mapHeading.text = mode == MapMode.Galaxy ? "Galaxy Map" : "System Map";
            _viewGalaxyBtn.style.display = mode == MapMode.Galaxy ? DisplayStyle.None : DisplayStyle.Flex;
            if (mode == MapMode.Galaxy) _currentSystem = null;
            _selectedSymbol = null;
            UpdateLegend();
            RefreshMapList(true);
        }

        private void RefreshMapList(bool resetView)
        {
            var listContainer = _dataContainer.Q<ScrollView>("system-list");
            if (listContainer == null) return;
            listContainer.Clear();
            _listEntries.Clear();

            string search = _systemSearch.value?.ToUpper() ?? "";

            if (_mapMode == MapMode.Galaxy)
            {
                _filteredGalaxySystems = string.IsNullOrEmpty(search) 
                    ? _allGalaxySystems 
                    : _allGalaxySystems.Where(s => s.Symbol.Contains(search)).ToList();

                _totalSystemsPages = (int)Math.Ceiling((double)_filteredGalaxySystems.Count / SystemsPerPage);
                if (_currentSystemsPage > _totalSystemsPages) _currentSystemsPage = Math.Max(1, _totalSystemsPages);

                _pagedGalaxySystems = _filteredGalaxySystems.Skip((_currentSystemsPage - 1) * SystemsPerPage).Take(SystemsPerPage).ToList();

                foreach (var sys in _pagedGalaxySystems)
                {
                    var entry = systemTemplate.Instantiate();
                    var root = entry.Q<VisualElement>(null, "dashboard-entry");
                    root.name = $"list-{sys.Symbol}";
                    root.AddToClassList("selectable-entry");
                    entry.Q<Label>("symbol-label").text = sys.Symbol;
                    entry.Q<Label>("details-label").text = $"{sys.Type} ({sys.WaypointCount} WP)";
                    if (sys.Symbol == _selectedSymbol) root.AddToClassList("selected-entry");
                    root.RegisterCallback<ClickEvent>(evt => SelectGalaxySystem(sys, root, false));
                    listContainer.Add(entry);
                    _listEntries.Add(root);
                }

                _pageInfoLabel.text = $"{_currentSystemsPage}/{_totalSystemsPages}";
                _prevPageBtn.SetEnabled(_currentSystemsPage > 1);
                _nextPageBtn.SetEnabled(_currentSystemsPage < _totalSystemsPages);

                if (_selectedSymbol == null && _pagedGalaxySystems.Count > 0) 
                    SelectGalaxySystem(_pagedGalaxySystems[0], _listEntries[0], resetView);
                else if (resetView)
                    ResetMap(); 
            }
            else
            {
                if (_currentSystem == null) return;
                var waypoints = _currentSystem.waypoints.ToList();
                var rootWaypoints = waypoints.Where(w => string.IsNullOrEmpty(w.orbits)).ToList();
                _pageInfoLabel.text = "1/1";
                _prevPageBtn.SetEnabled(false); _nextPageBtn.SetEnabled(false);

                foreach (var wp in rootWaypoints)
                {
                    bool matchesSelf = string.IsNullOrEmpty(search) || wp.symbol.Contains(search);
                    var children = waypoints.Where(w => w.orbits == wp.symbol).ToList();
                    var matchingChildren = children.Where(c => string.IsNullOrEmpty(search) || c.symbol.Contains(search)).ToList();
                    if (matchesSelf || matchingChildren.Count > 0)
                    {
                        AddSystemListEntry(listContainer, wp, 0, search);
                        var toShow = matchesSelf ? children : matchingChildren;
                        foreach(var child in toShow) AddSystemListEntry(listContainer, child, 1, search);
                    }
                }
                if (resetView) ResetMap();
            }
            RefreshMapUI();
        }

        private void AddSystemListEntry(VisualElement container, SystemWaypoint wp, int indent, string search)
        {
            var entry = systemTemplate.Instantiate();
            var root = entry.Q<VisualElement>(null, "dashboard-entry");
            root.name = $"list-{wp.symbol}";
            root.AddToClassList("selectable-entry");
            root.style.marginLeft = indent * 15;
            entry.Q<Label>("symbol-label").text = (indent > 0 ? "↳ " : "") + wp.symbol;
            entry.Q<Label>("details-label").text = wp.type;
            if (wp.symbol == _selectedSymbol) root.AddToClassList("selected-entry");
            root.RegisterCallback<ClickEvent>(evt => SelectWaypoint(wp, root, false));
            container.Add(entry);
            _listEntries.Add(root);
        }

        private void SelectGalaxySystem(DatabaseManager.IndexedSystem sys, VisualElement entryRoot, bool focus)
        {
            _selectedSymbol = sys.Symbol;
            foreach (var e in _listEntries) e.RemoveFromClassList("selected-entry");
            entryRoot?.AddToClassList("selected-entry");
            if (entryRoot != null) _dataContainer.Q<ScrollView>("system-list").ScrollTo(entryRoot);

            _selectedSystemTitle.text = sys.Symbol;
            _selectedSystemSubtitle.text = sys.Type;
            _wpSymbol.text = sys.Symbol;
            _wpType = _wpType ?? _dataContainer.Q<Label>("wp-type");
            _wpType.text = sys.Type;
            _wpCoords.text = $"({sys.X}, {sys.Y})";
            _wpDesc.text = "Click 'OPEN SYSTEM' to view internal waypoints.";
            _extraContentContainer.Clear();
            _extraInfoTitle.text = "Actions";
            var openBtn = new Button(() => _ = OpenSystem(sys.Symbol)) { text = "OPEN SYSTEM" };
            openBtn.AddToClassList("button");
            openBtn.style.width = 150; openBtn.style.height = 30;
            openBtn.style.marginTop = 0; openBtn.style.marginBottom = 0;
            _extraContentContainer.Add(openBtn);
            
            if (focus) CenterOnPoint(new Vector2(sys.X, sys.Y));
            RefreshMapUI();
        }

        private void SelectWaypoint(SystemWaypoint wp, VisualElement entryRoot, bool focus)
        {
            _selectedSymbol = wp.symbol;
            foreach (var e in _listEntries) e.RemoveFromClassList("selected-entry");
            entryRoot?.AddToClassList("selected-entry");
            if (entryRoot != null) _dataContainer.Q<ScrollView>("system-list").ScrollTo(entryRoot);

            _wpSymbol.text = wp.symbol;
            _wpType.text = wp.type;
            _wpCoords.text = $"({wp.x}, {wp.y})";
            _wpDesc.text = GetWaypointDescription(wp.type);
            _extraInfoTitle.text = "Checking access...";
            _extraContentContainer.Clear();
            
            bool hasAccess = _playerShips.Any(s => s.nav.systemSymbol == (_currentSystem?.symbol ?? _selectedSymbol.Split('-')[0]));
            if (hasAccess) _ = FetchWaypointDetails(wp);
            else _extraInfoTitle.text = "System Inaccessible (No Ships)";

            if (focus) CenterOnPoint(GetVisualWaypointPos(wp));
            RefreshMapUI();
        }

        private void CenterOnPoint(Vector2 point)
        {
            Vector2 screenPos = point * _mapZoom + _mapOffset;
            var rect = _mapContainer.contentRect;
            if (!rect.Contains(screenPos)) _mapOffset = new Vector2(rect.width / 2f, rect.height / 2f) - (point * _mapZoom);
        }

        private async Task OpenSystem(string symbol, string focusWaypoint = null)
        {
            _statusLabel.text = $"Opening {symbol}...";
            try
            {
                var res = await APIService.Instance.GetSystem(symbol);
                if (res != null && res.data != null)
                {
                    _currentSystem = res.data;
                    _selectedSymbol = focusWaypoint; 
                    _mapMode = MapMode.System; // Pre-set mode for SetupMapPanelAsync
                    SwitchTab(Tab.Map);
                    
                    if (!string.IsNullOrEmpty(focusWaypoint))
                    {
                        var wp = _currentSystem.waypoints.FirstOrDefault(w => w.symbol == focusWaypoint);
                        if (wp != null) SelectWaypoint(wp, null, true);
                    }
                }
                else { _statusLabel.text = "System details not found."; }
            }
            catch (Exception e) { _statusLabel.text = $"Error: {e.Message}"; }
        }

        private void OnMapGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_mapInitialized && (_mapMode == MapMode.Galaxy || _currentSystem != null))
            {
                ResetMap(); _mapInitialized = true;
            }
        }

        private void OnMapClick(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            Vector2 mousePos = evt.localPosition;
            float threshold = 12f;

            if (_mapMode == MapMode.Galaxy)
            {
                foreach (var sys in _pagedGalaxySystems.Concat(_filteredGalaxySystems))
                {
                    Vector2 screenPos = new Vector2(sys.X, sys.Y) * _mapZoom + _mapOffset;
                    if (Vector2.Distance(mousePos, screenPos) < threshold)
                    {
                        if (!_pagedGalaxySystems.Contains(sys))
                        {
                            int idx = _filteredGalaxySystems.IndexOf(sys);
                            _currentSystemsPage = (idx / SystemsPerPage) + 1;
                            RefreshMapList(false);
                        }
                        var entry = _listEntries.FirstOrDefault(e => e.name == $"list-{sys.Symbol}");
                        SelectGalaxySystem(sys, entry, true);
                        break;
                    }
                }
            }
            else
            {
                if (_currentSystem == null) return;
                foreach (var wp in _currentSystem.waypoints)
                {
                    Vector2 screenPos = GetVisualWaypointPos(wp) * _mapZoom + _mapOffset;
                    if (Vector2.Distance(mousePos, screenPos) < threshold)
                    {
                        var entry = _listEntries.FirstOrDefault(e => e.name == $"list-{wp.symbol}");
                        SelectWaypoint(wp, entry, true);
                        break;
                    }
                }
            }
        }

        private void ResetMap()
        {
            _mapOffset = Vector2.zero; _mapZoom = 1.0f;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

            if (_mapMode == MapMode.Galaxy)
            {
                var systems = _filteredGalaxySystems.Count > 0 ? _filteredGalaxySystems : _allGalaxySystems;
                if (systems.Count == 0) return;
                foreach (var s in systems) { minX = Math.Min(minX, s.X); maxX = Math.Max(maxX, s.X); minY = Math.Min(minY, s.Y); maxY = Math.Max(maxY, s.Y); }
            }
            else
            {
                if (_currentSystem == null || _currentSystem.waypoints.Length == 0) return;
                foreach (var wp in _currentSystem.waypoints) {
                    Vector2 vPos = GetVisualWaypointPos(wp);
                    minX = Math.Min(minX, vPos.x); maxX = Math.Max(maxX, vPos.x); minY = Math.Min(minY, vPos.y); maxY = Math.Max(maxY, vPos.y);
                }
            }

            float width = _mapContainer.layout.width, height = _mapContainer.layout.height;
            if (width <= 0 || height <= 0) return;
            float zoomX = (width * 0.8f) / Math.Max(100, maxX - minX);
            float zoomY = (height * 0.8f) / Math.Max(100, maxY - minY);
            _mapZoom = Math.Min(zoomX, zoomY);
            if (_mapZoom <= 0) _mapZoom = 1.0f;
            _mapOffset = new Vector2(width / 2f, height / 2f) - (new Vector2(minX + maxX, minY + maxY) / 2f * _mapZoom);
        }

        private void RefreshMapUI() { _mapContainer?.MarkDirtyRepaint(); UpdateMapLabels(); }

        private float GetOrbitalRadius(SystemWaypoint wp)
        {
            if (string.IsNullOrEmpty(wp.orbits)) return 0;
            var parent = _currentSystem.waypoints.FirstOrDefault(w => w.symbol == wp.orbits);
            if (parent == null || parent.orbitals == null) return 20f;
            int index = Array.FindIndex(parent.orbitals, o => o.symbol == wp.symbol);
            return 25f + (Mathf.Max(0, index) * 15f);
        }

        private Vector2 GetVisualWaypointPos(SystemWaypoint wp)
        {
            if (_currentSystem == null || string.IsNullOrEmpty(wp.orbits)) return new Vector2(wp.x, wp.y);
            var parent = _currentSystem.waypoints.FirstOrDefault(w => w.symbol == wp.orbits);
            if (parent == null) return new Vector2(wp.x, wp.y);
            
            Vector2 parentPos = GetVisualWaypointPos(parent);
            float radius = GetOrbitalRadius(wp);
            int index = parent.orbitals != null ? Array.FindIndex(parent.orbitals, o => o.symbol == wp.symbol) : -1;
            float angle = (Mathf.Max(0, index) * 45f + 30f) * Mathf.Deg2Rad;
            return parentPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private void UpdateMapLabels()
        {
            if (_labelContainer == null) return;
            _labelContainer.Clear();
            var rect = _mapContainer.contentRect;
            if (_mapMode == MapMode.Galaxy)
            {
                bool showAllLabels = _mapZoom > 3.5f;
                foreach (var sys in _filteredGalaxySystems)
                {
                    Vector2 pos = new Vector2(sys.X, sys.Y) * _mapZoom + _mapOffset;
                    if (!rect.Contains(pos)) continue;
                    if (sys.Symbol == _selectedSymbol || showAllLabels) _labelContainer.Add(GetLabelFromPool(sys.Symbol, pos, sys.Symbol == _selectedSymbol ? Color.cyan : Color.white));
                }
            }
            else
            {
                if (_currentSystem == null) return;
                foreach (var wp in _currentSystem.waypoints)
                {
                    Vector2 pos = GetVisualWaypointPos(wp) * _mapZoom + _mapOffset;
                    if (!rect.Contains(pos)) continue;
                    _labelContainer.Add(GetLabelFromPool(wp.symbol, pos, wp.symbol == _selectedSymbol ? Color.cyan : Color.white));
                }
            }
        }

        private Label GetLabelFromPool(string text, Vector2 pos, Color color)
        {
            var l = new Label(text) { style = { position = Position.Absolute, left = pos.x + 8, top = pos.y - 8, color = color, fontSize = 10, unityFontStyleAndWeight = color == Color.cyan ? FontStyle.Bold : FontStyle.Normal } };
            return l;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D; var rect = _mapContainer.contentRect;
            float logZoom = Mathf.Log10(50f / _mapZoom), floorLog = Mathf.Floor(logZoom);
            float majorScale = Mathf.Pow(10, floorLog + 1), minorScale = Mathf.Pow(10, floorLog);
            float majorSize = majorScale * _mapZoom, minorSize = minorScale * _mapZoom;
            float t = (floorLog + 1) - logZoom, minorAlpha = Mathf.Clamp01((t - 0.2f) / 0.8f), majorAlpha = Mathf.Clamp01(1.2f - (majorSize / 1000f));
            if (minorAlpha > 0) DrawLines(painter, rect, minorSize, new Color(0.2f, 0.2f, 0.2f, minorAlpha * 0.2f));
            if (majorAlpha > 0) DrawLines(painter, rect, majorSize, new Color(0.5f, 0.5f, 0.5f, majorAlpha * 0.5f));
            if (_mapMode == MapMode.Galaxy) DrawGalaxyBulk(painter, rect); else DrawSystemBulk(painter, rect);
        }

        private void DrawGalaxyBulk(Painter2D painter, Rect rect)
        {
            if (_showRoutes && _filteredGalaxySystems.Count > 1)
            {
                painter.strokeColor = new Color(0, 0.5f, 1.0f, 0.15f); painter.lineWidth = 1f;
                var systems = _filteredGalaxySystems.Count < 500 ? _filteredGalaxySystems : _pagedGalaxySystems;
                for (int i = 0; i < systems.Count; i++)
                {
                    var s1 = systems[i]; Vector2 p1 = new Vector2(s1.X, s1.Y) * _mapZoom + _mapOffset;
                    if (!rect.Contains(p1)) continue;
                    for (int j = i + 1; j < Math.Min(i + 10, systems.Count); j++)
                    {
                        var s2 = systems[j]; float dist = Math.Abs(s1.X - s2.X) + Math.Abs(s1.Y - s2.Y);
                        if (dist < 150) { Vector2 p2 = new Vector2(s2.X, s2.Y) * _mapZoom + _mapOffset; painter.BeginPath(); painter.MoveTo(p1); painter.LineTo(p2); painter.Stroke(); }
                    }
                }
            }
            foreach (var sys in _filteredGalaxySystems)
            {
                Vector2 pos = new Vector2(sys.X, sys.Y) * _mapZoom + _mapOffset;
                if (!rect.Contains(pos)) continue;
                painter.fillColor = sys.Symbol == _selectedSymbol ? Color.cyan : GetStarColor(sys.Type);
                painter.BeginPath(); if (sys.Type == "NEBULA") DrawRect(painter, pos, 3); else painter.Arc(pos, sys.Symbol == _selectedSymbol ? 4 : 2, 0, 360);
                painter.Fill();
            }
        }

        private void DrawRect(Painter2D painter, Vector2 pos, float size)
        {
            painter.MoveTo(new Vector2(pos.x - size, pos.y - size)); painter.LineTo(new Vector2(pos.x + size, pos.y - size));
            painter.LineTo(new Vector2(pos.x + size, pos.y + size)); painter.LineTo(new Vector2(pos.x - size, pos.y + size));
            painter.ClosePath();
        }

        private Color GetStarColor(string type) => type switch { "RED_STAR" => new Color(1f, 0.3f, 0.3f), "BLUE_STAR" => new Color(0.3f, 0.6f, 1f), "YOUNG_STAR" => new Color(0.6f, 1f, 1f), "NEBULA" => new Color(1f, 0.5f, 1f), _ => Color.white };

        private void DrawSystemBulk(Painter2D painter, Rect rect)
        {
            if (_currentSystem == null) return;
            foreach (var wp in _currentSystem.waypoints)
            {
                if (string.IsNullOrEmpty(wp.orbits)) continue;
                var parent = _currentSystem.waypoints.FirstOrDefault(w => w.symbol == wp.orbits);
                if (parent == null) continue;
                
                Vector2 pPos = GetVisualWaypointPos(parent) * _mapZoom + _mapOffset;
                float radius = GetOrbitalRadius(wp) * _mapZoom;
                
                bool isSelectedChild = wp.symbol == _selectedSymbol;
                painter.strokeColor = isSelectedChild ? new Color(0, 1f, 1f, 0.6f) : new Color(0.3f, 0.3f, 0.3f, 0.15f);
                painter.lineWidth = isSelectedChild ? 2f : 1f;
                
                painter.BeginPath(); painter.Arc(pPos, radius, 0, 360); painter.Stroke();
            }
            foreach (var wp in _currentSystem.waypoints)
            {
                Vector2 pos = GetVisualWaypointPos(wp) * _mapZoom + _mapOffset;
                if (!rect.Contains(pos)) continue;
                bool isSelected = wp.symbol == _selectedSymbol;
                painter.fillColor = isSelected ? Color.cyan : GetWaypointColor(wp.type);
                painter.BeginPath();
                if (wp.type == "ORBITAL_STATION") DrawRect(painter, pos, 4);
                else if (wp.type == "JUMP_GATE") { painter.Arc(pos, 5, 0, 360); painter.strokeColor = painter.fillColor; painter.lineWidth = 1f; painter.Stroke(); }
                else painter.Arc(pos, isSelected ? 6 : 4, 0, 360);
                painter.Fill();
                if (isSelected) { painter.strokeColor = Color.white; painter.lineWidth = 2f; painter.BeginPath(); painter.Arc(pos, 8, 0, 360); painter.Stroke(); }
            }
        }

        private Color GetWaypointColor(string type) => type switch { "PLANET" => new Color(0, 0.6f, 1f), "MOON" => Color.gray, "ORBITAL_STATION" => Color.yellow, "JUMP_GATE" => new Color(0.8f, 0, 1f), "ASTEROID_FIELD" => new Color(0.4f, 0.3f, 0.2f), "GAS_GIANT" => new Color(1f, 0.6f, 0), _ => Color.white };

        private void DrawLines(Painter2D painter, Rect rect, float size, Color color)
        {
            painter.strokeColor = color; painter.lineWidth = 1f;
            float startX = _mapOffset.x % size; if (startX < 0) startX += size;
            float startY = _mapOffset.y % size; if (startY < 0) startY += size;
            for (float x = startX; x <= rect.width + 1; x += size) { painter.BeginPath(); painter.MoveTo(new Vector2(x, 0)); painter.LineTo(new Vector2(x, rect.height)); painter.Stroke(); }
            for (float y = startY; y <= rect.height + 1; y += size) { painter.BeginPath(); painter.MoveTo(new Vector2(0, y)); painter.LineTo(new Vector2(rect.width, y)); painter.Stroke(); }
        }

        private VisualElement BindShip(Ship s) {
            var element = shipTemplate.Instantiate();
            element.Q<Label>("symbol-label").text = $"Symbol: {s.symbol}";
            element.Q<Label>("details-label").text = $"Role: {s.registration.role} | System: {s.nav.systemSymbol} | Waypoint: {s.nav.waypointSymbol}";
            element.Q<Label>("status-label").text = $"Status: {s.nav.status} | Fuel: {s.fuel.current}/{s.fuel.capacity}";
            
            var mapBtn = element.Q<Button>("show-on-map-btn");
            if (mapBtn != null)
            {
                mapBtn.clicked += () => _ = OpenSystem(s.nav.systemSymbol, s.nav.waypointSymbol);
            }
            return element;
        }

        private VisualElement BindContract(Contract c) { var element = contractTemplate.Instantiate(); element.Q<Label>("id-label").text = $"ID: {c.id}"; element.Q<Label>("type-label").text = $"Type: {c.type} | Faction: {c.factionSymbol}"; element.Q<Label>("status-label").text = $"Accepted: {c.accepted} | Fulfilled: {c.fulfilled}"; return element; }
        private VisualElement BindFaction(Faction f) { var element = factionTemplate.Instantiate(); element.Q<Label>("name-label").text = $"Name: {f.name}"; element.Q<Label>("details-label").text = $"Symbol: {f.symbol} | HQ: {f.headquarters}"; element.Q<Label>("description-label").text = f.description; return element; }
        private void AddRow(VisualElement root, string key, string value) { var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5 } }; row.Add(new Label($"{key}: ") { style = { unityFontStyleAndWeight = FontStyle.Bold, width = 150, color = Color.gray } }); row.Add(new Label(value) { style = { color = Color.white, flexGrow = 1 } }); root.Add(row); }

        private string GetWaypointDescription(string type) => type switch {
            "PLANET" => "Large celestial body orbiting a star.", "MOON" => "Natural satellite orbiting a planet.",
            "ORBITAL_STATION" => "Man-made structure in orbit.", "JUMP_GATE" => "Fast-travel gateway to other systems.",
            "ASTEROID_FIELD" => "Region rich in minerals.", "GAS_GIANT" => "Massive planet composed of gases.",
            _ => "Unknown waypoint type."
        };

        private async Task FetchWaypointDetails(SystemWaypoint wp)
        {
            string systemSymbol = _currentSystem.symbol;
            string wpSymbol = wp.symbol;
            try
            {
                if (wp.type == "ORBITAL_STATION" || wp.type == "PLANET")
                {
                    try {
                        var res = await APIService.Instance.GetMarket(systemSymbol, wpSymbol);
                        DisplayMarket(res.data);
                    } catch {
                        try {
                            var res = await APIService.Instance.GetShipyard(systemSymbol, wpSymbol);
                            DisplayShipyard(res.data);
                        } catch {
                            try {
                                var res = await APIService.Instance.GetConstruction(systemSymbol, wpSymbol);
                                DisplayConstruction(res.data);
                            } catch {
                                _extraInfoTitle.text = "No specialized info";
                            }
                        }
                    }
                }
                else if (wp.type == "JUMP_GATE")
                {
                    var res = await APIService.Instance.GetJumpGate(systemSymbol, wpSymbol);
                    _extraInfoTitle.text = "Jump Gate Connections";
                    foreach(var conn in res.data.connections) _extraContentContainer.Add(new Label($"• {conn}") { style = { fontSize = 11 } });
                }
                else { _extraInfoTitle.text = "Basic Waypoint"; }
            }
            catch (Exception e) { _extraInfoTitle.text = "Error loading info"; Debug.LogError(e.Message); }
        }

        private void DisplayMarket(Market m)
        {
            _extraInfoTitle.text = "Marketplace";
            if (m.tradeGoods != null && m.tradeGoods.Length > 0)
            {
                foreach (var g in m.tradeGoods)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 2 } };
                    row.Add(new Label(g.symbol) { style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, width = 80 } });
                    row.Add(new Label($"{g.supply}") { style = { fontSize = 10, color = GetSupplyColor(g.supply), width = 60 } });
                    row.Add(new Label($"B: {g.purchasePrice} S: {g.sellPrice}") { style = { fontSize = 10, color = Color.gray } });
                    _extraContentContainer.Add(row);
                }
            }
            else
            {
                if (m.imports.Length > 0) _extraContentContainer.Add(new Label($"Imports: {string.Join(", ", m.imports.Select(i => i.symbol))}") { style = { fontSize = 10, whiteSpace = WhiteSpace.Normal } });
                if (m.exports.Length > 0) _extraContentContainer.Add(new Label($"Exports: {string.Join(", ", m.exports.Select(e => e.symbol))}") { style = { fontSize = 10, whiteSpace = WhiteSpace.Normal } });
            }
        }

        private Color GetSupplyColor(string supply) => supply switch {
            "ABUNDANT" => Color.green, "HIGH" => new Color(0.5f, 1f, 0.5f), "MODERATE" => Color.white,
            "LIMITED" => Color.yellow, "SCARCE" => new Color(1f, 0.5f, 0f), _ => Color.gray
        };

        private void DisplayShipyard(Shipyard s)
        {
            _extraInfoTitle.text = "Shipyard";
            if (s.ships != null && s.ships.Length > 0)
            {
                foreach (var ship in s.ships)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 2 } };
                    row.Add(new Label(ship.name) { style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } });
                    row.Add(new Label($"{ship.purchasePrice:N0} C") { style = { fontSize = 11, color = Color.yellow } });
                    _extraContentContainer.Add(row);
                }
            }
            else if (s.shipTypes != null)
            {
                _extraContentContainer.Add(new Label("Available Models:") { style = { fontSize = 10, color = Color.gray } });
                foreach(var type in s.shipTypes) _extraContentContainer.Add(new Label($"• {type}") { style = { fontSize = 11 } });
            }
        }

        private void DisplayConstruction(Construction c)
        {
            _extraInfoTitle.text = "Construction Progress";
            if (c.isComplete) { _extraContentContainer.Add(new Label("Construction Complete") { style = { color = Color.green } }); return; }
            foreach (var m in c.materials)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 2 } };
                row.Add(new Label(m.tradeSymbol) { style = { fontSize = 11, flexGrow = 1 } });
                row.Add(new Label($"{m.fulfilled}/{m.required}") { style = { fontSize = 11, color = m.fulfilled >= m.required ? Color.green : Color.white } });
                _extraContentContainer.Add(row);
            }
        }

        public void Pan(Vector2 delta) { _mapOffset += delta; RefreshMapUI(); }
        public void Zoom(float delta, Vector2 mousePos) {
            float oldZoom = _mapZoom; _mapZoom = Mathf.Clamp(_mapZoom * (1f + delta), 0.01f, 10000f);
            Vector2 worldPos = (mousePos - _mapOffset) / oldZoom; _mapOffset = mousePos - (worldPos * _mapZoom);
            RefreshMapUI();
        }

        private class MapManipulator : Manipulator {
            private DashboardController _controller; private bool _active; private Vector2 _lastMousePos;
            public MapManipulator(DashboardController controller) { _controller = controller; }
            protected override void RegisterCallbacksOnTarget() { target.RegisterCallback<PointerDownEvent>(OnPointerDown); target.RegisterCallback<PointerMoveEvent>(OnPointerMove); target.RegisterCallback<PointerUpEvent>(OnPointerUp); target.RegisterCallback<WheelEvent>(OnWheel); }
            protected override void UnregisterCallbacksFromTarget() { target.UnregisterCallback<PointerDownEvent>(OnPointerDown); target.UnregisterCallback<PointerMoveEvent>(OnPointerMove); target.UnregisterCallback<PointerUpEvent>(OnPointerUp); target.UnregisterCallback<WheelEvent>(OnWheel); }
            private void OnPointerDown(PointerDownEvent evt) { if (evt.button == 1 || evt.button == 2) { _active = true; _lastMousePos = evt.localPosition; target.CapturePointer(evt.pointerId); evt.StopPropagation(); } }
            private void OnPointerMove(PointerMoveEvent evt) { if (_active) { Vector2 delta = (Vector2)evt.localPosition - _lastMousePos; _controller.Pan(delta); _lastMousePos = evt.localPosition; evt.StopPropagation(); } }
            private void OnPointerUp(PointerUpEvent evt) { if (_active && (evt.button == 1 || evt.button == 2)) { _active = false; target.ReleasePointer(evt.pointerId); evt.StopPropagation(); } }
            private void OnWheel(WheelEvent evt) { float delta = -evt.delta.y * 0.1f; _controller.Zoom(delta, evt.localMousePosition); evt.StopPropagation(); }
        }
    }
}
