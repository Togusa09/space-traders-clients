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
        private Button _backButton;
        private Dictionary<Tab, Button> _tabButtons = new Dictionary<Tab, Button>();

        // Map Panel References
        private VisualElement _mapContainer;
        private Label _selectedSystemTitle, _selectedSystemSubtitle, _mapHeading;
        private Label _wpSymbol, _wpType, _wpCoords, _wpDesc, _extraInfoTitle;
        private VisualElement _extraContentContainer;
        private List<VisualElement> _listEntries = new List<VisualElement>();
        private List<VisualElement> _mapIcons = new List<VisualElement>();
        private Button _prevPageBtn, _nextPageBtn, _viewGalaxyBtn;
        private Label _pageInfoLabel;
        private TextField _systemSearch;
        
        // Map State
        private Vector2 _mapOffset = Vector2.zero;
        private float _mapZoom = 1.0f;
        private SystemData _currentSystem;
        private List<DatabaseManager.IndexedSystem> _allGalaxySystems = new List<DatabaseManager.IndexedSystem>();
        private List<DatabaseManager.IndexedSystem> _pagedGalaxySystems = new List<DatabaseManager.IndexedSystem>();
        private bool _mapInitialized = false;
        private bool _showRoutes = true;

        private int _currentSystemsPage = 1;
        private int _totalSystemsPages = 1;
        private const int SystemsPerPage = 20;

        // Async Safety
        private int _requestSequence = 0;

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            _dataContainer = root.Q<VisualElement>("data-container");
            _statusLabel = root.Q<Label>("status-label");
            _backButton = root.Q<Button>("back-button");

            RegisterTab(Tab.Agent, root.Q<Button>("tab-agent"));
            RegisterTab(Tab.Contracts, root.Q<Button>("tab-contracts"));
            RegisterTab(Tab.Fleet, root.Q<Button>("tab-fleet"));
            RegisterTab(Tab.Map, root.Q<Button>("tab-map"));
            RegisterTab(Tab.Factions, root.Q<Button>("tab-factions"));

            _backButton.clicked += () => SceneManager.LoadScene(mainMenuSceneName);

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
                _mapMode = MapMode.Galaxy;
                _currentSystemsPage = 1;
                // Pre-load all systems for map
                _allGalaxySystems = DatabaseManager.Instance.GetAllSystems();
                SetupMapPanel();
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
                        if (sequence == _requestSequence) DisplayList(Tab.Fleet, ships.data);
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

        private void SetupMapPanel()
        {
            var panel = systemPanelTemplate.Instantiate();
            panel.style.flexGrow = 1;
            _dataContainer.Add(panel);

            _mapHeading = panel.Q<Label>("map-heading");
            _mapContainer = panel.Q<VisualElement>("map-container");
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
            _systemSearch.RegisterValueChangedCallback(evt => RefreshMapList());

            _prevPageBtn.clicked += () => { _currentSystemsPage--; RefreshMapList(); };
            _nextPageBtn.clicked += () => { _currentSystemsPage++; RefreshMapList(); };

            _mapContainer.generateVisualContent += DrawMapContent;
            _mapContainer.AddManipulator(new MapManipulator(this));
            _mapContainer.RegisterCallback<GeometryChangedEvent>(OnMapGeometryChanged);

            RefreshMapList();
        }

        private void SwitchMapMode(MapMode mode)
        {
            _mapMode = mode;
            _mapInitialized = false;
            _mapHeading.text = mode == MapMode.Galaxy ? "Galaxy Map" : "System Map";
            _viewGalaxyBtn.style.display = mode == MapMode.Galaxy ? DisplayStyle.None : DisplayStyle.Flex;
            if (mode == MapMode.Galaxy) _currentSystem = null;
            RefreshMapList();
        }

        private void RefreshMapList()
        {
            var listContainer = _dataContainer.Q<ScrollView>("system-list");
            if (listContainer == null) return;
            listContainer.Clear();
            _listEntries.Clear();

            string search = _systemSearch.value?.ToUpper() ?? "";

            if (_mapMode == MapMode.Galaxy)
            {
                // Filter all systems for searching
                var filtered = string.IsNullOrEmpty(search) 
                    ? _allGalaxySystems 
                    : _allGalaxySystems.Where(s => s.Symbol.Contains(search)).ToList();

                _totalSystemsPages = (int)Math.Ceiling((double)filtered.Count / SystemsPerPage);
                if (_currentSystemsPage > _totalSystemsPages) _currentSystemsPage = Math.Max(1, _totalSystemsPages);

                _pagedGalaxySystems = filtered.Skip((_currentSystemsPage - 1) * SystemsPerPage).Take(SystemsPerPage).ToList();

                foreach (var sys in _pagedGalaxySystems)
                {
                    var entry = systemTemplate.Instantiate();
                    var root = entry.Q<VisualElement>(null, "dashboard-entry");
                    root.name = $"list-{sys.Symbol}";
                    root.AddToClassList("selectable-entry");
                    entry.Q<Label>("symbol-label").text = sys.Symbol;
                    entry.Q<Label>("details-label").text = $"{sys.Type} ({sys.WaypointCount} WP)";
                    root.RegisterCallback<ClickEvent>(evt => SelectGalaxySystem(sys, root));
                    listContainer.Add(entry);
                    _listEntries.Add(root);
                }

                _pageInfoLabel.text = $"{_currentSystemsPage}/{_totalSystemsPages}";
                _prevPageBtn.SetEnabled(_currentSystemsPage > 1);
                _nextPageBtn.SetEnabled(_currentSystemsPage < _totalSystemsPages);

                if (_pagedGalaxySystems.Count > 0) SelectGalaxySystem(_pagedGalaxySystems[0], _listEntries[0]);
                else ResetMap(); 
            }
            else
            {
                if (_currentSystem == null) return;
                var waypoints = _currentSystem.waypoints.Where(w => string.IsNullOrEmpty(search) || w.symbol.Contains(search)).ToList();
                _pageInfoLabel.text = "1/1";
                _prevPageBtn.SetEnabled(false);
                _nextPageBtn.SetEnabled(false);

                foreach (var wp in waypoints)
                {
                    var entry = systemTemplate.Instantiate();
                    var root = entry.Q<VisualElement>(null, "dashboard-entry");
                    root.name = $"list-{wp.symbol}";
                    root.AddToClassList("selectable-entry");
                    entry.Q<Label>("symbol-label").text = wp.symbol;
                    entry.Q<Label>("details-label").text = wp.type;
                    root.RegisterCallback<ClickEvent>(evt => {
                        var icon = _mapIcons.FirstOrDefault(i => i.name == wp.symbol);
                        if (icon != null) SelectWaypoint(wp, icon);
                    });
                    listContainer.Add(entry);
                    _listEntries.Add(root);
                }
                ResetMap();
            }
            RenderMap();
        }

        private void SelectGalaxySystem(DatabaseManager.IndexedSystem sys, VisualElement entryRoot)
        {
            foreach (var e in _listEntries) e.RemoveFromClassList("selected-entry");
            entryRoot?.AddToClassList("selected-entry");
            
            // Scroll to entry if it exists in the list
            if (entryRoot != null) {
                var scroll = _dataContainer.Q<ScrollView>("system-list");
                scroll.ScrollTo(entryRoot);
            }

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
            openBtn.style.width = 150;
            openBtn.style.height = 30;
            openBtn.style.marginTop = 0;
            openBtn.style.marginBottom = 0;
            _extraContentContainer.Add(openBtn);
            
            // Highlight icon on map
            foreach (var icon in _mapIcons) {
                if (icon.name == sys.Symbol) icon.AddToClassList("waypoint-selected");
                else icon.RemoveFromClassList("waypoint-selected");
            }
        }

        private async Task OpenSystem(string symbol)
        {
            _statusLabel.text = $"Opening {symbol}...";
            try
            {
                var res = await APIService.Instance.GetSystems(1, 20);
                var sys = res.data.FirstOrDefault(s => s.symbol == symbol);
                if (sys != null)
                {
                    _currentSystem = sys;
                    SwitchMapMode(MapMode.System);
                }
                _statusLabel.text = string.Empty;
            }
            catch (Exception e)
            {
                _statusLabel.text = $"Error: {e.Message}";
            }
        }

        private void OnMapGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_mapInitialized && (_mapMode == MapMode.Galaxy || _currentSystem != null))
            {
                ResetMap();
                _mapInitialized = true;
            }
        }

        private void ResetMap()
        {
            _mapOffset = Vector2.zero;
            _mapZoom = 1.0f;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            if (_mapMode == MapMode.Galaxy)
            {
                if (_allGalaxySystems.Count == 0) return;
                foreach (var s in _allGalaxySystems) {
                    minX = Math.Min(minX, s.X); maxX = Math.Max(maxX, s.X);
                    minY = Math.Min(minY, s.Y); maxY = Math.Max(maxY, s.Y);
                }
            }
            else
            {
                if (_currentSystem == null || _currentSystem.waypoints.Length == 0) return;
                foreach (var wp in _currentSystem.waypoints) {
                    minX = Math.Min(minX, wp.x); maxX = Math.Max(maxX, wp.x);
                    minY = Math.Min(minY, wp.y); maxY = Math.Max(maxY, wp.y);
                }
            }

            float rangeX = Math.Max(100, maxX - minX);
            float rangeY = Math.Max(100, maxY - minY);
            float width = _mapContainer.layout.width;
            float height = _mapContainer.layout.height;
            if (width <= 0 || height <= 0) return;
            float zoomX = (width * 0.8f) / rangeX;
            float zoomY = (height * 0.8f) / rangeY;
            _mapZoom = Math.Min(zoomX, zoomY);
            if (_mapZoom <= 0) _mapZoom = 1.0f;
            _mapOffset = new Vector2(-(minX + maxX) / 2f * _mapZoom, -(minY + maxY) / 2f * _mapZoom);
            _mapOffset += new Vector2(width / 2f, height / 2f);
            _mapContainer.MarkDirtyRepaint();
        }

        private void DrawMapContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            var rect = _mapContainer.contentRect;
            float logZoom = Mathf.Log10(50f / _mapZoom);
            float floorLog = Mathf.Floor(logZoom);
            float majorScale = Mathf.Pow(10, floorLog + 1);
            float minorScale = Mathf.Pow(10, floorLog);
            float majorSize = majorScale * _mapZoom;
            float minorSize = minorScale * _mapZoom;
            float t = (floorLog + 1) - logZoom;
            float minorAlpha = Mathf.Clamp01((t - 0.2f) / 0.8f);
            float majorAlpha = Mathf.Clamp01(1.2f - (majorSize / 1000f));
            if (minorAlpha > 0) DrawLines(painter, rect, minorSize, new Color(0.2f, 0.2f, 0.2f, minorAlpha * 0.2f));
            if (majorAlpha > 0) DrawLines(painter, rect, majorSize, new Color(0.5f, 0.5f, 0.5f, majorAlpha * 0.5f));

            if (_mapMode == MapMode.Galaxy && _showRoutes && _allGalaxySystems.Count > 1)
            {
                painter.strokeColor = new Color(0, 0.5f, 1.0f, 0.2f);
                painter.lineWidth = 1f;
                // Simplified route drawing for all systems
                for (int i = 0; i < _allGalaxySystems.Count; i += 2) // Step to reduce drawing overhead if large
                {
                    var s1 = _allGalaxySystems[i];
                    Vector2 p1 = new Vector2(s1.X, s1.Y) * _mapZoom + _mapOffset;
                    if (!rect.Contains(p1)) continue;

                    for (int j = i + 1; j < Math.Min(i + 5, _allGalaxySystems.Count); j++)
                    {
                        var s2 = _allGalaxySystems[j];
                        float dist = Math.Abs(s1.X - s2.X) + Math.Abs(s1.Y - s2.Y);
                        if (dist < 150) {
                            Vector2 p2 = new Vector2(s2.X, s2.Y) * _mapZoom + _mapOffset;
                            painter.BeginPath(); painter.MoveTo(p1); painter.LineTo(p2); painter.Stroke();
                        }
                    }
                }
            }
        }

        private void DrawLines(Painter2D painter, Rect rect, float size, Color color)
        {
            painter.strokeColor = color;
            painter.lineWidth = 1f;
            float startX = _mapOffset.x % size;
            if (startX < 0) startX += size;
            float startY = _mapOffset.y % size;
            if (startY < 0) startY += size;
            for (float x = startX; x <= rect.width + 1; x += size)
            {
                painter.BeginPath(); painter.MoveTo(new Vector2(x, 0)); painter.LineTo(new Vector2(x, rect.height)); painter.Stroke();
            }
            for (float y = startY; y <= rect.height + 1; y += size)
            {
                painter.BeginPath(); painter.MoveTo(new Vector2(0, y)); painter.LineTo(new Vector2(rect.width, y)); painter.Stroke();
            }
        }

        private void RenderMap()
        {
            _mapContainer.Clear();
            _mapIcons.Clear();
            
            // Only render VisualElements for systems in the CURRENT PAGE of the list to keep perf high
            // Background dots for everything else could be added to DrawMapContent if needed.
            
            if (_mapMode == MapMode.Galaxy)
            {
                // We show all systems as clickable points
                foreach (var sys in _allGalaxySystems)
                {
                    var iconRoot = waypointIconTemplate.Instantiate();
                    var icon = iconRoot.Q<VisualElement>("waypoint-root");
                    var label = iconRoot.Q<Label>("waypoint-name");
                    label.text = sys.Symbol;
                    icon.name = sys.Symbol;
                    
                    float posX = (sys.X * _mapZoom) + _mapOffset.x;
                    float posY = (sys.Y * _mapZoom) + _mapOffset.y;
                    icon.style.left = posX - 6; icon.style.top = posY - 6;

                    // Visibility optimization: hide label if too zoomed out
                    label.style.display = _mapZoom > 2.0f ? DisplayStyle.Flex : DisplayStyle.None;

                    icon.RegisterCallback<ClickEvent>(evt => {
                        // Find if it exists in current page
                        var entry = _listEntries.FirstOrDefault(e => e.name == $"list-{sys.Symbol}");
                        SelectGalaxySystem(sys, entry);
                        evt.StopImmediatePropagation();
                    });
                    _mapContainer.Add(icon);
                    _mapIcons.Add(icon);
                }
            }
            else
            {
                if (_currentSystem == null) return;
                foreach (var wp in _currentSystem.waypoints)
                {
                    var iconRoot = waypointIconTemplate.Instantiate();
                    var icon = iconRoot.Q<VisualElement>("waypoint-root");
                    var label = iconRoot.Q<Label>("waypoint-name");
                    label.text = wp.symbol;
                    icon.name = wp.symbol;
                    icon.AddToClassList($"wp-{wp.type.ToLower()}");

                    float posX = (wp.x * _mapZoom) + _mapOffset.x;
                    float posY = (wp.y * _mapZoom) + _mapOffset.y;
                    icon.style.left = posX - 6; icon.style.top = posY - 6;

                    icon.RegisterCallback<ClickEvent>(evt => {
                        SelectWaypoint(wp, icon);
                        evt.StopImmediatePropagation();
                    });
                    _mapContainer.Add(icon);
                    _mapIcons.Add(icon);
                }
            }
        }

        private void SelectWaypoint(SystemWaypoint wp, VisualElement icon)
        {
            foreach (var i in _mapIcons) i.RemoveFromClassList("waypoint-selected");
            icon.AddToClassList("waypoint-selected");
            _wpSymbol.text = wp.symbol;
            _wpType.text = wp.type;
            _wpCoords.text = $"({wp.x}, {wp.y})";
            _wpDesc.text = GetWaypointDescription(wp.type);
            _extraInfoTitle.text = "Loading...";
            _extraContentContainer.Clear();
            _ = FetchWaypointDetails(wp);
            
            // Scroll to list entry
            var entry = _listEntries.FirstOrDefault(e => e.name == $"list-{wp.symbol}");
            if (entry != null) {
                foreach (var e in _listEntries) e.RemoveFromClassList("selected-entry");
                entry.AddToClassList("selected-entry");
                var scroll = _dataContainer.Q<ScrollView>("system-list");
                scroll.ScrollTo(entry);
            }
        }

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

        private string GetWaypointDescription(string type) => type switch {
            "PLANET" => "Large celestial body orbiting a star.", "MOON" => "Natural satellite orbiting a planet.",
            "ORBITAL_STATION" => "Man-made structure in orbit.", "JUMP_GATE" => "Fast-travel gateway to other systems.",
            "ASTEROID_FIELD" => "Region rich in minerals.", "GAS_GIANT" => "Massive planet composed of gases.",
            _ => "Unknown waypoint type."
        };

        private VisualElement BindContract(Contract c) {
            var element = contractTemplate.Instantiate();
            element.Q<Label>("id-label").text = $"ID: {c.id}";
            element.Q<Label>("type-label").text = $"Type: {c.type} | Faction: {c.factionSymbol}";
            element.Q<Label>("status-label").text = $"Accepted: {c.accepted} | Fulfilled: {c.fulfilled}";
            return element;
        }

        private VisualElement BindShip(Ship s) {
            var element = shipTemplate.Instantiate();
            element.Q<Label>("symbol-label").text = $"Symbol: {s.symbol}";
            element.Q<Label>("details-label").text = $"Role: {s.registration.role} | System: {s.nav.systemSymbol}";
            element.Q<Label>("status-label").text = $"Status: {s.nav.status} | Fuel: {s.fuel.current}/{s.fuel.capacity}";
            return element;
        }

        private VisualElement BindFaction(Faction f) {
            var element = factionTemplate.Instantiate();
            element.Q<Label>("name-label").text = $"Name: {f.name}";
            element.Q<Label>("details-label").text = $"Symbol: {f.symbol} | HQ: {f.headquarters}";
            element.Q<Label>("description-label").text = f.description;
            return element;
        }

        private void AddRow(VisualElement root, string key, string value) {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5 } };
            row.Add(new Label($"{key}: ") { style = { unityFontStyleAndWeight = FontStyle.Bold, width = 150, color = Color.gray } });
            row.Add(new Label(value) { style = { color = Color.white, flexGrow = 1 } });
            root.Add(row);
        }

        public void Pan(Vector2 delta) { _mapOffset += delta; RefreshMapUI(); }
        public void Zoom(float delta, Vector2 mousePos) {
            float oldZoom = _mapZoom; _mapZoom = Mathf.Clamp(_mapZoom * (1f + delta), 0.01f, 10000f);
            Vector2 worldPos = (mousePos - _mapOffset) / oldZoom; _mapOffset = mousePos - (worldPos * _mapZoom);
            RefreshMapUI();
        }
        private void RefreshMapUI() { _mapContainer?.MarkDirtyRepaint(); RenderMap(); }

        private class MapManipulator : Manipulator {
            private DashboardController _controller; private bool _active; private Vector2 _lastMousePos;
            public MapManipulator(DashboardController controller) { _controller = controller; }
            protected override void RegisterCallbacksOnTarget() {
                target.RegisterCallback<PointerDownEvent>(OnPointerDown); target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                target.RegisterCallback<PointerUpEvent>(OnPointerUp); target.RegisterCallback<WheelEvent>(OnWheel);
            }
            protected override void UnregisterCallbacksFromTarget() {
                target.UnregisterCallback<PointerDownEvent>(OnPointerDown); target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                target.UnregisterCallback<PointerUpEvent>(OnPointerUp); target.UnregisterCallback<WheelEvent>(OnWheel);
            }
            private void OnPointerDown(PointerDownEvent evt) {
                if (evt.button == 1 || evt.button == 2) { _active = true; _lastMousePos = evt.localPosition; target.CapturePointer(evt.pointerId); evt.StopPropagation(); }
            }
            private void OnPointerMove(PointerMoveEvent evt) {
                if (_active) { Vector2 delta = (Vector2)evt.localPosition - _lastMousePos; _controller.Pan(delta); _lastMousePos = evt.localPosition; evt.StopPropagation(); }
            }
            private void OnPointerUp(PointerUpEvent evt) {
                if (_active && (evt.button == 1 || evt.button == 2)) { _active = false; target.ReleasePointer(evt.pointerId); evt.StopPropagation(); }
            }
            private void OnWheel(WheelEvent evt) { float delta = -evt.delta.y * 0.1f; _controller.Zoom(delta, evt.localMousePosition); evt.StopPropagation(); }
        }
    }
}
