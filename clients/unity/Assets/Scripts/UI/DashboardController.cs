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
        public enum Tab { Agent, Contracts, Fleet, Map, Factions }
        public enum MapMode { Galaxy, System }

        // Active Cooldown/Transit timers
        public class ActiveTimer
        {
            public string ShipSymbol;
            public DateTime Expiration;
            public double TotalDuration;
            public bool IsCooldown;
        }

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

        // Presenters
        private MapPresenter _mapPresenter;
        private FleetPresenter _fleetPresenter;
        private ContractPresenter _contractPresenter;

        // Shared State
        private Tab _currentTab = Tab.Agent;
        private VisualElement _dataContainer;
        private Label _statusLabel;
        private Dictionary<Tab, Button> _tabButtons = new Dictionary<Tab, Button>();
        private List<Ship> _playerShips = new List<Ship>();
        private List<ActiveTimer> _activeTimers = new List<ActiveTimer>();
        private int _requestSequence = 0;

        // Global Popup References
        private VisualElement _popupInstance, _popupOverlay, _popupDataContainer;
        private Label _popupTitle;
        private Button _popupCloseButton;

        // Public Accessors for Presenters
        public List<Ship> PlayerShips => _playerShips;
        public List<ActiveTimer> ActiveTimers => _activeTimers;
        public Tab CurrentTab => _currentTab;

        private void Awake()
        {
            bool hasErrors = false;
            if (uiDocument == null) { Debug.LogError("[DashboardController] Inspector Reference Missing: uiDocument is null!"); hasErrors = true; }
            if (contractTemplate == null) { Debug.LogError("[DashboardController] Inspector Reference Missing: contractTemplate is null!"); hasErrors = true; }
            if (shipTemplate == null) { Debug.LogError("[DashboardController] Inspector Reference Missing: shipTemplate is null!"); hasErrors = true; }
            if (systemTemplate == null) { Debug.LogError("[DashboardController] Inspector Reference Missing: systemTemplate is null!"); hasErrors = true; }
            if (factionTemplate == null) { Debug.LogError("[DashboardController] Inspector Reference Missing: factionTemplate is null!"); hasErrors = true; }
            if (systemPanelTemplate == null) { Debug.LogError("[DashboardController] Inspector Reference Missing: systemPanelTemplate is null!"); hasErrors = true; }
            if (waypointIconTemplate == null) { Debug.LogError("[DashboardController] Inspector Reference Missing: waypointIconTemplate is null!"); hasErrors = true; }

            if (hasErrors)
            {
                Debug.LogError("[DashboardController] Critical startup failure: Essential inspector references are missing! The dashboard may not function correctly.");
            }

            _mapPresenter = new MapPresenter(this);
            _fleetPresenter = new FleetPresenter(this);
            _contractPresenter = new ContractPresenter(this);
        }

        private void OnDisable()
        {
            AuthManager.OnTokenUnauthorized -= HandleOnTokenUnauthorized;
        }

        private void HandleOnTokenUnauthorized()
        {
            ShowPopup("Session Expired", "Your session has expired. You will be redirected to the main menu.", Color.red);
            _ = RedirectToMainMenuAfterDelay();
        }

        private async Task RedirectToMainMenuAfterDelay()
        {
            await Task.Delay(3000);
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnEnable()
        {
            AuthManager.OnTokenUnauthorized += HandleOnTokenUnauthorized;

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

            // Global Popup Initialization
            _popupInstance = root.Q<VisualElement>("popup-instance");
            _popupOverlay = root.Q<VisualElement>("popup-overlay");
            _popupDataContainer = root.Q<VisualElement>("popup-data-container");
            _popupTitle = root.Q<Label>("popup-title");
            _popupCloseButton = root.Q<Button>("popup-close-button");
            if (_popupCloseButton != null)
            {
                _popupCloseButton.clicked += () => {
                    _popupOverlay.style.display = DisplayStyle.None;
                    _popupInstance.style.display = DisplayStyle.None;
                };
            }

            SwitchTab(Tab.Agent);
        }

        public void SetStatusText(string text)
        {
            if (_statusLabel != null) _statusLabel.text = text;
        }

        public void ShowPopupMessage(string title, string content, Color? textColor = null)
        {
            ShowPopup(title, content, textColor);
        }

        public void ShowChoicePopupMessage(string title, string content, string confirmText, Func<Task> onConfirm, string cancelText = "CANCEL")
        {
            ShowChoicePopup(title, content, confirmText, onConfirm, cancelText);
        }

        public void TriggerTabSwitch(Tab tab)
        {
            SwitchTab(tab);
        }

        public void HandleNavigationResponseExternal(string shipSymbol, NavigateResponse res, string destinationSymbol)
        {
            HandleNavigationResponse(shipSymbol, res, destinationSymbol);
        }

        public void UpdatePlayerShipsExternal(List<Ship> ships)
        {
            _playerShips = ships;
            UpdateActiveTimersFromShips(_playerShips);
        }

        public async Task OpenSystemFromExternal(string systemSymbol, string focusWaypoint)
        {
            SwitchTab(Tab.Map);
            if (_mapPresenter != null)
            {
                await _mapPresenter.OpenSystem(systemSymbol, focusWaypoint);
            }
        }

        private void Update()
        {
            if (_fleetPresenter != null)
            {
                _fleetPresenter.UpdateTimers(_dataContainer);
            }
        }

        private void ShowPopup(string title, string content, Color? textColor = null)
        {
            if (_popupTitle == null || _popupDataContainer == null || _popupInstance == null || _popupOverlay == null) return;
            _popupTitle.text = title;
            _popupDataContainer.Clear();
            var label = new Label(content) { style = { whiteSpace = WhiteSpace.Normal, color = textColor ?? Color.white } };
            _popupDataContainer.Add(label);
            
            if (_popupCloseButton != null) _popupCloseButton.style.display = DisplayStyle.Flex;

            _popupInstance.style.display = DisplayStyle.Flex;
            _popupOverlay.style.display = DisplayStyle.Flex;
        }

        private void ShowChoicePopup(string title, string content, string confirmText, Func<Task> onConfirm, string cancelText = "CANCEL")
        {
            if (_popupTitle == null || _popupDataContainer == null || _popupInstance == null || _popupOverlay == null) return;
            _popupTitle.text = title;
            _popupDataContainer.Clear();
            
            var label = new Label(content) { style = { whiteSpace = WhiteSpace.Normal, color = Color.white, marginBottom = 15 } };
            _popupDataContainer.Add(label);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };

            var cancelBtn = new Button(() => {
                _popupOverlay.style.display = DisplayStyle.None;
                _popupInstance.style.display = DisplayStyle.None;
            }) { text = cancelText };
            cancelBtn.AddToClassList("button");
            cancelBtn.AddToClassList("btn-small");
            cancelBtn.AddToClassList("btn-red");
            cancelBtn.style.width = 100; cancelBtn.style.height = 25;
            buttonRow.Add(cancelBtn);

            var confirmBtn = new Button(async () => {
                _popupOverlay.style.display = DisplayStyle.None;
                _popupInstance.style.display = DisplayStyle.None;
                await onConfirm();
            }) { text = confirmText };
            confirmBtn.AddToClassList("button");
            confirmBtn.AddToClassList("btn-small");
            confirmBtn.AddToClassList("btn-green");
            confirmBtn.style.width = 150; confirmBtn.style.height = 25;
            buttonRow.Add(confirmBtn);

            _popupDataContainer.Add(buttonRow);
            
            if (_popupCloseButton != null) _popupCloseButton.style.display = DisplayStyle.None;

            _popupInstance.style.display = DisplayStyle.Flex;
            _popupOverlay.style.display = DisplayStyle.Flex;
        }

        private void HandleNavigationResponse(string shipSymbol, NavigateResponse res, string destinationSymbol)
        {
            if (res?.data?.nav?.route == null) return;

            string depTimeStr = res.data.nav.route.departureTime;
            string arrTimeStr = res.data.nav.route.arrivalTime;

            DateTime departure = !string.IsNullOrEmpty(depTimeStr)
                ? DateTime.Parse(depTimeStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal)
                : DateTime.UtcNow;

            DateTime arrival = !string.IsNullOrEmpty(arrTimeStr)
                ? DateTime.Parse(arrTimeStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal)
                : DateTime.UtcNow.AddSeconds(60);

            double duration = (arrival - departure).TotalSeconds;

            _activeTimers.RemoveAll(t => t.ShipSymbol == shipSymbol && !t.IsCooldown);
            _activeTimers.Add(new ActiveTimer
            {
                ShipSymbol = shipSymbol,
                Expiration = arrival,
                TotalDuration = duration > 0 ? duration : 60,
                IsCooldown = false
            });

            ShowPopup("Navigation Initiated", $"Ship {shipSymbol} is in transit to {destinationSymbol}!\nEstimated Arrival: {res.data.nav.route.arrivalTime}", Color.green);
        }

        private void UpdateActiveTimersFromShips(List<Ship> ships)
        {
            if (ships == null) return;
            DateTime now = DateTime.UtcNow;

            foreach (var s in ships)
            {
                if (s.nav != null && s.nav.status == "IN_TRANSIT" && s.nav.route != null)
                {
                    _activeTimers.RemoveAll(t => t.ShipSymbol == s.symbol && !t.IsCooldown);
                    try
                    {
                        string depTimeStr = s.nav.route.departureTime;
                        string arrTimeStr = s.nav.route.arrivalTime;

                        DateTime departure = !string.IsNullOrEmpty(depTimeStr)
                            ? DateTime.Parse(depTimeStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal)
                            : DateTime.UtcNow;

                        DateTime arrival = !string.IsNullOrEmpty(arrTimeStr)
                            ? DateTime.Parse(arrTimeStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal)
                            : DateTime.UtcNow.AddSeconds(60);

                        double remaining = (arrival - now).TotalSeconds;
                        if (remaining > 0)
                        {
                            double duration = (arrival - departure).TotalSeconds;
                            _activeTimers.Add(new ActiveTimer
                            {
                                ShipSymbol = s.symbol,
                                Expiration = arrival,
                                TotalDuration = duration > 0 ? duration : 60,
                                IsCooldown = false
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[DashboardController] Failed to parse transit times for {s.symbol}: {ex.Message}");
                    }
                }
                else if (s.nav != null && s.nav.status != "IN_TRANSIT")
                {
                    _activeTimers.RemoveAll(t => t.ShipSymbol == s.symbol && !t.IsCooldown);
                }
            }
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
            SetStatusText(string.Empty);

            if (tab == Tab.Map)
            {
                if (_playerShips.Count == 0)
                {
                    _ = FetchShipsBeforeMap();
                }
                _ = _mapPresenter.SetupMapPanelAsync(_dataContainer, systemPanelTemplate, systemTemplate);
                return;
            }

            _ = FetchAndDisplayTab(tab, sequence);
        }

        private async Task FetchShipsBeforeMap()
        {
            try
            {
                var ships = await APIService.Instance.GetShips();
                _playerShips = ships.data.ToList();
                UpdateActiveTimersFromShips(_playerShips);
            }
            catch { }
        }

        private async Task FetchAndDisplayTab(Tab tab, int sequence)
        {
            SetStatusText("Fetching data...");
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
                            UpdateActiveTimersFromShips(_playerShips);
                            DisplayList(Tab.Fleet, ships.data);
                        }
                        break;
                    case Tab.Factions:
                        var factions = await APIService.Instance.GetFactions();
                        if (sequence == _requestSequence) DisplayList(Tab.Factions, factions.data);
                        break;
                }
                if (sequence == _requestSequence) SetStatusText(string.Empty);
            }
            catch (Exception e)
            {
                if (sequence == _requestSequence) SetStatusText($"Error: {e.Message}");
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
                if (item is Contract c) entry = _contractPresenter.BindContract(c, contractTemplate);
                else if (item is Ship s) entry = _fleetPresenter.BindShip(s, shipTemplate);
                else if (item is Faction f) entry = BindFaction(f);
                if (entry != null) root.Add(entry);
            }
        }

        private VisualElement BindFaction(Faction f)
        {
            var element = factionTemplate.Instantiate();
            element.Q<Label>("name-label").text = $"Name: {f.name}";
            element.Q<Label>("details-label").text = $"Symbol: {f.symbol} | HQ: {f.headquarters}";
            element.Q<Label>("description-label").text = f.description;
            return element;
        }

        private void AddRow(VisualElement root, string key, string value)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5 } };
            row.Add(new Label($"{key}: ") { style = { unityFontStyleAndWeight = FontStyle.Bold, width = 150, color = Color.gray } });
            row.Add(new Label(value) { style = { color = Color.white, flexGrow = 1 } });
            root.Add(row);
        }
    }
}
