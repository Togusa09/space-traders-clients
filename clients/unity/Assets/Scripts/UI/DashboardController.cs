using System;
using System.Collections.Generic;
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
        private enum Tab { Agent, Contracts, Fleet, Systems, Factions }

        [Header("UI References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Item Templates")]
        [SerializeField] private VisualTreeAsset contractTemplate;
        [SerializeField] private VisualTreeAsset shipTemplate;
        [SerializeField] private VisualTreeAsset systemTemplate;
        [SerializeField] private VisualTreeAsset factionTemplate;

        private Tab _currentTab = Tab.Agent;
        private Label _tabTitle;
        private VisualElement _dataContainer;
        private Label _statusLabel;
        private Button _backButton;

        private Dictionary<Tab, (object data, float timestamp)> _cache = new Dictionary<Tab, (object, float)>();
        private const float CacheDuration = 60f; // 1 minute

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            _tabTitle = root.Q<Label>("tab-title");
            _dataContainer = root.Q<VisualElement>("data-container");
            _statusLabel = root.Q<Label>("status-label");
            _backButton = root.Q<Button>("back-button");

            root.Q<Button>("tab-agent").clicked += () => SwitchTab(Tab.Agent);
            root.Q<Button>("tab-contracts").clicked += () => SwitchTab(Tab.Contracts);
            root.Q<Button>("tab-fleet").clicked += () => SwitchTab(Tab.Fleet);
            root.Q<Button>("tab-systems").clicked += () => SwitchTab(Tab.Systems);
            root.Q<Button>("tab-factions").clicked += () => SwitchTab(Tab.Factions);

            _backButton.clicked += () => SceneManager.LoadScene(mainMenuSceneName);

            // Set initial token and load first tab
            SpaceTradersClient.Instance.SetToken(AuthManager.Instance.AgentToken);
            SwitchTab(Tab.Agent);
        }

        private void SwitchTab(Tab tab)
        {
            _currentTab = tab;
            _tabTitle.text = tab.ToString();
            _dataContainer.Clear();
            _statusLabel.text = string.Empty;

            if (IsCacheValid(tab))
            {
                _statusLabel.text = "(Loaded from cache)";
                DisplayCachedData(tab);
            }
            else
            {
                FetchData(tab);
            }
        }

        private bool IsCacheValid(Tab tab)
        {
            return _cache.ContainsKey(tab) && (Time.time - _cache[tab].timestamp) < CacheDuration;
        }

        private void FetchData(Tab tab)
        {
            _statusLabel.text = "Fetching data...";
            switch (tab)
            {
                case Tab.Agent:
                    APIService.Instance.GetMyAgent(res => CacheAndDisplay(Tab.Agent, res.data, DisplayAgent), OnError);
                    break;
                case Tab.Contracts:
                    APIService.Instance.GetContracts(res => CacheAndDisplay(Tab.Contracts, res.data, data => DisplayList(Tab.Contracts, (Contract[])data)), OnError);
                    break;
                case Tab.Fleet:
                    apiServiceInstance.GetShips(res => CacheAndDisplay(Tab.Fleet, res.data, data => DisplayList(Tab.Fleet, (Ship[])data)), OnError);
                    break;
                case Tab.Systems:
                    APIService.Instance.GetSystems(res => CacheAndDisplay(Tab.Systems, res.data, data => DisplayList(Tab.Systems, (SystemData[])data)), OnError);
                    break;
                case Tab.Factions:
                    APIService.Instance.GetFactions(res => CacheAndDisplay(Tab.Factions, res.data, data => DisplayList(Tab.Factions, (Faction[])data)), OnError);
                    break;
            }
        }
        
        // Helper to fix the accidental rename during refactor
        private APIService apiServiceInstance => APIService.Instance;

        private void CacheAndDisplay<T>(Tab tab, T data, Action<T> displayAction)
        {
            _cache[tab] = (data, Time.time);
            _statusLabel.text = string.Empty;
            displayAction(data);
        }

        private void DisplayCachedData(Tab tab)
        {
            var data = _cache[tab].data;
            switch (tab)
            {
                case Tab.Agent: DisplayAgent((Agent)data); break;
                case Tab.Contracts: DisplayList(Tab.Contracts, (Contract[])data); break;
                case Tab.Fleet: DisplayList(Tab.Fleet, (Ship[])data); break;
                case Tab.Systems: DisplayList(Tab.Systems, (SystemData[])data); break;
                case Tab.Factions: DisplayList(Tab.Factions, (Faction[])data); break;
            }
        }

        private void OnError(string error)
        {
            _statusLabel.text = $"Error: {error}";
        }

        private void DisplayAgent(Agent agent)
        {
            AddRow("Symbol", agent.symbol);
            AddRow("Headquarters", agent.headquarters);
            AddRow("Credits", agent.credits.ToString("N0"));
            AddRow("Starting Faction", agent.startingFaction);
            AddRow("AccountId", agent.accountId);
        }

        private void DisplayList<T>(Tab tab, T[] items)
        {
            if (items == null || items.Length == 0)
            {
                _dataContainer.Add(new Label("No items found."));
                return;
            }

            foreach (var item in items)
            {
                VisualElement entry = null;

                if (item is Contract c) entry = BindContract(c);
                else if (item is Ship s) entry = BindShip(s);
                else if (item is SystemData sys) entry = BindSystem(sys);
                else if (item is Faction f) entry = BindFaction(f);

                if (entry != null)
                {
                    _dataContainer.Add(entry);
                }
            }
        }

        private VisualElement BindContract(Contract c)
        {
            var element = contractTemplate.Instantiate();
            element.Q<Label>("id-label").text = $"ID: {c.id}";
            element.Q<Label>("type-label").text = $"Type: {c.type} | Faction: {c.factionSymbol}";
            element.Q<Label>("status-label").text = $"Accepted: {c.accepted} | Fulfilled: {c.fulfilled}";
            return element;
        }

        private VisualElement BindShip(Ship s)
        {
            var element = shipTemplate.Instantiate();
            element.Q<Label>("symbol-label").text = $"Symbol: {s.symbol}";
            element.Q<Label>("details-label").text = $"Role: {s.registration.role} | System: {s.nav.systemSymbol}";
            element.Q<Label>("status-label").text = $"Status: {s.nav.status} | Fuel: {s.fuel.current}/{s.fuel.capacity}";
            return element;
        }

        private VisualElement BindSystem(SystemData sys)
        {
            var element = systemTemplate.Instantiate();
            element.Q<Label>("symbol-label").text = $"Symbol: {sys.symbol}";
            element.Q<Label>("details-label").text = $"Type: {sys.type} | Waypoints: {sys.waypoints.Length}";
            return element;
        }

        private VisualElement BindFaction(Faction f)
        {
            var element = factionTemplate.Instantiate();
            element.Q<Label>("name-label").text = $"Name: {f.name}";
            element.Q<Label>("details-label").text = $"Symbol: {f.symbol} | HQ: {f.headquarters}";
            element.Q<Label>("description-label").text = f.description;
            return element;
        }

        private void AddRow(string key, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 5;

            var keyLabel = new Label($"{key}: ");
            keyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            keyLabel.style.width = 150;
            keyLabel.style.color = Color.gray;

            var valueLabel = new Label(value);
            valueLabel.style.color = Color.white;
            valueLabel.style.flexGrow = 1;

            row.Add(keyLabel);
            row.Add(valueLabel);
            _dataContainer.Add(row);
        }
    }
}
