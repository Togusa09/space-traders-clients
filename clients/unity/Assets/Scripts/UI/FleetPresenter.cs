using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using System.Collections.Generic;
using VContainer;
using Unity.Logging;

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
                        Log.Info("[FleetPresenter] Ship {Symbol} put into orbit.", s.symbol);
                    } catch (System.Exception e) { Log.Error("[FleetPresenter] Orbit failed: {Error}", e.Message); }
                };
                dockBtn.clicked += async () => {
                    try {
                        await _apiService.DockShip(s.symbol);
                        Log.Info("[FleetPresenter] Ship {Symbol} docked.", s.symbol);
                    } catch (System.Exception e) { Log.Error("[FleetPresenter] Dock failed: {Error}", e.Message); }
                };

                var extractBtn = entry.Q<Button>("BtnExtract");
                extractBtn.clicked += async () => {
                    try {
                        var res = await _apiService.ExtractResources(s.symbol);
                        Log.Info("[FleetPresenter] Ship {Symbol} extracted {Units} of {Symbol2}", s.symbol, res.data.extraction.yield.units, res.data.extraction.yield.symbol);
                    } catch (System.Exception e) { Log.Error("[FleetPresenter] Extraction failed: {Error}", e.Message); }
                };

                var refuelBtn = entry.Q<Button>("BtnRefuel");
                refuelBtn.clicked += async () => {
                    try {
                        await _apiService.RefuelShip(s.symbol);
                        Log.Info("[FleetPresenter] Ship {Symbol} refueled.", s.symbol);
                    } catch (System.Exception e) { Log.Error("[FleetPresenter] Refuel failed: {Error}", e.Message); }
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
                            Log.Info("[FleetPresenter] Sold {Units} {Symbol} for {Price} credits", item.units, item.symbol, res.data.transaction.totalPrice);
                        } catch (System.Exception e) { Log.Error("[FleetPresenter] Sell failed: {Error}", e.Message); }
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
                        Log.Info("[FleetPresenter] Navigating {Symbol} to {Target}. Arrival: {Arrival}", s.symbol, target, res.data.nav.route.arrivalTime);
                    } catch (System.Exception e) { Log.Error("[FleetPresenter] Navigation failed: {Error}", e.Message); }
                };

                list.Add(entry);
            }
        }
    }
}
