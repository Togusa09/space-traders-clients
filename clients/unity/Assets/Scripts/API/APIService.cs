using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SpaceTraders.API.Models;
using SpaceTraders.Core;
using UnityEngine;
using Newtonsoft.Json;
using VContainer;

namespace SpaceTraders.API
{
    public class APIService : MonoBehaviour
    {
        private const long PersistentCacheMaxAge = 24 * 60 * 60; // 24 hours

        private SpaceTradersClient _client;
        private DatabaseManager _dbManager;

        [Inject]
        public void Construct(SpaceTradersClient client, DatabaseManager dbManager)
        {
            _client = client;
            _dbManager = dbManager;
        }

        public async Task<RegistrationResponse> Register(string symbol, string faction)
        {
            var data = new RegistrationData { symbol = symbol, faction = faction };
            return await _client.PostRequest<RegistrationResponse, RegistrationData>("/register", data);
        }

        public async Task<AgentResponse> GetMyAgent()
        {
            return await _client.GetRequest<AgentResponse>("/my/agent");
        }

        public async Task<ContractsResponse> GetContracts()
        {
            return await _client.GetRequest<ContractsResponse>("/my/contracts");
        }

        public async Task<ShipsResponse> GetShips()
        {
            return await _client.GetRequest<ShipsResponse>("/my/ships");
        }

        public async Task<SystemsResponse> GetSystems(int page = 1, int limit = 10)
        {
            string cacheKey = $"systems_p{page}_l{limit}";
            string cachedJson = _dbManager.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Debug.Log($"[APIService] Using cached systems for page {page}");
                    return JsonConvert.DeserializeObject<SystemsResponse>(cachedJson);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[APIService] Failed to parse cached systems: {e.Message}");
                }
            }

            string endpoint = $"/systems?page={page}&limit={limit}";
            Debug.Log($"[APIService] Fetching systems page {page} from network...");
            string json = await _client.GetRequestRaw(endpoint);
            
