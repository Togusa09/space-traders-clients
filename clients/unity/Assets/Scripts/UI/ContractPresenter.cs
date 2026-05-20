using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using System.Collections.Generic;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.UI
{
    public class ContractPresenter : MonoBehaviour
    {
        public VisualTreeAsset contractEntryTemplate;

        private APIService _apiService;

        [Inject]
        public void Construct(APIService apiService)
        {
            _apiService = apiService;
        }

        public void Populate(ScrollView list, Contract[] contracts)
        {
            list.Clear();
            foreach (var c in contracts)
            {
                var entry = contractEntryTemplate.Instantiate();
                
                entry.Q<Label>("ContractId").text = $"ID: {c.id}";
                entry.Q<Label>("Faction").text = $"Faction: {c.factionSymbol}";
                entry.Q<Label>("Type").text = $"Type: {c.type}";
                entry.Q<Label>("Deadline").text = $"Deadline: {c.terms.deadline}";
                
                var status = entry.Q<Label>("Status");
                status.text = c.accepted ? (c.fulfilled ? "FULFILLED" : "ACCEPTED") : "UNACCEPTED";
                status.style.color = c.fulfilled ? Color.green : (c.accepted ? Color.cyan : Color.yellow);

                var acceptBtn = entry.Q<Button>("BtnAccept");
                acceptBtn.SetEnabled(!c.accepted);
                acceptBtn.clicked += async () => {
                    try {
                        await _apiService.AcceptContract(c.id);
                        Log.Info("[ContractPresenter] Contract {Id} accepted.", c.id);
                    } catch (System.Exception e) { Log.Error("[ContractPresenter] Accept failed: {Error}", e.Message); }
                };

                var fulfillBtn = entry.Q<Button>("BtnFulfill");
                fulfillBtn.SetEnabled(c.accepted && !c.fulfilled);
                
                // Deliverables
                var delList = entry.Q<VisualElement>("Deliverables");
                delList.Clear();
                bool allDelivered = true;
                foreach (var d in c.terms.deliver)
                {
                    var dLabel = new Label($"{d.tradeSymbol}: {d.unitsFulfilled}/{d.unitsRequired} to {d.destinationSymbol}");
                    delList.Add(dLabel);
                    if (d.unitsFulfilled < d.unitsRequired) allDelivered = false;

                    // Quick delivery button (simplified)
                    var deliverBtn = new Button { text = "Deliver" };
                    deliverBtn.clicked += async () => {
                        try {
                            var shipsRes = await _apiService.GetShips();
                            foreach (var ship in shipsRes.data)
                            {
                                if (ship.nav.waypointSymbol == d.destinationSymbol)
                                {
                                    foreach (var item in ship.cargo.inventory)
                                    {
                                        if (item.symbol == d.tradeSymbol)
                                        {
                                            int unitsToDeliver = Mathf.Min(item.units, d.unitsRequired - d.unitsFulfilled);
                                            await _apiService.DeliverContractCargo(c.id, ship.symbol, d.tradeSymbol, unitsToDeliver);
                                            Log.Info("[ContractPresenter] Delivered {Units} {Symbol} to {Waypoint}", unitsToDeliver, d.tradeSymbol, d.destinationSymbol);
                                            break;
                                        }
                                    }
                                }
                            }
                        } catch (System.Exception e) { Log.Error("[ContractPresenter] Delivery failed: {Error}", e.Message); }
                    };
                    delList.Add(deliverBtn);
                }

                fulfillBtn.SetEnabled(c.accepted && !c.fulfilled && allDelivered);
                fulfillBtn.clicked += async () => {
                    try {
                        await _apiService.FulfillContract(c.id);
                        Log.Info("[ContractPresenter] Contract {Id} fulfilled.", c.id);
                    } catch (System.Exception e) { Log.Error("[ContractPresenter] Fulfill failed: {Error}", e.Message); }
                };

                list.Add(entry);
            }
        }
    }
}
