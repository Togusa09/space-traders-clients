using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using SpaceTraders.Core;

namespace SpaceTraders.UI
{
    public class MapPresenter
    {
        private readonly DashboardController _controller;

        // UI references
        private VisualElement _mapContainer;
        private VisualElement _labelContainer;
        private VisualElement _legendItems;
        private VisualElement _legendContent;
        private Button _legendToggle;
        private Label _selectedSystemTitle;
        private Label _selectedSystemSubtitle;
        private Label _mapHeading;
        private Label _wpSymbol;
        private Label _wpType;
        private Label _wpCoords;
        private Label _wpDesc;
        private Label _extraInfoTitle;
        private VisualElement _extraContentContainer;
        private Button _prevPageBtn;
        private Button _nextPageBtn;
        private Button _viewGalaxyBtn;
        private Label _pageInfoLabel;
        private TextField _systemSearch;

        // Templates
        private VisualTreeAsset _systemTemplate;
        private VisualTreeAsset _systemPanelTemplate;

        // Map State
        public Vector2 MapOffset { get; set; } = Vector2.zero;
        public float MapZoom { get; set; } = 1.0f;
        public DashboardController.MapMode MapModeState { get; private set; } = DashboardController.MapMode.Galaxy;
        public SystemData CurrentSystem { get; private set; }
        public string SelectedSymbol { get; private set; }

        private List<DatabaseManager.IndexedSystem> _allGalaxySystems = new List<DatabaseManager.IndexedSystem>();
        private List<DatabaseManager.IndexedSystem> _filteredGalaxySystems = new List<DatabaseManager.IndexedSystem>();
        private List<DatabaseManager.IndexedSystem> _pagedGalaxySystems = new List<DatabaseManager.IndexedSystem>();
        private List<VisualElement> _listEntries = new List<VisualElement>();
        
        private int _currentSystemsPage = 1;
        private int _totalSystemsPages = 1;
        private const int SystemsPerPage = 20;

        private bool _mapInitialized = false;
        private bool _showRoutes = true;
        private bool _legendExpanded = true;

        public MapPresenter(DashboardController controller)
        {
            _controller = controller;
        }

