using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using System.Collections.Generic;
using VContainer;

namespace SpaceTraders.UI
{
    public class FleetPresenter : MonoBehaviour
    {
        public VisualTreeAsset shipEntryTemplate;

        private APIService _apiService;

        [Inject]
        public void Construct(APIService apiService)
        {
            _apiService = apiService;
        }

        public void Populate(ScrollView list, Ship[] ships)
        {
            list.Clear();
            foreach (var s in ships)
            {
                var entry = shipEntryTemplate.Instantiate();
                
                entry.Q<Label>("ShipSymbol").text = s.symbol;
                entry.Q<Label>("Role").text = s.registration.role;
                entry.Q<Label>("Location").text = $"{s.nav.waypointSymbol} ({s.nav.status})";
                entry.Q<Label>("Cargo").text = $"Cargo: {s.cargo.units}/{s.cargo.capacity}";
                entry.Q<Label>("Fuel").text = $"Fuel: {s.fuel.current}/{s.fuel.capacity}";

                var orbitBtn = entry.Q<Button>("BtnOrbit");
                var dockBtn = entry.Q<Button>("BtnDock");
                orbitBtn.SetEnabled(s.nav.status == "DOCKED");
                dockBtn.SetEnabled(s.nav.status == "IN_ORBIT");

                orbitBtn.clicked += async () => {
                    try {
                        await _apiService.OrbitShip(s.symbol);
                    } catch (System.Exception e) { Debug.LogError(e.Message); }
                };
                dockBtn.clicked += async () => {
                    try {
                        await _apiService.DockShip(s.symbol);
                    } catch (System.Exception e) { Debug.LogError(e.Message); }
                };

                var extractBtn = entry.Q<Button>("BtnExtract");
                extractBtn.clicked += async () => {
                    try {
                        var res = await _apiService.ExtractResources(s.symbol);
                        Debug.Log($"Extracted {res.data.extraction.yield.units} of {res.data.extraction.yield.symbol}");
                    } catch (System.Exception e) { Debug.LogError(e.Message); }
                };

                var refuelBtn = entry.Q<Button>("BtnRefuel");
                refuelBtn.clicked += async () => {
                    try {
                        await _apiService.RefuelShip(s.symbol);
                    } catch (System.Exception e) { Debug.LogError(e.Message); }
                };

                // Cargo details
                var cargoList = entry.Q<VisualElement>("CargoList");
                cargoList.Clear();
                foreach (var item in s.cargo.inventory)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween }};
                    row.Add(new Label($"{item.symbol} x{item.units}"));
                    
                    var sellBtn = new Button { text = "Sell" };
                    sellBtn.clicked += async () => {
                        try {
                            var res = await _apiService.SellCargo(s.symbol, item.symbol, item.units);
                            Debug.Log($"Sold {item.units} {item.symbol} for {res.data.transaction.totalPrice} credits");
                        } catch (System.Exception e) { Debug.LogError(e.Message); }
                    };
                    row.Add(sellBtn);
                    cargoList.Add(row);
                }

                // Nav logic (Simplified)
                var navField = entry.Q<TextField>("TargetWaypoint");
                var navBtn = entry.Q<Button>("BtnNavigate");
                navBtn.clicked += async () => {
                    try {
                        var target = navField.value;
                        if (string.IsNullOrEmpty(target)) return;

                        var sys = await _apiService.GetSystem(s.nav.systemSymbol);
                        // Validation logic could go here
                        
                        if (s.nav.status == "DOCKED") await _apiService.OrbitShip(s.symbol);
                        var res = await _apiService.NavigateShip(s.symbol, target);
                        Debug.Log($"Navigating to {target}. Arrival: {res.data.nav.route.arrivalTime}");
                    } catch (System.Exception e) { Debug.LogError(e.Message); }
                };

                list.Add(entry);
            }
        }
    }
}
