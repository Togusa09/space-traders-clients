using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;
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
            if (list == null) return;
            list.Clear();

            foreach (var s in ships)
            {
                if (shipEntryTemplate == null) continue;
                var entry = shipEntryTemplate.Instantiate();
                
                // Mapping to Entry_Ship.uxml (kebab-case)
                var symbolLabel = entry.Q<Label>("symbol-label");
                var detailsLabel = entry.Q<Label>("details-label");
                var statusLabel = entry.Q<Label>("status-label");
                var cargoLabel = entry.Q<Label>("cargo-capacity-label");

                if (symbolLabel != null) symbolLabel.text = s.Symbol;
                if (detailsLabel != null) detailsLabel.text = $"Role: {s.Registration.Role} | Location: {s.Nav.WaypointSymbol}";
                if (statusLabel != null) statusLabel.text = $"Status: {s.Nav.Status} | Fuel: {s.Fuel.Current}/{s.Fuel.Capacity}";
                if (cargoLabel != null) cargoLabel.text = $"Cargo: {s.Cargo.Units}/{s.Cargo.Capacity}";

                // Buttons
                var orbitBtn = entry.Q<Button>("action-orbit-dock-btn");
                var extractBtn = entry.Q<Button>("action-extract-btn");
                var refuelBtn = entry.Q<Button>("action-refuel-btn");

                if (orbitBtn != null)
                {
                    bool inTransit = s.Nav.Status == ShipNavStatus.INTRANSIT;
                    bool isDocked = s.Nav.Status == ShipNavStatus.DOCKED;

                    orbitBtn.text = isDocked ? "ORBIT" : "DOCK";
                    orbitBtn.SetEnabled(!inTransit);
                    
                    orbitBtn.clicked += async () => {
                        try {
                            orbitBtn.SetEnabled(false);
                            if (isDocked) await _apiService.OrbitShip(s.Symbol);
                            else await _apiService.DockShip(s.Symbol);
                            Log.Info("[FleetPresenter] Ship {Symbol} status toggled.", s.Symbol);
                        } 
                        catch (System.Exception e) { Log.Error("[FleetPresenter] Toggle status failed: {Error}", e.Message); }
                        finally { if (orbitBtn != null) orbitBtn.SetEnabled(true); }
                    };
                }

                if (extractBtn != null)
                {
                    bool canExtract = s.Nav.Status == ShipNavStatus.INORBIT;
                    extractBtn.SetEnabled(canExtract);
                    extractBtn.clicked += async () => {
                        try {
                            extractBtn.SetEnabled(false);
                            var res = await _apiService.ExtractResources(s.Symbol);
                            Log.Info("[FleetPresenter] Extracted {Units} of {Symbol}", res.Data.Extraction.Yield.Units, res.Data.Extraction.Yield.Symbol);
                        } catch (System.Exception e) { Log.Error("[FleetPresenter] Extraction failed: {Error}", e.Message); }
                        finally { if (extractBtn != null) extractBtn.SetEnabled(true); }
                    };
                }

                if (refuelBtn != null)
                {
                    bool canRefuel = s.Nav.Status == ShipNavStatus.DOCKED;
                    refuelBtn.SetEnabled(canRefuel);
                    refuelBtn.clicked += async () => {
                        try {
                            refuelBtn.SetEnabled(false);
                            await _apiService.RefuelShip(s.Symbol);
                            Log.Info("[FleetPresenter] Ship {Symbol} refueled.", s.Symbol);
                        } catch (System.Exception e) { Log.Error("[FleetPresenter] Refuel failed: {Error}", e.Message); }
                        finally { if (refuelBtn != null) refuelBtn.SetEnabled(true); }
                    };
                }

                // Cargo List
                var cargoContainer = entry.Q<VisualElement>("cargo-list-container");
                if (cargoContainer != null)
                {
                    cargoContainer.Clear();
                    foreach (var item in s.Cargo.Inventory)
                    {
                        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween }};
                        row.Add(new Label($"{item.Symbol} x{item.Units}"));
                        
                        var sellBtn = new Button { text = "SELL" };
                        sellBtn.clicked += async () => {
                            try {
                                sellBtn.SetEnabled(false);
                                var res = await _apiService.SellCargo(s.Symbol, item.Symbol.ToString(), item.Units);
                                Log.Info("[FleetPresenter] Sold {Units} {Symbol} for {Price}", item.Units, item.Symbol, res.Data.Transaction.TotalPrice);
                            } catch (System.Exception e) { Log.Error("[FleetPresenter] Sell failed: {Error}", e.Message); }
                            finally { if (sellBtn != null) sellBtn.SetEnabled(true); }
                        };
                        row.Add(sellBtn);
                        cargoContainer.Add(row);
                    }
                }

                list.Add(entry);
            }
        }
    }
}