            Debug.Log($"[APIService] Received systems page {page} from network. Caching...");
            _dbManager.SetCache(cacheKey, json);
            return JsonConvert.DeserializeObject<SystemsResponse>(json);
        }

        public async Task<SystemResponse> GetSystem(string systemSymbol)
        {
            string cacheKey = $"system_{systemSymbol}";
            string cachedJson = _dbManager.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Debug.Log($"[APIService] Using cached details for system {systemSymbol}");
                    return JsonConvert.DeserializeObject<SystemResponse>(cachedJson);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[APIService] Failed to parse cached system {systemSymbol}: {e.Message}");
                }
            }

            string endpoint = $"/systems/{systemSymbol}";
            Debug.Log($"[APIService] Fetching system {systemSymbol} details from network...");
            string json = await _client.GetRequestRaw(endpoint);
            
            Debug.Log($"[APIService] Received system {systemSymbol} from network. Caching...");
            _dbManager.SetCache(cacheKey, json);
            return JsonConvert.DeserializeObject<SystemResponse>(json);
        }

        public async Task<SystemWaypointsResponse> GetSystemWaypoints(string systemSymbol)
        {
            string cacheKey = $"system_waypoints_{systemSymbol}";
            string cachedJson = _dbManager.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Debug.Log($"[APIService] Using cached waypoints for system {systemSymbol}");
                    return JsonConvert.DeserializeObject<SystemWaypointsResponse>(cachedJson);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[APIService] Failed to parse cached waypoints for system {systemSymbol}: {e.Message}");
                }
            }

            int page = 1;
            int limit = 20;
            var allWaypoints = new List<SystemWaypoint>();
            SystemWaypointsResponse firstPage = null;

            try
            {
                while (true)
                {
                    string endpoint = $"/systems/{systemSymbol}/waypoints?page={page}&limit={limit}";
                    Debug.Log($"[APIService] Fetching waypoints for system {systemSymbol} page {page} from network...");
                    string json = await _client.GetRequestRaw(endpoint);
                    var pageRes = JsonConvert.DeserializeObject<SystemWaypointsResponse>(json);
                    
                    if (pageRes != null && pageRes.data != null)
                    {
                        if (firstPage == null) firstPage = pageRes;
                        allWaypoints.AddRange(pageRes.data);
                        if (pageRes.meta != null && allWaypoints.Count < pageRes.meta.total && pageRes.data.Length > 0)
                        {
                            page++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (firstPage != null)
                {
                    firstPage.data = allWaypoints.ToArray();
                    string fullJson = JsonConvert.SerializeObject(firstPage);
                    _dbManager.SetCache(cacheKey, fullJson);
                    return firstPage;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIService] Error fetching system waypoints: {ex.Message}");
                throw;
            }

            return null;
        }

        public async Task<MarketResponse> GetMarket(string systemSymbol, string waypointSymbol)
        {
            return await _client.GetRequest<MarketResponse>($"/systems/{systemSymbol}/waypoints/{waypointSymbol}/market");
        }

        public async Task<ShipyardResponse> GetShipyard(string systemSymbol, string waypointSymbol)
        {
            return await _client.GetRequest<ShipyardResponse>($"/systems/{systemSymbol}/waypoints/{waypointSymbol}/shipyard");
        }

        public async Task<ConstructionResponse> GetConstruction(string systemSymbol, string waypointSymbol)
        {
            return await _client.GetRequest<ConstructionResponse>($"/systems/{systemSymbol}/waypoints/{waypointSymbol}/construction");
        }

        public async Task<JumpGateResponse> GetJumpGate(string systemSymbol, string waypointSymbol)
        {
            return await _client.GetRequest<JumpGateResponse>($"/systems/{systemSymbol}/waypoints/{waypointSymbol}/jump-gate");
        }

        [Serializable]
        public class JumpGate
        {
            public string symbol;
            public string[] connections;
        }

        [Serializable]
        public class JumpGateResponse
        {
            public JumpGate data;
        }

        public async Task<FactionsResponse> GetFactions()
        {
            string cacheKey = "factions_list";
            string cachedJson = _dbManager.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Debug.Log("[APIService] Using cached factions");
                    return JsonConvert.DeserializeObject<FactionsResponse>(cachedJson);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[APIService] Failed to parse cached factions: {e.Message}");
                }
            }

            Debug.Log("[APIService] Fetching factions from network...");
            string json = await _client.GetRequestRaw("/factions");
            
            Debug.Log("[APIService] Received factions from network. Caching...");
            _dbManager.SetCache(cacheKey, json);
            return JsonConvert.DeserializeObject<FactionsResponse>(json);
        }

        public async Task<AcceptContractResponse> AcceptContract(string contractId)
        {
            return await _client.PostRequest<AcceptContractResponse>($"/my/contracts/{contractId}/accept");
        }

        public async Task<PurchaseShipResponse> PurchaseShip(string shipType, string waypointSymbol)
        {
            var req = new PurchaseShipRequest { shipType = shipType, waypointSymbol = waypointSymbol };
            return await _client.PostRequest<PurchaseShipResponse, PurchaseShipRequest>("/my/ships", req);
        }

        public async Task<ShipNavResponse> OrbitShip(string shipSymbol)
        {
            return await _client.PostRequest<ShipNavResponse>($"/my/ships/{shipSymbol}/orbit");
        }

        public async Task<ShipNavResponse> DockShip(string shipSymbol)
        {
            return await _client.PostRequest<ShipNavResponse>($"/my/ships/{shipSymbol}/dock");
        }

        public async Task<NavigateResponse> NavigateShip(string shipSymbol, string waypointSymbol)
        {
            var req = new NavigateRequest { waypointSymbol = waypointSymbol };
            return await _client.PostRequest<NavigateResponse, NavigateRequest>($"/my/ships/{shipSymbol}/navigate", req);
        }

        public async Task<ExtractionResponse> ExtractResources(string shipSymbol)
        {
            return await _client.PostRequest<ExtractionResponse>($"/my/ships/{shipSymbol}/extract");
        }

        public async Task<SellCargoResponse> SellCargo(string shipSymbol, string tradeSymbol, int units)
        {
            var req = new SellCargoRequest { symbol = tradeSymbol, units = units };
            return await _client.PostRequest<SellCargoResponse, SellCargoRequest>($"/my/ships/{shipSymbol}/sell", req);
        }

        public async Task<RefuelResponse> RefuelShip(string shipSymbol, int units)
        {
            var req = new RefuelRequest { units = units };
            return await _client.PostRequest<RefuelResponse, RefuelRequest>($"/my/ships/{shipSymbol}/refuel", req);
        }

        public async Task<RefuelResponse> RefuelShip(string shipSymbol)
        {
            return await _client.PostRequest<RefuelResponse>($"/my/ships/{shipSymbol}/refuel");
        }

        public async Task<DeliverContractResponse> DeliverContractCargo(string contractId, string shipSymbol, string tradeSymbol, int units)
        {
            var req = new DeliverContractRequest { shipSymbol = shipSymbol, tradeSymbol = tradeSymbol, units = units };
            return await _client.PostRequest<DeliverContractResponse, DeliverContractRequest>($"/my/contracts/{contractId}/deliver", req);
        }

        public async Task<FulfillContractResponse> FulfillContract(string contractId)
        {
            return await _client.PostRequest<FulfillContractResponse>($"/my/contracts/{contractId}/fulfill");
        }
    }
}
