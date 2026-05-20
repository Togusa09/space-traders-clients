using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SpaceTraders.API.Models;
using SpaceTraders.Core;
using UnityEngine;

namespace SpaceTraders.API
{
    public class APIService : MonoBehaviour
    {
        private static APIService _instance;
        public static APIService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<APIService>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("APIService");
                        _instance = go.AddComponent<APIService>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private const long PersistentCacheMaxAge = 24 * 60 * 60; // 24 hours

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task<RegistrationResponse> Register(string symbol, string faction)
        {
            var data = new RegistrationData { symbol = symbol, faction = faction };
            return await SpaceTradersClient.Instance.PostRequest<RegistrationResponse, RegistrationData>("/register", data);
        }

        public async Task<AgentResponse> GetMyAgent()
        {
            return await SpaceTradersClient.Instance.GetRequest<AgentResponse>("/my/agent");
        }

        public async Task<ContractsResponse> GetContracts()
        {
            return await SpaceTradersClient.Instance.GetRequest<ContractsResponse>("/my/contracts");
        }

        public async Task<ShipsResponse> GetShips()
        {
            return await SpaceTradersClient.Instance.GetRequest<ShipsResponse>("/my/ships");
        }

        public async Task<SystemsResponse> GetSystems(int page = 1, int limit = 10)
        {
            string cacheKey = $"systems_p{page}_l{limit}";
            string cachedJson = DatabaseManager.Instance.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Debug.Log($"[APIService] Using cached systems for page {page}");
                    return JsonUtility.FromJson<SystemsResponse>(cachedJson);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[APIService] Failed to parse cached systems: {e.Message}");
                }
            }

            string endpoint = $"/systems?page={page}&limit={limit}";
            Debug.Log($"[APIService] Fetching systems page {page} from network...");
            string json = await SpaceTradersClient.Instance.GetRequestRaw(endpoint);
            
