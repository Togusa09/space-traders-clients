using UnityEngine;
using UnityEngine.UIElements;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;
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
            if (list == null) return;
            list.Clear();

            foreach (var c in contracts)
            {
                if (contractEntryTemplate == null) continue;
                var entry = contractEntryTemplate.Instantiate();
                
                // Mapping to Entry_Contract.uxml names (kebab-case)
                var idLabel = entry.Q<Label>("id-label");
                var typeLabel = entry.Q<Label>("type-label");
                var statusLabel = entry.Q<Label>("status-label");

                if (idLabel != null) idLabel.text = $"ID: {c.Id}";
                if (typeLabel != null) typeLabel.text = $"Type: {c.Type} | Faction: {c.FactionSymbol}";
                if (statusLabel != null)
                {
                    statusLabel.text = $"Accepted: {(c.Accepted ? "YES" : "NO")} | Fulfilled: {(c.Fulfilled ? "YES" : "NO")}";
                    statusLabel.style.color = c.Fulfilled ? Color.green : (c.Accepted ? Color.cyan : Color.yellow);
                }

                // Note: Entry_Contract.uxml seems missing Accept/Fulfill buttons and Deliverables list
                // I will add them dynamically if they are missing from the template
                var actions = entry.Q<VisualElement>("actions-container") ?? entry.ElementAt(0); // Fallback to root element
                
                if (entry.Q<Button>("btn-accept") == null)
                {
                    var acceptBtn = new Button { name = "btn-accept", text = "ACCEPT" };
                    acceptBtn.SetEnabled(!c.Accepted);
                    acceptBtn.clicked += async () => {
                        try {
                            await _apiService.AcceptContract(c.Id);
                            Log.Info("[ContractPresenter] Contract {Id} accepted.", c.Id);
                        } catch (System.Exception e) { Log.Error("[ContractPresenter] Accept failed: {Error}", e.Message); }
                    };
                    actions.Add(acceptBtn);
                }

                if (entry.Q<Button>("btn-fulfill") == null)
                {
                    var fulfillBtn = new Button { name = "btn-fulfill", text = "FULFILL" };
                    fulfillBtn.SetEnabled(c.Accepted && !c.Fulfilled);
                    
                    // Fulfill check simplified here
                    fulfillBtn.clicked += async () => {
                        try {
                            await _apiService.FulfillContract(c.Id);
                            Log.Info("[ContractPresenter] Contract {Id} fulfilled.", c.Id);
                        } catch (System.Exception e) { Log.Error("[ContractPresenter] Fulfill failed: {Error}", e.Message); }
                    };
                    actions.Add(fulfillBtn);
                }

                list.Add(entry);
            }
        }
    }
}
