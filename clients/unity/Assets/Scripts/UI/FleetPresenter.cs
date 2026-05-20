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
    public class FleetPresenter
    {
        private readonly DashboardController _controller;

        public FleetPresenter(DashboardController controller)
        {
            _controller = controller;
        }

        public VisualElement BindShip(Ship s, VisualTreeAsset shipTemplate)
        {
            var element = shipTemplate.Instantiate();
            var root = element.Q<VisualElement>(null, "dashboard-entry");
            if (root == null) root = element.Q<VisualElement>();
            
            // Name the root element so we can find it in Update() for timers
            root.name = $"ship-{s.symbol}";

            element.Q<Label>("symbol-label").text = s.symbol;
            element.Q<Label>("details-label").text = $"Role: {s.registration.role} | Location: {s.nav.waypointSymbol}";
            element.Q<Label>("status-label").text = $"Status: {s.nav.status} | Fuel: {s.fuel.current}/{s.fuel.capacity}";
            element.Q<Label>("cargo-capacity-label").text = $"Cargo: {s.cargo.units}/{s.cargo.capacity}";

            bool isInTransit = s.nav.status == "IN_TRANSIT" || _controller.ActiveTimers.Any(t => t.ShipSymbol == s.symbol && !t.IsCooldown);
            bool isCooldownActive = _controller.ActiveTimers.Any(t => t.ShipSymbol == s.symbol && t.IsCooldown);

            // Map button
            var mapBtn = element.Q<Button>("show-on-map-btn");
            if (mapBtn != null)
            {
                mapBtn.clicked += () => _ = _controller.OpenSystemFromExternal(s.nav.systemSymbol, s.nav.waypointSymbol);
            }

            // Orbit/Dock button
            var orbitDockBtn = element.Q<Button>("action-orbit-dock-btn");
            if (orbitDockBtn != null)
            {
                bool isDocked = s.nav.status == "DOCKED";
                orbitDockBtn.text = isDocked ? "ORBIT" : "DOCK";
                orbitDockBtn.SetEnabled(!isInTransit);
                orbitDockBtn.clicked += async () => {
                    _controller.SetStatusText(isDocked ? "Transitioning to orbit..." : "Docking ship...");
                    try
                    {
                        if (isDocked)
                            await APIService.Instance.OrbitShip(s.symbol);
                        else
                            await APIService.Instance.DockShip(s.symbol);
                        
                        _controller.ShowPopupMessage("Ship Status Changed", $"Ship {s.symbol} successfully {(isDocked ? "entered orbit" : "docked")}!", Color.green);
                        _controller.TriggerTabSwitch(DashboardController.Tab.Fleet);
                    }
                    catch (Exception ex)
                    {
                        _controller.ShowPopupMessage("Action Failed", $"Failed to change status:\n{ex.Message}", Color.red);
                    }
                };
            }

            // Extract button
            var extractBtn = element.Q<Button>("action-extract-btn");
            if (extractBtn != null)
            {
                bool canExtract = s.nav.status == "IN_ORBIT" && s.nav.waypointSymbol.Contains("ASTEROID") && !isInTransit && !isCooldownActive;
                extractBtn.SetEnabled(canExtract);
                extractBtn.clicked += async () => {
                    _controller.SetStatusText("Extracting resources...");
                    try
                    {
                        var res = await APIService.Instance.ExtractResources(s.symbol);
                        _controller.ShowPopupMessage("Extraction Complete", $"Yield: {res.data.extraction.yield.units} units of {res.data.extraction.yield.symbol}!\nCooldown: {res.data.cooldown.totalSeconds}s", Color.green);
                        
                        // Add cooldown timer safely
                        string expStr = res.data?.cooldown?.expiration;
                        DateTime expiration = !string.IsNullOrEmpty(expStr)
                            ? DateTime.Parse(expStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal)
                            : DateTime.UtcNow.AddSeconds(res.data?.cooldown?.totalSeconds ?? 0);

                        _controller.ActiveTimers.RemoveAll(t => t.ShipSymbol == s.symbol && t.IsCooldown);
                        _controller.ActiveTimers.Add(new DashboardController.ActiveTimer {
                            ShipSymbol = s.symbol,
                            Expiration = expiration,
                            TotalDuration = res.data.cooldown.totalSeconds,
                            IsCooldown = true
                        });

                        _controller.TriggerTabSwitch(DashboardController.Tab.Fleet);
                    }
                    catch (Exception ex)
                    {
                        _controller.ShowPopupMessage("Extraction Failed", $"Failed to extract:\n{ex.Message}", Color.red);
                    }
                };
            }

            // Refuel button
            var refuelBtn = element.Q<Button>("action-refuel-btn");
            if (refuelBtn != null)
            {
                bool canRefuel = s.nav.status == "DOCKED" && !isInTransit;
                refuelBtn.SetEnabled(canRefuel);
                refuelBtn.clicked += async () => {
                    _controller.SetStatusText("Refueling ship...");
                    try
                    {
                        await APIService.Instance.RefuelShip(s.symbol);
                        _controller.ShowPopupMessage("Refueling Successful", $"Ship {s.symbol} successfully refueled!", Color.green);
                        _controller.TriggerTabSwitch(DashboardController.Tab.Fleet);
                    }
                    catch (Exception ex)
                    {
                        _controller.ShowPopupMessage("Refuel Failed", $"Failed to refuel:\n{ex.Message}", Color.red);
                    }
                };
            }

            // Navigation Dropdown
            var dropdownPlaceholder = element.Q<VisualElement>("nav-dropdown-placeholder");
            if (dropdownPlaceholder != null)
            {
                var dropdown = new DropdownField();
                dropdown.style.flexGrow = 1;
                dropdown.style.height = Length.Percent(100);
                dropdownPlaceholder.Add(dropdown);
                
                if (isInTransit)
                {
                    dropdown.choices = new List<string> { "In Transit" };
                    dropdown.value = "In Transit";
                    dropdown.SetEnabled(false);
                }
                else
                {
                    dropdown.choices = new List<string> { "Loading..." };
                    dropdown.value = "Loading...";
                    _ = PopulateNavDropdown(dropdown, s);
                }
            }

            // Render cargo list
            var cargoListContainer = element.Q<VisualElement>("cargo-list-container");
            if (cargoListContainer != null)
            {
                cargoListContainer.Clear();
                if (s.cargo.inventory == null || s.cargo.inventory.Length == 0)
                {
                    cargoListContainer.Add(new Label("Empty cargo bay") { style = { fontSize = 10, color = Color.gray } });
                }
                else
                {
                    foreach (var item in s.cargo.inventory)
                    {
                        var row = new VisualElement();
                        row.AddToClassList("inventory-row");
                        
                        row.Add(new Label($"{item.name} ({item.units} units)") { style = { fontSize = 10 } });

                        if (s.nav.status == "DOCKED")
                        {
                            var sellBtn = new Button(async () => {
                                _controller.SetStatusText($"Selling {item.symbol}...");
                                try
                                {
                                    var res = await APIService.Instance.SellCargo(s.symbol, item.symbol, item.units);
                                    _controller.ShowPopupMessage("Cargo Sold", $"Sold {item.units} units of {item.symbol}!\nCredits gained: {res.data.transaction.totalPrice:N0} C", Color.green);
                                    _controller.TriggerTabSwitch(DashboardController.Tab.Fleet);
                                }
                                catch (Exception ex)
                                {
                                    _controller.ShowPopupMessage("Sale Failed", $"Failed to sell cargo:\n{ex.Message}", Color.red);
                                }
                            }) { text = "SELL ALL" };
                            sellBtn.AddToClassList("button");
                            sellBtn.AddToClassList("btn-small");
                            sellBtn.AddToClassList("btn-red");
                            sellBtn.style.width = 65; sellBtn.style.height = 20; sellBtn.style.fontSize = 8;
                            row.Add(sellBtn);
                        }

                        cargoListContainer.Add(row);
                    }
                }
            }

            // Transit/Cooldown countdown display if active
            var timerContainer = element.Q<VisualElement>("timer-container");
            var timer = _controller.ActiveTimers.FirstOrDefault(t => t.ShipSymbol == s.symbol);
            if (timer != null && timerContainer != null)
            {
                double remaining = (timer.Expiration - DateTime.UtcNow).TotalSeconds;
                if (remaining > 0)
                {
                    timerContainer.style.display = DisplayStyle.Flex;
                    var label = element.Q<Label>("timer-label");
                    var bar = element.Q<VisualElement>("timer-bar-fill");
                    if (timer.IsCooldown)
                    {
                        label.text = $"Mining Cooldown: {Mathf.CeilToInt((float)remaining)}s remaining";
                        bar.AddToClassList("timer-bar-fill--cooldown");
                    }
                    else
                    {
                        label.text = $"Transit: {Mathf.CeilToInt((float)remaining)}s remaining";
                        bar.RemoveFromClassList("timer-bar-fill--cooldown");
                    }
                    float pct = Mathf.Clamp01(1f - ((float)remaining / (float)timer.TotalDuration));
                    bar.style.width = Length.Percent(pct * 100f);
                }
            }

            return element;
        }

        private async Task PopulateNavDropdown(DropdownField dropdown, Ship s)
        {
            try
            {
                bool isInTransit = s.nav.status == "IN_TRANSIT" || _controller.ActiveTimers.Any(t => t.ShipSymbol == s.symbol && !t.IsCooldown);
                if (isInTransit)
                {
                    dropdown.choices = new List<string> { "In Transit" };
                    dropdown.value = "In Transit";
                    dropdown.SetEnabled(false);
                    return;
                }
                var sys = await APIService.Instance.GetSystem(s.nav.systemSymbol);
                if (sys != null && sys.data != null && sys.data.waypoints != null)
                {
                    var wps = sys.data.waypoints.Select(w => w.symbol).ToList();
                    dropdown.choices = wps;
                    dropdown.value = s.nav.waypointSymbol;
                    dropdown.RegisterValueChangedCallback(async evt => {
                        if (evt.newValue == s.nav.waypointSymbol || evt.newValue == "Loading..." || evt.newValue == "In Transit") return;
                        
                        // Check if in orbit
                        if (s.nav.status != "IN_ORBIT")
                        {
                            _controller.ShowChoicePopupMessage("Ship is Docked",
                                $"Ship {s.symbol} must be in orbit before navigating. Orbit now and proceed to {evt.newValue}?",
                                "ORBIT & NAVIGATE",
                                async () => {
                                    _controller.SetStatusText($"Orbiting and navigating {s.symbol} to {evt.newValue}...");
                                    try
                                    {
                                        await APIService.Instance.OrbitShip(s.symbol);
                                        var res = await APIService.Instance.NavigateShip(s.symbol, evt.newValue);
                                        _controller.HandleNavigationResponseExternal(s.symbol, res, evt.newValue);
                                        _controller.TriggerTabSwitch(DashboardController.Tab.Fleet);
                                    }
                                    catch (Exception ex)
                                    {
                                        _controller.ShowPopupMessage("Navigation Failed", $"Failed to orbit and navigate:\n{ex.Message}", Color.red);
                                    }
                                }
                            );
                            dropdown.SetValueWithoutNotify(s.nav.waypointSymbol);
                            return;
                        }

                        _controller.SetStatusText($"Navigating {s.symbol} to {evt.newValue}...");
                        try
                        {
                            var res = await APIService.Instance.NavigateShip(s.symbol, evt.newValue);
                            _controller.HandleNavigationResponseExternal(s.symbol, res, evt.newValue);
                            _controller.TriggerTabSwitch(DashboardController.Tab.Fleet);
                        }
                        catch (Exception ex)
                        {
                            _controller.ShowPopupMessage("Navigation Failed", $"Failed to navigate:\n{ex.Message}", Color.red);
                            dropdown.SetValueWithoutNotify(s.nav.waypointSymbol);
                        }
                    });
                }
            }
            catch
            {
                dropdown.choices = new List<string> { "Error loading" };
            }
        }

        public void UpdateTimers(VisualElement dataContainer)
        {
            var activeTimers = _controller.ActiveTimers;
            if (activeTimers.Count == 0) return;

            DateTime now = DateTime.UtcNow;
            for (int i = activeTimers.Count - 1; i >= 0; i--)
            {
                var timer = activeTimers[i];
                double remaining = (timer.Expiration - now).TotalSeconds;
                if (remaining <= 0)
                {
                    activeTimers.RemoveAt(i);
                    // Refresh visual elements to enable buttons again when finished
                    if (_controller.CurrentTab == DashboardController.Tab.Fleet)
                        _controller.TriggerTabSwitch(DashboardController.Tab.Fleet);
                    continue;
                }

                var card = dataContainer.Q<VisualElement>($"ship-{timer.ShipSymbol}");
                if (card != null)
                {
                    var container = card.Q<VisualElement>("timer-container");
                    if (container != null)
                    {
                        container.style.display = DisplayStyle.Flex;
                        var label = card.Q<Label>("timer-label");
                        var bar = card.Q<VisualElement>("timer-bar-fill");
                        
                        if (timer.IsCooldown)
                        {
                            label.text = $"Mining Cooldown: {Mathf.CeilToInt((float)remaining)}s remaining";
                            bar.AddToClassList("timer-bar-fill--cooldown");
                        }
                        else
                        {
                            label.text = $"Transit: {Mathf.CeilToInt((float)remaining)}s remaining";
                            bar.RemoveFromClassList("timer-bar-fill--cooldown");
                        }

                        float pct = Mathf.Clamp01(1f - ((float)remaining / (float)timer.TotalDuration));
                        bar.style.width = Length.Percent(pct * 100f);
                    }
                }
            }
        }
    }
}
