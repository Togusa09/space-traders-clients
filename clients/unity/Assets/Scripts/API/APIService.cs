using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Model;
using SpaceTraders.Core;
using UnityEngine;
using Newtonsoft.Json;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.API
{
    public class APIService : MonoBehaviour
    {
        private const long PersistentCacheMaxAge = 24 * 60 * 60; // 24 hours

        private SpaceTradersClient _client;
        private IApiCacheRepository _cacheRepository;

        [Inject]
        internal void Construct(SpaceTradersClient client, IApiCacheRepository cacheRepository)
        {
            _client = client;
            _cacheRepository = cacheRepository;
        }

        public async Task<Register201Response> Register(string symbol, string faction)
        {
            var data = new RegisterRequest(faction: (FactionSymbol)Enum.Parse(typeof(FactionSymbol), faction), symbol: symbol);
            return await _client.ExecuteAsync(() => _client.Global.RegisterWithHttpInfoAsync(data), "Register");
        }

        public async Task<GetMyAgent200Response> GetMyAgent()
        {
            return await _client.ExecuteAsync(() => _client.Agents.GetMyAgentWithHttpInfoAsync(), "GetMyAgent");
        }

        public async Task<GetContracts200Response> GetContracts(int page = 1, int limit = 10)
        {
            return await _client.ExecuteAsync(() => _client.Contracts.GetContractsWithHttpInfoAsync(page, limit), "GetContracts");
        }

        public async Task<GetMyShips200Response> GetShips(int page = 1, int limit = 10)
        {
            return await _client.ExecuteAsync(() => _client.Fleet.GetMyShipsWithHttpInfoAsync(page, limit), "GetShips");
        }

        public async Task<GetMyShip200Response> GetShip(string shipSymbol)
        {
            return await _client.ExecuteAsync(() => _client.Fleet.GetMyShipWithHttpInfoAsync(shipSymbol), "GetShip");
        }

        public async Task<GetSystems200Response> GetSystems(int page = 1, int limit = 10)
        {
            string cacheKey = $"systems_p{page}_l{limit}";
            string cachedJson = _cacheRepository.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Log.Info("[APIService] Using cached systems for page {Page}", page);
                    return JsonConvert.DeserializeObject<GetSystems200Response>(cachedJson);
                }
                catch (Exception e)
                {
                    Log.Warning("[APIService] Failed to parse cached systems: {Error}", e.Message);
                }
            }

            Log.Info("[APIService] Fetching systems page {Page} from network...", page);
            var response = await _client.ExecuteAsync(() => _client.Systems.GetSystemsWithHttpInfoAsync(page, limit), "GetSystems");
            
            Log.Info("[APIService] Received systems page {Page} from network. Caching...", page);
            string json = JsonConvert.SerializeObject(response);
            _cacheRepository.SetCache(cacheKey, json);
            return response;
        }

        public async Task<GetSystem200Response> GetSystem(string systemSymbol)
        {
            string cacheKey = $"system_{systemSymbol}";
            string cachedJson = _cacheRepository.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Log.Info("[APIService] Using cached details for system {System}", systemSymbol);
                    return JsonConvert.DeserializeObject<GetSystem200Response>(cachedJson);
                }
                catch (Exception e)
                {
                    Log.Warning("[APIService] Failed to parse cached system {System}: {Error}", systemSymbol, e.Message);
                }
            }

            Log.Info("[APIService] Fetching system {System} details from network...", systemSymbol);
            var response = await _client.ExecuteAsync(() => _client.Systems.GetSystemWithHttpInfoAsync(systemSymbol), "GetSystem");
            
            Log.Info("[APIService] Received system {System} from network. Caching...", systemSymbol);
            string json = JsonConvert.SerializeObject(response);
            _cacheRepository.SetCache(cacheKey, json);
            return response;
        }

        public async Task<GetSystemWaypoints200Response> GetSystemWaypoints(string systemSymbol)
        {
            string cacheKey = $"system_waypoints_{systemSymbol}";
            string cachedJson = _cacheRepository.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Log.Info("[APIService] Using cached waypoints for system {System}", systemSymbol);
                    return JsonConvert.DeserializeObject<GetSystemWaypoints200Response>(cachedJson);
                }
                catch (Exception e)
                {
                    Log.Warning("[APIService] Failed to parse cached waypoints for system {System}: {Error}", systemSymbol, e.Message);
                }
            }

            int page = 1;
            int limit = 20;
            var allWaypoints = new List<Waypoint>();
            GetSystemWaypoints200Response firstPage = null;

            try
            {
                while (true)
                {
                    Log.Info("[APIService] Fetching waypoints for system {System} page {Page} from network...", systemSymbol, page);
                    var pageRes = await _client.ExecuteAsync(() => _client.Systems.GetSystemWaypointsWithHttpInfoAsync(systemSymbol, page, limit), "GetSystemWaypoints");
                    
                    if (pageRes != null && pageRes.Data != null)
                    {
                        if (firstPage == null) firstPage = pageRes;
                        allWaypoints.AddRange(pageRes.Data);
                        if (pageRes.Meta != null && allWaypoints.Count < pageRes.Meta.Total && pageRes.Data.Count > 0)
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
                    firstPage.Data = allWaypoints;
                    string fullJson = JsonConvert.SerializeObject(firstPage);
                    _cacheRepository.SetCache(cacheKey, fullJson);
                    return firstPage;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[APIService] Error fetching system waypoints: {Error}", ex.Message);
                throw;
            }

            return null;
        }

        public async Task<GetMarket200Response> GetMarket(string systemSymbol, string waypointSymbol)
        {
            return await _client.ExecuteAsync(() => _client.Systems.GetMarketWithHttpInfoAsync(systemSymbol, waypointSymbol), "GetMarket");
        }

        public async Task<GetShipyard200Response> GetShipyard(string systemSymbol, string waypointSymbol)
        {
            return await _client.ExecuteAsync(() => _client.Systems.GetShipyardWithHttpInfoAsync(systemSymbol, waypointSymbol), "GetShipyard");
        }

        public async Task<GetConstruction200Response> GetConstruction(string systemSymbol, string waypointSymbol)
        {
            return await _client.ExecuteAsync(() => _client.Systems.GetConstructionWithHttpInfoAsync(systemSymbol, waypointSymbol), "GetConstruction");
        }

        public async Task<GetJumpGate200Response> GetJumpGate(string systemSymbol, string waypointSymbol)
        {
            return await _client.ExecuteAsync(() => _client.Systems.GetJumpGateWithHttpInfoAsync(systemSymbol, waypointSymbol), "GetJumpGate");
        }

        public async Task<GetFactions200Response> GetFactions(int page = 1, int limit = 10)
        {
            string cacheKey = $"factions_p{page}_l{limit}";
            string cachedJson = _cacheRepository.GetCache(cacheKey, PersistentCacheMaxAge);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    Log.Info("[APIService] Using cached factions");
                    return JsonConvert.DeserializeObject<GetFactions200Response>(cachedJson);
                }
                catch (Exception e)
                {
                    Log.Warning("[APIService] Failed to parse cached factions: {Error}", e.Message);
                }
            }

            Log.Info("[APIService] Fetching factions from network...");
            var response = await _client.ExecuteAsync(() => _client.Factions.GetFactionsWithHttpInfoAsync(page, limit), "GetFactions");
            
            Log.Info("[APIService] Received factions from network. Caching...");
            string json = JsonConvert.SerializeObject(response);
            _cacheRepository.SetCache(cacheKey, json);
            return response;
        }

        public async Task<AcceptContract200Response> AcceptContract(string contractId)
        {
            return await _client.ExecuteAsync(() => _client.Contracts.AcceptContractWithHttpInfoAsync(contractId), "AcceptContract");
        }

        public async Task<PurchaseShip201Response> PurchaseShip(string shipType, string waypointSymbol)
        {
            var req = new PurchaseShipRequest((ShipType)Enum.Parse(typeof(ShipType), shipType), waypointSymbol);
            return await _client.ExecuteAsync(() => _client.Fleet.PurchaseShipWithHttpInfoAsync(req), "PurchaseShip");
        }

        public async Task<OrbitShip200Response> OrbitShip(string shipSymbol)
        {
            return await _client.ExecuteAsync(() => _client.Fleet.OrbitShipWithHttpInfoAsync(shipSymbol), "OrbitShip");
        }

        public async Task<DockShip200Response> DockShip(string shipSymbol)
        {
            return await _client.ExecuteAsync(() => _client.Fleet.DockShipWithHttpInfoAsync(shipSymbol), "DockShip");
        }

        public async Task<NavigateShip200Response> NavigateShip(string shipSymbol, string waypointSymbol)
        {
            var req = new NavigateShipRequest(waypointSymbol);
            return await _client.ExecuteAsync(() => _client.Fleet.NavigateShipWithHttpInfoAsync(shipSymbol, req), "NavigateShip");
        }

        public async Task<ExtractResources201Response> ExtractResources(string shipSymbol)
        {
            return await _client.ExecuteAsync(() => _client.Fleet.ExtractResourcesWithHttpInfoAsync(shipSymbol), "ExtractResources");
        }

        public async Task<SellCargo201Response> SellCargo(string shipSymbol, string tradeSymbol, int units)
        {
            var req = new SellCargoRequest(symbol: (TradeSymbol)Enum.Parse(typeof(TradeSymbol), tradeSymbol), units: units);
            return await _client.ExecuteAsync(() => _client.Fleet.SellCargoWithHttpInfoAsync(shipSymbol, req), "SellCargo");
        }

        public async Task<RefuelShip200Response> RefuelShip(string shipSymbol, int units = 0)
        {
            var req = new RefuelShipRequest(units: units);
            return await _client.ExecuteAsync(() => _client.Fleet.RefuelShipWithHttpInfoAsync(shipSymbol, req), "RefuelShip");
        }

        public async Task<DeliverContract200Response> DeliverContractCargo(string contractId, string shipSymbol, string tradeSymbol, int units)
        {
            var req = new DeliverContractRequest(shipSymbol, tradeSymbol, units);
            return await _client.ExecuteAsync(() => _client.Contracts.DeliverContractWithHttpInfoAsync(contractId, req), "DeliverContractCargo");
        }

        public async Task<FulfillContract200Response> FulfillContract(string contractId)
        {
            return await _client.ExecuteAsync(() => _client.Contracts.FulfillContractWithHttpInfoAsync(contractId), "FulfillContract");
        }
    }
}
