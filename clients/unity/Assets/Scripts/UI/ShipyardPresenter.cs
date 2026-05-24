using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.UI
{
    public class ShipyardPresenter : MonoBehaviour
    {
        public VisualTreeAsset shipPurchaseTemplate;

        private APIService _apiService;

        [Inject]
        public void Construct(APIService apiService)
        {
            _apiService = apiService;
        }

        public void Populate(ScrollView list, Shipyard shipyard)
        {
            if (list == null) return;
            list.Clear();

            if (shipyard?.Ships == null || shipyard.Ships.Count == 0)
            {
                list.Add(new Label("No ships available for purchase at this shipyard."));
                return;
            }

            foreach (var ship in shipyard.Ships)
            {
                if (shipPurchaseTemplate == null) continue;
                var entry = shipPurchaseTemplate.Instantiate();

                var nameLabel = entry.Q<Label>("ship-name");
                var typeLabel = entry.Q<Label>("ship-type");
                var priceLabel = entry.Q<Label>("price-label");
                var descriptionLabel = entry.Q<Label>("description-label");
                var buyBtn = entry.Q<Button>("buy-btn");

                if (nameLabel != null) nameLabel.text = ship.Name;
                if (typeLabel != null) typeLabel.text = ship.Type.ToString();
                if (priceLabel != null) priceLabel.text = $"{ship.PurchasePrice:N0} Credits";
                if (descriptionLabel != null) descriptionLabel.text = ship.Description;

                if (buyBtn != null)
                {
                    buyBtn.clicked += async () => {
                        try {
                            buyBtn.SetEnabled(false);
                            var res = await _apiService.PurchaseShip(ship.Type.ToString(), shipyard.Symbol);
                            Log.Info("[ShipyardPresenter] Successfully purchased {Type} for {Price}", ship.Type, res.Data.Transaction.Price);
                            // TODO: Refresh agent credits/fleet if needed or show success popup
                        } catch (Exception e) {
                            Log.Error("[ShipyardPresenter] Purchase failed: {Error}", e.Message);
                        } finally {
                            if (buyBtn != null) buyBtn.SetEnabled(true);
                        }
                    };
                }

                list.Add(entry);
            }
        }
    }
}
