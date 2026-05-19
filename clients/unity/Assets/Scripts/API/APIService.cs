using System;
using System.Threading.Tasks;
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
    }
}