        public async Task SetupMapPanelAsync(
            VisualElement dataContainer, 
            VisualTreeAsset systemPanelTemplate, 
            VisualTreeAsset systemTemplate)
        {
            _systemTemplate = systemTemplate;
            _systemPanelTemplate = systemPanelTemplate;
            _mapInitialized = false;

            var panel = _systemPanelTemplate.Instantiate();
            panel.style.flexGrow = 1;
            dataContainer.Add(panel);

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

            _viewGalaxyBtn.clicked += () => SwitchMapMode(DashboardController.MapMode.Galaxy);
            _systemSearch.RegisterValueChangedCallback(evt => { _currentSystemsPage = 1; RefreshMapList(true, dataContainer); });

            _prevPageBtn.clicked += () => { _currentSystemsPage--; RefreshMapList(false, dataContainer); };
            _nextPageBtn.clicked += () => { _currentSystemsPage++; RefreshMapList(false, dataContainer); };

            _mapContainer.generateVisualContent += OnGenerateVisualContent;
            _mapContainer.AddManipulator(new MapManipulator(this));
            _mapContainer.RegisterCallback<GeometryChangedEvent>(OnMapGeometryChanged);
            _mapContainer.RegisterCallback<PointerDownEvent>(OnMapClick);

            _allGalaxySystems = DatabaseManager.Instance.GetAllSystems();

            UpdateLegend();
            
            if (MapModeState == DashboardController.MapMode.System && CurrentSystem != null)
            {
                _mapHeading.text = "System Map";
                _viewGalaxyBtn.style.display = DisplayStyle.Flex;
                RefreshMapList(true, dataContainer);
            }
            else
            {
                MapModeState = DashboardController.MapMode.Galaxy;
                RefreshMapList(true, dataContainer);
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
            if (MapModeState == DashboardController.MapMode.Galaxy)
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

        public void SwitchMapMode(DashboardController.MapMode mode)
        {
            MapModeState = mode;
            _mapInitialized = false;
            _mapHeading.text = mode == DashboardController.MapMode.Galaxy ? "Galaxy Map" : "System Map";
            _viewGalaxyBtn.style.display = mode == DashboardController.MapMode.Galaxy ? DisplayStyle.None : DisplayStyle.Flex;
            if (mode == DashboardController.MapMode.Galaxy) CurrentSystem = null;
            SelectedSymbol = null;
            UpdateLegend();
            RefreshMapList(true, _mapContainer.parent.parent);
        }

        private void RefreshMapList(bool resetView, VisualElement dataContainer)
        {
            var listContainer = dataContainer.Q<ScrollView>("system-list");
            if (listContainer == null) return;
            listContainer.Clear();
            _listEntries.Clear();

            string search = _systemSearch.value?.ToUpper() ?? "";

            if (MapModeState == DashboardController.MapMode.Galaxy)
            {
                _filteredGalaxySystems = string.IsNullOrEmpty(search) 
                    ? _allGalaxySystems 
                    : _allGalaxySystems.Where(s => s.Symbol.Contains(search)).ToList();

                _totalSystemsPages = (int)Math.Ceiling((double)_filteredGalaxySystems.Count / SystemsPerPage);
                if (_currentSystemsPage > _totalSystemsPages) _currentSystemsPage = Math.Max(1, _totalSystemsPages);

                _pagedGalaxySystems = _filteredGalaxySystems.Skip((_currentSystemsPage - 1) * SystemsPerPage).Take(SystemsPerPage).ToList();

                foreach (var sys in _pagedGalaxySystems)
                {
                    var entry = _systemTemplate.Instantiate();
                    var root = entry.Q<VisualElement>(null, "dashboard-entry");
                    root.name = $"list-{sys.Symbol}";
                    root.AddToClassList("selectable-entry");
                    entry.Q<Label>("symbol-label").text = sys.Symbol;
                    entry.Q<Label>("details-label").text = $"{sys.Type} ({sys.WaypointCount} WP)";
                    if (sys.Symbol == SelectedSymbol) root.AddToClassList("selected-entry");
                    root.RegisterCallback<ClickEvent>(evt => SelectGalaxySystem(sys, root, false, dataContainer));
                    listContainer.Add(entry);
                    _listEntries.Add(root);
                }

                _pageInfoLabel.text = $"{_currentSystemsPage}/{_totalSystemsPages}";
                _prevPageBtn.SetEnabled(_currentSystemsPage > 1);
                _nextPageBtn.SetEnabled(_currentSystemsPage < _totalSystemsPages);

                if (SelectedSymbol == null && _pagedGalaxySystems.Count > 0) 
                    SelectGalaxySystem(_pagedGalaxySystems[0], _listEntries[0], resetView, dataContainer);
                else if (resetView)
                    ResetMap(); 
            }
            else
            {
                if (CurrentSystem == null) return;
                var waypoints = CurrentSystem.waypoints.ToList();
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
                        AddSystemListEntry(listContainer, wp, 0, search, dataContainer);
                        var toShow = matchesSelf ? children : matchingChildren;
                        foreach(var child in toShow) AddSystemListEntry(listContainer, child, 1, search, dataContainer);
                    }
                }
                if (resetView) ResetMap();
            }
            RefreshMapUI();
        }

        private void AddSystemListEntry(VisualElement container, SystemWaypoint wp, int indent, string search, VisualElement dataContainer)
        {
            var entry = _systemTemplate.Instantiate();
            var root = entry.Q<VisualElement>(null, "dashboard-entry");
            root.name = $"list-{wp.symbol}";
            root.AddToClassList("selectable-entry");
            root.style.marginLeft = indent * 15;
            entry.Q<Label>("symbol-label").text = (indent > 0 ? "↳ " : "") + wp.symbol;
            entry.Q<Label>("details-label").text = wp.type;

            if (wp.traits != null && wp.traits.Length > 0)
            {
                var badgesRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 3 } };
                bool hasBadges = false;
                foreach (var trait in wp.traits)
                {
                    if (trait.symbol == "MARKETPLACE")
                    {
                        var badge = CreateBadge("MARKET", new Color(0.12f, 0.58f, 0.39f), Color.white);
                        badgesRow.Add(badge);
                        hasBadges = true;
                    }
                    else if (trait.symbol == "SHIPYARD")
                    {
                        var badge = CreateBadge("SHIPYARD", new Color(0.1f, 0.45f, 0.82f), Color.white);
                        badgesRow.Add(badge);
                        hasBadges = true;
                    }
                }
                if (hasBadges)
                {
                    root.Add(badgesRow);
                }
            }

