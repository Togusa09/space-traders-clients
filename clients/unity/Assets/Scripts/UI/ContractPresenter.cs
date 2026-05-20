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
    public class ContractPresenter
    {
        private readonly DashboardController _controller;

        public ContractPresenter(DashboardController controller)
        {
            _controller = controller;
        }

        public VisualElement BindContract(Contract c, VisualTreeAsset contractTemplate)
        {
            var element = contractTemplate.Instantiate();
            var root = element.Q<VisualElement>(null, "dashboard-entry");
            if (root == null) root = element.Q<VisualElement>();

            element.Q<Label>("id-label").text = $"ID: {c.id}";
            element.Q<Label>("type-label").text = $"Type: {c.type} | Faction: {c.factionSymbol}";
            element.Q<Label>("status-label").text = $"Accepted: {(c.accepted ? "Yes" : "No")} | Fulfilled: {(c.fulfilled ? "Yes" : "No")}";

            // Detailed Panel
            var detailsContainer = new VisualElement();
            detailsContainer.AddToClassList("ship-details-container");
            root.Add(detailsContainer);

            // Payments
            detailsContainer.Add(new Label($"Payment: Upfront: {c.terms.payment.onAccepted:N0} C | Completion: {c.terms.payment.onFulfilled:N0} C") { style = { fontSize = 11, color = Color.gray } });
            detailsContainer.Add(new Label($"Deadline: {c.terms.deadline}") { style = { fontSize = 11, color = Color.gray, marginBottom = 5 } });

            if (!c.accepted)
            {
                var acceptBtn = new Button(async () => {
                    _controller.SetStatusText("Accepting contract...");
                    try
                    {
                        await APIService.Instance.AcceptContract(c.id);
                        _controller.ShowPopupMessage("Contract Accepted", $"Contract accepted successfully!\nCredits Received upfront: {c.terms.payment.onAccepted:N0} C", Color.green);
                        _controller.TriggerTabSwitch(DashboardController.Tab.Contracts);
                    }
                    catch (Exception ex)
                    {
                        _controller.ShowPopupMessage("Accept Failed", $"Failed to accept contract:\n{ex.Message}", Color.red);
                    }
                }) { text = "ACCEPT CONTRACT" };
                acceptBtn.AddToClassList("button");
                acceptBtn.AddToClassList("btn-small");
                acceptBtn.AddToClassList("btn-green");
                acceptBtn.style.width = 150; acceptBtn.style.height = 25;
                detailsContainer.Add(acceptBtn);
            }
            else if (!c.fulfilled)
            {
                detailsContainer.Add(new Label("Deliverables:") { style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 5 } });
                bool allFulfilled = true;

                foreach (var d in c.terms.deliver)
                {
                    int remaining = d.unitsRequired - d.unitsFulfilled;
                    if (remaining > 0) allFulfilled = false;

                    var delivRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginTop = 2, marginBottom = 2 } };
                    delivRow.Add(new Label($"• {d.tradeSymbol} to {d.destinationSymbol}: {d.unitsFulfilled}/{d.unitsRequired} units") { style = { fontSize = 11 } });
                    detailsContainer.Add(delivRow);

                    if (remaining > 0)
                    {
                        // Check if any ship is docked at d.destinationSymbol and has d.tradeSymbol
                        foreach (var ship in _controller.PlayerShips)
                        {
                            if (ship.nav.status == "DOCKED" && ship.nav.waypointSymbol == d.destinationSymbol)
                            {
                                var cargoItem = ship.cargo.inventory?.FirstOrDefault(i => i.symbol == d.tradeSymbol);
                                if (cargoItem != null && cargoItem.units > 0)
                                {
                                    int unitsToDeliver = Math.Min(cargoItem.units, remaining);
                                    var deliverBtn = new Button(async () => {
                                        _controller.SetStatusText("Delivering cargo...");
                                        try
                                        {
                                            await APIService.Instance.DeliverContractCargo(c.id, ship.symbol, d.tradeSymbol, unitsToDeliver);
                                            _controller.ShowPopupMessage("Cargo Delivered", $"Successfully delivered {unitsToDeliver} units of {d.tradeSymbol} from ship {ship.symbol}!", Color.green);
                                            _controller.TriggerTabSwitch(DashboardController.Tab.Contracts);
                                        }
                                        catch (Exception ex)
                                        {
                                            _controller.ShowPopupMessage("Delivery Failed", $"Failed to deliver cargo:\n{ex.Message}", Color.red);
                                        }
                                    }) { text = $"DELIVER FROM {ship.symbol} ({unitsToDeliver})" };
                                    deliverBtn.AddToClassList("button");
                                    deliverBtn.AddToClassList("btn-small");
                                    deliverBtn.AddToClassList("btn-orange");
                                    deliverBtn.style.width = 180; deliverBtn.style.height = 20; deliverBtn.style.fontSize = 9;
                                    detailsContainer.Add(deliverBtn);
                                }
                            }
                        }
                    }
                }

                if (allFulfilled)
                {
                    var fulfillBtn = new Button(async () => {
                        _controller.SetStatusText("Fulfilling contract...");
                        try
                        {
                            await APIService.Instance.FulfillContract(c.id);
                            _controller.ShowPopupMessage("Contract Fulfilled", $"Contract fulfilled successfully!\nCredits Received on fulfillment: {c.terms.payment.onFulfilled:N0} C", Color.green);
                            _controller.TriggerTabSwitch(DashboardController.Tab.Contracts);
                        }
                        catch (Exception ex)
                        {
                            _controller.ShowPopupMessage("Fulfillment Failed", $"Failed to fulfill contract:\n{ex.Message}", Color.red);
                        }
                    }) { text = "FULFILL CONTRACT" };
                    fulfillBtn.AddToClassList("button");
                    fulfillBtn.AddToClassList("btn-small");
                    fulfillBtn.AddToClassList("btn-blue");
                    fulfillBtn.style.width = 150; fulfillBtn.style.height = 25;
                    detailsContainer.Add(fulfillBtn);
                }
            }
            else
            {
                detailsContainer.Add(new Label("Contract Completed") { style = { color = Color.green, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
            }

            return element;
        }
    }
}