            Debug.Log($"[APIService] Received systems page {page} from network. Caching...");
            DatabaseManager.Instance.SetCache(cacheKey, json);
            return JsonUtility.FromJson<SystemsResponse>(json);
        }

        public async Task<SystemResponse> GetSystem(string systemSymbol)
        {
            string cacheKey = $"system_{systemSymbol}";
            string cachedJson = DatabaseManager.Instance.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Debug.Log($"[APIService] Using cached details for system {systemSymbol}");
                    return JsonUtility.FromJson<SystemResponse>(cachedJson);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[APIService] Failed to parse cached system {systemSymbol}: {e.Message}");
                }
            }

            string endpoint = $"/systems/{systemSymbol}";
            Debug.Log($"[APIService] Fetching system {systemSymbol} details from network...");
            string json = await SpaceTradersClient.Instance.GetRequestRaw(endpoint);
            
            Debug.Log($"[APIService] Received system {systemSymbol} from network. Caching...");
            DatabaseManager.Instance.SetCache(cacheKey, json);
            return JsonUtility.FromJson<SystemResponse>(json);
        }

        public async Task<SystemWaypointsResponse> GetSystemWaypoints(string systemSymbol)
        {
            string cacheKey = $"system_waypoints_{systemSymbol}";
            string cachedJson = DatabaseManager.Instance.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Debug.Log($"[APIService] Using cached waypoints for system {systemSymbol}");
                    return JsonUtility.FromJson<SystemWaypointsResponse>(cachedJson);
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
                    string json = await SpaceTradersClient.Instance.GetRequestRaw(endpoint);
                    var pageRes = JsonUtility.FromJson<SystemWaypointsResponse>(json);
                    
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
                    string fullJson = JsonUtility.ToJson(firstPage);
                    DatabaseManager.Instance.SetCache(cacheKey, fullJson);
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
            return await SpaceTradersClient.Instance.GetRequest<MarketResponse>($"/systems/{systemSymbol}/waypoints/{waypointSymbol}/market");
        }

        public async Task<ShipyardResponse> GetShipyard(string systemSymbol, string waypointSymbol)
        {
            return await SpaceTradersClient.Instance.GetRequest<ShipyardResponse>($"/systems/{systemSymbol}/waypoints/{waypointSymbol}/shipyard");
        }

        public async Task<ConstructionResponse> GetConstruction(string systemSymbol, string waypointSymbol)
        {
            return await SpaceTradersClient.Instance.GetRequest<ConstructionResponse>($"/systems/{systemSymbol}/waypoints/{waypointSymbol}/construction");
        }

        public async Task<JumpGateResponse> GetJumpGate(string systemSymbol, string waypointSymbol)
        {
            return await SpaceTradersClient.Instance.GetRequest<JumpGateResponse>($"/systems/{systemSymbol}/waypoints/{waypointSymbol}/jump-gate");
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
            string cachedJson = DatabaseManager.Instance.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Debug.Log("[APIService] Using cached factions");
                    return JsonUtility.FromJson<FactionsResponse>(cachedJson);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[APIService] Failed to parse cached factions: {e.Message}");
                }
            }

            Debug.Log("[APIService] Fetching factions from network...");
            string json = await SpaceTradersClient.Instance.GetRequestRaw("/factions");
            
            Debug.Log("[APIService] Received factions from network. Caching...");
            DatabaseManager.Instance.SetCache(cacheKey, json);
            return JsonUtility.FromJson<FactionsResponse>(json);
        }

        public async Task<AcceptContractResponse> AcceptContract(string contractId)
        {
            return await SpaceTradersClient.Instance.PostRequest<AcceptContractResponse>($"/my/contracts/{contractId}/accept");
        }

        public async Task<PurchaseShipResponse> PurchaseShip(string shipType, string waypointSymbol)
        {
            var req = new PurchaseShipRequest { shipType = shipType, waypointSymbol = waypointSymbol };
            return await SpaceTradersClient.Instance.PostRequest<PurchaseShipResponse, PurchaseShipRequest>("/my/ships", req);
        }

        public async Task<ShipNavResponse> OrbitShip(string shipSymbol)
        {
            return await SpaceTradersClient.Instance.PostRequest<ShipNavResponse>($"/my/ships/{shipSymbol}/orbit");
        }

        public async Task<ShipNavResponse> DockShip(string shipSymbol)
        {
            return await SpaceTradersClient.Instance.PostRequest<ShipNavResponse>($"/my/ships/{shipSymbol}/dock");
        }

        public async Task<NavigateResponse> NavigateShip(string shipSymbol, string waypointSymbol)
        {
            var req = new NavigateRequest { waypointSymbol = waypointSymbol };
            return await SpaceTradersClient.Instance.PostRequest<NavigateResponse, NavigateRequest>($"/my/ships/{shipSymbol}/navigate", req);
        }

        public async Task<ExtractionResponse> ExtractResources(string shipSymbol)
        {
            return await SpaceTradersClient.Instance.PostRequest<ExtractionResponse>($"/my/ships/{shipSymbol}/extract");
        }

        public async Task<SellCargoResponse> SellCargo(string shipSymbol, string tradeSymbol, int units)
        {
            var req = new SellCargoRequest { symbol = tradeSymbol, units = units };
            return await SpaceTradersClient.Instance.PostRequest<SellCargoResponse, SellCargoRequest>($"/my/ships/{shipSymbol}/sell", req);
        }

        public async Task<RefuelResponse> RefuelShip(string shipSymbol, int units)
        {
            var req = new RefuelRequest { units = units };
            return await SpaceTradersClient.Instance.PostRequest<RefuelResponse, RefuelRequest>($"/my/ships/{shipSymbol}/refuel", req);
        }

        public async Task<RefuelResponse> RefuelShip(string shipSymbol)
        {
            return await SpaceTradersClient.Instance.PostRequest<RefuelResponse>($"/my/ships/{shipSymbol}/refuel");
        }

        public async Task<DeliverContractResponse> DeliverContractCargo(string contractId, string shipSymbol, string tradeSymbol, int units)
        {
            var req = new DeliverContractRequest { shipSymbol = shipSymbol, tradeSymbol = tradeSymbol, units = units };
            return await SpaceTradersClient.Instance.PostRequest<DeliverContractResponse, DeliverContractRequest>($"/my/contracts/{contractId}/deliver", req);
        }

        public async Task<FulfillContractResponse> FulfillContract(string contractId)
        {
            return await SpaceTradersClient.Instance.PostRequest<FulfillContractResponse>($"/my/contracts/{contractId}/fulfill");
        }
    }
}