            if (wp.symbol == SelectedSymbol) root.AddToClassList("selected-entry");
            root.RegisterCallback<ClickEvent>(evt => SelectWaypoint(wp, root, false));
            container.Add(entry);
            _listEntries.Add(root);
        }

        private VisualElement CreateBadge(string text, Color backgroundColor, Color textColor)
        {
            var badge = new Label(text)
            {
                style =
                {
                    backgroundColor = backgroundColor,
                    color = textColor,
                    fontSize = 8,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 1,
                    paddingBottom = 1,
                    marginRight = 4,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };
            return badge;
        }

        private void SelectGalaxySystem(DatabaseManager.IndexedSystem sys, VisualElement entryRoot, bool focus, VisualElement dataContainer)
        {
            SelectedSymbol = sys.Symbol;
            foreach (var e in _listEntries) e.RemoveFromClassList("selected-entry");
            entryRoot?.AddToClassList("selected-entry");
            if (entryRoot != null) dataContainer.Q<ScrollView>("system-list").ScrollTo(entryRoot);

            _selectedSystemTitle.text = sys.Symbol;
            _selectedSystemSubtitle.text = sys.Type;
            _wpSymbol.text = sys.Symbol;
            _wpType = _wpType ?? dataContainer.Q<Label>("wp-type");
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

        public void SelectWaypoint(SystemWaypoint wp, VisualElement entryRoot, bool focus)
        {
            SelectedSymbol = wp.symbol;
            foreach (var e in _listEntries) e.RemoveFromClassList("selected-entry");
            entryRoot?.AddToClassList("selected-entry");
            if (entryRoot != null) _mapContainer.parent.parent.Q<ScrollView>("system-list").ScrollTo(entryRoot);

            _wpSymbol.text = wp.symbol;
            _wpType.text = wp.type;
            _wpCoords.text = $"({wp.x}, {wp.y})";
            _wpDesc.text = GetWaypointDescription(wp.type);
            _extraInfoTitle.text = "Checking access...";
            _extraContentContainer.Clear();
            
            bool hasAccess = _controller.PlayerShips.Any(s => s.nav.systemSymbol == (CurrentSystem?.symbol ?? SelectedSymbol.Split('-')[0]));
            if (hasAccess) _ = FetchWaypointDetails(wp);
            else _extraInfoTitle.text = "System Inaccessible (No Ships)";

            if (focus) CenterOnPoint(GetVisualWaypointPos(wp));
            RefreshMapUI();
        }

        private void CenterOnPoint(Vector2 point)
        {
            Vector2 screenPos = point * MapZoom + MapOffset;
            var rect = _mapContainer.contentRect;
            if (!rect.Contains(screenPos)) MapOffset = new Vector2(rect.width / 2f, rect.height / 2f) - (point * MapZoom);
        }

        public async Task OpenSystem(string symbol, string focusWaypoint = null)
        {
            _controller.SetStatusText($"Opening {symbol}...");
            try
            {
                var res = await APIService.Instance.GetSystem(symbol);
                if (res != null && res.data != null)
                {
                    CurrentSystem = res.data;

                    if (CurrentSystem.waypoints != null && CurrentSystem.waypoints.Length > 0 && !string.IsNullOrEmpty(CurrentSystem.waypoints[0].traits == null ? "" : "hasTraits"))
                    {
                        // Loaded successfully
                    }
                    else
                    {
                        try
                        {
                            var wpsRes = await APIService.Instance.GetSystemWaypoints(symbol);
                            if (wpsRes != null) CurrentSystem.waypoints = wpsRes.data;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[MapPresenter] Failed to fetch detailed waypoints: {ex.Message}");
                        }
                    }

                    _controller.TriggerTabSwitch(DashboardController.Tab.Map);
                    SwitchMapMode(DashboardController.MapMode.System);

                    if (!string.IsNullOrEmpty(focusWaypoint))
                    {
                        var targetWp = CurrentSystem.waypoints?.FirstOrDefault(w => w.symbol == focusWaypoint);
                        if (targetWp != null)
                        {
                            var listContainer = _mapContainer.parent.parent.Q<ScrollView>("system-list");
                            var item = listContainer?.Q<VisualElement>($"list-{focusWaypoint}");
                            SelectWaypoint(targetWp, item, true);
                            return;
                        }
                    }

                    if (CurrentSystem.waypoints != null && CurrentSystem.waypoints.Length > 0)
                    {
                        var targetWp = CurrentSystem.waypoints[0];
                        var listContainer = _mapContainer.parent.parent.Q<ScrollView>("system-list");
                        var item = listContainer?.Q<VisualElement>($"list-{targetWp.symbol}");
                        SelectWaypoint(targetWp, item, true);
                    }
                    else
                    {
                        ResetMap();
                    }
                }
            }
            catch (Exception ex)
            {
                _controller.ShowPopupMessage("Error", $"Failed to load system details:\n{ex.Message}", Color.red);
            }
            finally
            {
                _controller.SetStatusText(string.Empty);
            }
        }

        private void ResetMap()
        {
            var rect = _mapContainer.contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;

            if (MapModeState == DashboardController.MapMode.Galaxy)
            {
                if (_filteredGalaxySystems.Count == 0)
                {
                    MapOffset = new Vector2(rect.width / 2f, rect.height / 2f);
                    MapZoom = 1.0f;
                    return;
                }
                int minX = _filteredGalaxySystems.Min(s => s.X), maxX = _filteredGalaxySystems.Max(s => s.X);
                int minY = _filteredGalaxySystems.Min(s => s.Y), maxY = _filteredGalaxySystems.Max(s => s.Y);
                FitBounds(minX, maxX, minY, maxY, rect.width, rect.height);
            }
            else
            {
                if (CurrentSystem?.waypoints == null || CurrentSystem.waypoints.Length == 0)
                {
                    MapOffset = new Vector2(rect.width / 2f, rect.height / 2f);
                    MapZoom = 1.0f;
                    return;
                }
                var pts = CurrentSystem.waypoints.Select(GetVisualWaypointPos).ToList();
                float minX = pts.Min(p => p.x), maxX = pts.Max(p => p.x);
                float minY = pts.Min(p => p.y), maxY = pts.Max(p => p.y);
                FitBounds(minX, maxX, minY, maxY, rect.width, rect.height);
            }
            _mapInitialized = true;
        }

        private void FitBounds(float minX, float maxX, float minY, float maxY, float width, float height)
        {
            if (width <= 0 || height <= 0) return;
            float zoomX = (width * 0.8f) / Math.Max(100, maxX - minX);
            float zoomY = (height * 0.8f) / Math.Max(100, maxY - minY);
            MapZoom = Math.Min(zoomX, zoomY);
            if (MapZoom <= 0) MapZoom = 1.0f;
            MapOffset = new Vector2(width / 2f, height / 2f) - (new Vector2(minX + maxX, minY + maxY) / 2f * MapZoom);
        }

        private void RefreshMapUI()
        {
            _mapContainer?.MarkDirtyRepaint();
            UpdateMapLabels();
        }

        private void UpdateMapLabels()
        {
            if (_labelContainer == null) return;
            _labelContainer.Clear();
            var rect = _mapContainer.contentRect;
            if (MapModeState == DashboardController.MapMode.Galaxy)
            {
                bool showAllLabels = MapZoom > 3.5f;
                foreach (var sys in _filteredGalaxySystems)
                {
                    Vector2 pos = new Vector2(sys.X, sys.Y) * MapZoom + MapOffset;
                    if (!rect.Contains(pos)) continue;
                    if (sys.Symbol == SelectedSymbol || showAllLabels) _labelContainer.Add(GetLabelFromPool(sys.Symbol, pos, sys.Symbol == SelectedSymbol ? Color.cyan : Color.white));
                }
            }
            else
            {
                if (CurrentSystem == null) return;
                foreach (var wp in CurrentSystem.waypoints)
                {
                    Vector2 pos = GetVisualWaypointPos(wp) * MapZoom + MapOffset;
                    if (!rect.Contains(pos)) continue;
                    _labelContainer.Add(GetLabelFromPool(wp.symbol, pos, wp.symbol == SelectedSymbol ? Color.cyan : Color.white));
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
            float logZoom = Mathf.Log10(50f / MapZoom), floorLog = Mathf.Floor(logZoom);
            float majorScale = Mathf.Pow(10, floorLog + 1), minorScale = Mathf.Pow(10, floorLog);
            float majorSize = majorScale * MapZoom, minorSize = minorScale * MapZoom;
            float t = (floorLog + 1) - logZoom, minorAlpha = Mathf.Clamp01((t - 0.2f) / 0.8f), majorAlpha = Mathf.Clamp01(1.2f - (majorSize / 1000f));
            if (minorAlpha > 0) DrawLines(painter, rect, minorSize, new Color(0.2f, 0.2f, 0.2f, minorAlpha * 0.2f));
            if (majorAlpha > 0) DrawLines(painter, rect, majorSize, new Color(0.5f, 0.5f, 0.5f, majorAlpha * 0.5f));
            if (MapModeState == DashboardController.MapMode.Galaxy) DrawGalaxyBulk(painter, rect); else DrawSystemBulk(painter, rect);
        }

        private void DrawGalaxyBulk(Painter2D painter, Rect rect)
        {
            if (_showRoutes && _filteredGalaxySystems.Count > 1)
            {
                painter.strokeColor = new Color(0, 0.5f, 1.0f, 0.15f); painter.lineWidth = 1f;
                var systems = _filteredGalaxySystems.Count < 500 ? _filteredGalaxySystems : _pagedGalaxySystems;
                for (int i = 0; i < systems.Count; i++)
                {
                    var s1 = systems[i]; Vector2 p1 = new Vector2(s1.X, s1.Y) * MapZoom + MapOffset;
                    if (!rect.Contains(p1)) continue;
                    for (int j = i + 1; j < Math.Min(i + 10, systems.Count); j++)
                    {
                        var s2 = systems[j]; float dist = Math.Abs(s1.X - s2.X) + Math.Abs(s1.Y - s2.Y);
                        if (dist < 150) { Vector2 p2 = new Vector2(s2.X, s2.Y) * MapZoom + MapOffset; painter.BeginPath(); painter.MoveTo(p1); painter.LineTo(p2); painter.Stroke(); }
                    }
                }
            }
            foreach (var sys in _filteredGalaxySystems)
            {
                Vector2 pos = new Vector2(sys.X, sys.Y) * MapZoom + MapOffset;
                if (!rect.Contains(pos)) continue;
                painter.fillColor = sys.Symbol == SelectedSymbol ? Color.cyan : GetStarColor(sys.Type);
                painter.BeginPath(); if (sys.Type == "NEBULA") DrawRect(painter, pos, 3); else painter.Arc(pos, sys.Symbol == SelectedSymbol ? 4 : 2, 0, 360);
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
            if (CurrentSystem == null) return;
            foreach (var wp in CurrentSystem.waypoints)
            {
                if (string.IsNullOrEmpty(wp.orbits)) continue;
                var parent = CurrentSystem.waypoints.FirstOrDefault(w => w.symbol == wp.orbits);
                if (parent == null) continue;
                
                Vector2 pPos = GetVisualWaypointPos(parent) * MapZoom + MapOffset;
                float radius = GetOrbitalRadius(wp) * MapZoom;
                
                bool isSelectedChild = wp.symbol == SelectedSymbol;
                
                painter.strokeColor = isSelectedChild ? new Color(0, 1f, 1f, 0.4f) : new Color(1f, 1f, 1f, 0.05f);
                painter.lineWidth = isSelectedChild ? 1.5f : 1f;
                painter.BeginPath(); painter.Arc(pPos, radius, 0, 360); painter.Stroke();
            }
            
            foreach (var wp in CurrentSystem.waypoints)
            {
                Vector2 pos = GetVisualWaypointPos(wp) * MapZoom + MapOffset;
                if (!rect.Contains(pos)) continue;
                
                bool isSelected = wp.symbol == SelectedSymbol;
                painter.fillColor = isSelected ? Color.cyan : GetWaypointColor(wp.type);
                painter.BeginPath();
                if (wp.type == "ORBITAL_STATION") DrawRect(painter, pos, isSelected ? 4 : 3);
                else if (wp.type == "JUMP_GATE") {
                    float size = isSelected ? 4 : 3;
                    painter.MoveTo(new Vector2(pos.x, pos.y - size)); painter.LineTo(new Vector2(pos.x + size, pos.y));
                    painter.LineTo(new Vector2(pos.x, pos.y + size)); painter.LineTo(new Vector2(pos.x - size, pos.y));
                    painter.ClosePath();
                }
                else painter.Arc(pos, isSelected ? 4 : GetWaypointSize(wp.type), 0, 360);
                painter.Fill();
            }
        }

        private float GetWaypointSize(string type) => type switch { "PLANET" => 3.5f, "MOON" => 1.5f, "ASTEROID_FIELD" => 2f, _ => 2.5f };
        private Color GetWaypointColor(string type) => type switch { "PLANET" => new Color(0, 0.6f, 1f), "MOON" => Color.gray, "ORBITAL_STATION" => Color.yellow, "JUMP_GATE" => new Color(0.8f, 0, 1f), "ASTEROID_FIELD" => new Color(0.4f, 0.3f, 0.2f), _ => Color.white };

        private void DrawLines(Painter2D painter, Rect rect, float size, Color color)
        {
            painter.strokeColor = color; painter.lineWidth = 1f; painter.BeginPath();
            float startX = MapOffset.x % size; if (startX < 0) startX += size;
            for (float x = startX; x < rect.width; x += size) { painter.MoveTo(new Vector2(x, 0)); painter.LineTo(new Vector2(x, rect.height)); }
            float startY = MapOffset.y % size; if (startY < 0) startY += size;
            for (float y = startY; y < rect.height; y += size) { painter.MoveTo(new Vector2(0, y)); painter.LineTo(new Vector2(rect.width, y)); }
            painter.Stroke();
        }

        private float GetOrbitalRadius(SystemWaypoint wp)
        {
            if (string.IsNullOrEmpty(wp.orbits)) return 0;
            var parent = CurrentSystem.waypoints.FirstOrDefault(w => w.symbol == wp.orbits);
            if (parent == null || parent.orbitals == null) return 20f;
            int index = Array.FindIndex(parent.orbitals, o => o.symbol == wp.symbol);
            return 25f + (Mathf.Max(0, index) * 15f);
        }

        private Vector2 GetVisualWaypointPos(SystemWaypoint wp)
        {
            if (CurrentSystem == null || string.IsNullOrEmpty(wp.orbits)) return new Vector2(wp.x, wp.y);
            var parent = CurrentSystem.waypoints.FirstOrDefault(w => w.symbol == wp.orbits);
            if (parent == null) return new Vector2(wp.x, wp.y);
            
            Vector2 parentPos = GetVisualWaypointPos(parent);
            float radius = GetOrbitalRadius(wp);
            int index = parent.orbitals != null ? Array.FindIndex(parent.orbitals, o => o.symbol == wp.symbol) : -1;
            float angle = (Mathf.Max(0, index) * 45f + 30f) * Mathf.Deg2Rad;
            return parentPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private string GetWaypointDescription(string type) => type switch {
            "PLANET" => "Large celestial body orbiting a star.", "MOON" => "Natural satellite orbiting a planet.",
            "ORBITAL_STATION" => "Man-made structure in orbit.", "JUMP_GATE" => "Fast-travel gateway to other systems.",
            "ASTEROID_FIELD" => "Region rich in minerals.", "GAS_GIANT" => "Massive planet composed of gases.",
            _ => "Unknown waypoint type."
        };

        private async Task FetchWaypointDetails(SystemWaypoint wp)
        {
            string systemSymbol = CurrentSystem.symbol;
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
                
                RenderWaypointActions(wp);
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
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 4 } };
                    
                    var nameLabel = new Label(ship.name) { style = { fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } };
                    row.Add(nameLabel);
                    
                    var priceLabel = new Label($"{ship.purchasePrice:N0} C") { style = { fontSize = 10, color = Color.yellow, marginRight = 10 } };
                    row.Add(priceLabel);

                    var buyBtn = new Button(async () => {
                        _controller.SetStatusText($"Purchasing {ship.type}...");
                        try
                        {
                            var res = await APIService.Instance.PurchaseShip(ship.type, s.symbol);
                            _controller.ShowPopupMessage("Ship Purchased", $"Successfully purchased {ship.name}!\nShip Symbol: {res.data.ship.symbol}\nCredits Remaining: {res.data.agent.credits:N0} C", Color.green);
                            
                            var newShips = await APIService.Instance.GetShips();
                            _controller.UpdatePlayerShipsExternal(newShips.data.ToList());
                            _ = FetchWaypointDetails(CurrentSystem.waypoints.FirstOrDefault(w => w.symbol == s.symbol));
                        }
                        catch (Exception ex)
                        {
                            _controller.ShowPopupMessage("Purchase Failed", $"Failed to purchase ship:\n{ex.Message}", Color.red);
                        }
                    }) { text = "BUY" };
                    buyBtn.AddToClassList("button");
                    buyBtn.AddToClassList("btn-small");
                    buyBtn.AddToClassList("btn-green");
                    buyBtn.style.width = 50; buyBtn.style.height = 20; buyBtn.style.fontSize = 8;
                    row.Add(buyBtn);

                    _extraContentContainer.Add(row);
                }
            }
            else if (s.shipTypes != null)
            {
                _extraContentContainer.Add(new Label("Available Models (Bring ship here to see details):") { style = { fontSize = 10, color = Color.gray } });
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

        private void RenderWaypointActions(SystemWaypoint wp)
        {
            if (_controller.PlayerShips == null || _controller.PlayerShips.Count == 0) return;

            var divider = new VisualElement { style = { height = 1, backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f), marginTop = 10, marginBottom = 5 } };
            _extraContentContainer.Add(divider);
            _extraContentContainer.Add(new Label("SHIP IN-SYSTEM ACTIONS") { style = { fontSize = 10, color = Color.gray, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });

            string systemSymbol = CurrentSystem?.symbol ?? wp.symbol.Split('-')[0];
            var systemShips = _controller.PlayerShips.Where(s => s.nav.systemSymbol == systemSymbol).ToList();

            if (systemShips.Count == 0)
            {
                _extraContentContainer.Add(new Label("No ships in this system.") { style = { fontSize = 9, color = Color.gray } });
                return;
            }

            foreach (var ship in systemShips)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 3 } };
                row.Add(new Label($"{ship.symbol} ({ship.nav.status})") { style = { fontSize = 9 } });

                bool isInTransit = ship.nav.status == "IN_TRANSIT" || _controller.ActiveTimers.Any(t => t.ShipSymbol == ship.symbol && !t.IsCooldown);
                bool isCooldownActive = _controller.ActiveTimers.Any(t => t.ShipSymbol == ship.symbol && t.IsCooldown);

                if (ship.nav.waypointSymbol != wp.symbol)
                {
                    var navBtn = new Button(async () => {
                        if (ship.nav.status != "IN_ORBIT")
                        {
                            _controller.ShowChoicePopupMessage("Ship is Docked",
                                $"Ship {ship.symbol} must be in orbit before navigating. Orbit now and proceed to {wp.symbol}?",
                                "ORBIT & NAVIGATE",
                                async () => {
                                    _controller.SetStatusText($"Orbiting and navigating {ship.symbol} to {wp.symbol}...");
                                    try
                                    {
                                        await APIService.Instance.OrbitShip(ship.symbol);
                                        var res = await APIService.Instance.NavigateShip(ship.symbol, wp.symbol);
                                        _controller.HandleNavigationResponseExternal(ship.symbol, res, wp.symbol);
                                        
                                        var newShips = await APIService.Instance.GetShips();
                                        _controller.UpdatePlayerShipsExternal(newShips.data.ToList());
                                        SelectWaypoint(wp, null, false);
                                    }
                                    catch (Exception ex)
                                    {
                                        _controller.ShowPopupMessage("Navigation Failed", $"Failed to orbit and navigate:\n{ex.Message}", Color.red);
                                    }
                                }
                            );
                            return;
                        }

                        _controller.SetStatusText($"Navigating {ship.symbol} here...");
                        try
                        {
                            var res = await APIService.Instance.NavigateShip(ship.symbol, wp.symbol);
                            _controller.HandleNavigationResponseExternal(ship.symbol, res, wp.symbol);

                            var newShips = await APIService.Instance.GetShips();
                            _controller.UpdatePlayerShipsExternal(newShips.data.ToList());
                            SelectWaypoint(wp, null, false);
                        }
                        catch (Exception ex)
                        {
                            _controller.ShowPopupMessage("Navigation Failed", $"Failed to navigate:\n{ex.Message}", Color.red);
                        }
                    }) { text = isInTransit ? "IN TRANSIT" : "NAV HERE" };
                    navBtn.AddToClassList("button");
                    navBtn.AddToClassList("btn-small");
                    navBtn.AddToClassList("btn-blue");
                    navBtn.style.width = 75; navBtn.style.height = 18; navBtn.style.fontSize = 8;
                    navBtn.SetEnabled(!isInTransit);
                    row.Add(navBtn);
                }
                else
                {
                    var localRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    
                    if (wp.type == "ASTEROID_FIELD" && ship.nav.status == "IN_ORBIT")
                    {
                        var extBtn = new Button(async () => {
                            _controller.SetStatusText("Extracting resources...");
                            try
                            {
                                var res = await APIService.Instance.ExtractResources(ship.symbol);
                                _controller.ShowPopupMessage("Extraction Complete", $"Yield: {res.data.extraction.yield.units} units of {res.data.extraction.yield.symbol}!", Color.green);
                                
                                string expStr = res.data?.cooldown?.expiration;
                                DateTime expiration = !string.IsNullOrEmpty(expStr)
                                    ? DateTime.Parse(expStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal)
                                    : DateTime.UtcNow.AddSeconds(res.data?.cooldown?.totalSeconds ?? 0);

                                _controller.ActiveTimers.RemoveAll(t => t.ShipSymbol == ship.symbol && t.IsCooldown);
                                _controller.ActiveTimers.Add(new DashboardController.ActiveTimer {
                                    ShipSymbol = ship.symbol,
                                    Expiration = expiration,
                                    TotalDuration = res.data.cooldown.totalSeconds,
                                    IsCooldown = true
                                });

                                var newShips = await APIService.Instance.GetShips();
                                _controller.UpdatePlayerShipsExternal(newShips.data.ToList());
                                SelectWaypoint(wp, null, false);
                            }
                            catch (Exception ex)
                            {
                                _controller.ShowPopupMessage("Extraction Failed", $"Failed to extract:\n{ex.Message}", Color.red);
                            }
                        }) { text = "EXTRACT" };
                        extBtn.AddToClassList("button");
                        extBtn.AddToClassList("btn-small");
                        extBtn.AddToClassList("btn-green");
                        extBtn.style.width = 65; extBtn.style.height = 18; extBtn.style.fontSize = 8;
                        extBtn.SetEnabled(!isInTransit && !isCooldownActive);
                        localRow.Add(extBtn);
                    }
                    
                    if (ship.nav.status == "DOCKED")
                    {
                        var refBtn = new Button(async () => {
                            _controller.SetStatusText("Refueling ship...");
                            try
                            {
                                await APIService.Instance.RefuelShip(ship.symbol);
                                _controller.ShowPopupMessage("Refuel Complete", $"Ship {ship.symbol} successfully refueled!", Color.green);
                                var newShips = await APIService.Instance.GetShips();
                                _controller.UpdatePlayerShipsExternal(newShips.data.ToList());
                                SelectWaypoint(wp, null, false);
                            }
                            catch (Exception ex)
                            {
                                _controller.ShowPopupMessage("Refuel Failed", $"Failed to refuel:\n{ex.Message}", Color.red);
                            }
                        }) { text = "REFUEL" };
                        refBtn.AddToClassList("button");
                        refBtn.AddToClassList("btn-small");
                        refBtn.AddToClassList("btn-orange");
                        refBtn.style.width = 65; refBtn.style.height = 18; refBtn.style.fontSize = 8;
                        refBtn.SetEnabled(!isInTransit);
                        localRow.Add(refBtn);
                    }

                    row.Add(localRow);
                }

                _extraContentContainer.Add(row);
            }
        }

        private void OnMapGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_mapInitialized) ResetMap();
        }

        private void OnMapClick(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            var localPos = (Vector2)evt.localPosition;
            var worldPos = (localPos - MapOffset) / MapZoom;

            if (MapModeState == DashboardController.MapMode.Galaxy)
            {
                DatabaseManager.IndexedSystem closest = null;
                float closestDist = float.MaxValue;
                foreach (var sys in _filteredGalaxySystems)
                {
                    float dist = Vector2.Distance(new Vector2(sys.X, sys.Y), worldPos);
                    if (dist < closestDist && dist < (15f / MapZoom))
                    {
                        closestDist = dist; closest = sys;
                    }
                }
                if (closest != null)
                {
                    var item = _mapContainer.parent.parent.Q<ScrollView>("system-list")?.Q<VisualElement>($"list-{closest.Symbol}");
                    SelectGalaxySystem(closest, item, false, _mapContainer.parent.parent);
                }
            }
            else
            {
                if (CurrentSystem == null) return;
                SystemWaypoint closest = null;
                float closestDist = float.MaxValue;
                foreach (var wp in CurrentSystem.waypoints)
                {
                    float dist = Vector2.Distance(GetVisualWaypointPos(wp), worldPos);
                    if (dist < closestDist && dist < (15f / MapZoom))
                    {
                        closestDist = dist; closest = wp;
                    }
                }
                if (closest != null)
                {
                    var item = _mapContainer.parent.parent.Q<ScrollView>("system-list")?.Q<VisualElement>($"list-{closest.symbol}");
                    SelectWaypoint(closest, item, false);
                }
            }
        }

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
                if (evt.button == 1 || evt.button == 2)
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
                _presenter.MapZoom = Mathf.Clamp(_presenter.MapZoom * (1f + delta), 0.01f, 10000f);
                Vector2 mousePos = evt.localMousePosition;
                Vector2 worldPos = (mousePos - _presenter.MapOffset) / oldZoom;
                _presenter.MapOffset = mousePos - (worldPos * _presenter.MapZoom);
                _presenter.RefreshMapUI();
                evt.StopPropagation();
            }
        }
    }
}
