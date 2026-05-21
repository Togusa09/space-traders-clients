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
        private DatabaseManager _dbManager;

        [Inject]
        public void Construct(SpaceTradersClient client, DatabaseManager dbManager)
        {
            _client = client;
            _dbManager = dbManager;
        }

        public async Task<Register201Response> Register(string symbol, string faction)
        {
            var data = new RegisterRequest(faction: (FactionSymbol)Enum.Parse(typeof(FactionSymbol), faction), symbol: symbol);
            return await _client.Global.RegisterAsync(data);
        }

        public async Task<GetMyAgent200Response> GetMyAgent()
        {
            return await _client.Agents.GetMyAgentAsync();
        }

        public async Task<GetContracts200Response> GetContracts(int page = 1, int limit = 10)
        {
            return await _client.Contracts.GetContractsAsync(page, limit);
        }

        public async Task<GetMyShips200Response> GetShips(int page = 1, int limit = 10)
        {
            return await _client.Fleet.GetMyShipsAsync(page, limit);
        }

        public async Task<GetSystems200Response> GetSystems(int page = 1, int limit = 10)
        {
            string cacheKey = $"systems_p{page}_l{limit}";
            string cachedJson = _dbManager.GetCache(cacheKey, PersistentCacheMaxAge);

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
            var response = await _client.Systems.GetSystemsAsync(page, limit);
            
            Log.Info("[APIService] Received systems page {Page} from network. Caching...", page);
            string json = JsonConvert.SerializeObject(response);
            _dbManager.SetCache(cacheKey, json);
            return response;
        }

        public async Task<GetSystem200Response> GetSystem(string systemSymbol)
        {
            string cacheKey = $"system_{systemSymbol}";
            string cachedJson = _dbManager.GetCache(cacheKey, PersistentCacheMaxAge);

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
            var response = await _client.Systems.GetSystemAsync(systemSymbol);
            
            Log.Info("[APIService] Received system {System} from network. Caching...", systemSymbol);
            string json = JsonConvert.SerializeObject(response);
            _dbManager.SetCache(cacheKey, json);
            return response;
        }

        public async Task<GetSystemWaypoints200Response> GetSystemWaypoints(string systemSymbol)
        {
            string cacheKey = $"system_waypoints_{systemSymbol}";
            string cachedJson = _dbManager.GetCache(cacheKey, PersistentCacheMaxAge);

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
                    var pageRes = await _client.Systems.GetSystemWaypointsAsync(systemSymbol, page, limit);
                    
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
                    _dbManager.SetCache(cacheKey, fullJson);
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
            return await _client.Systems.GetMarketAsync(systemSymbol, waypointSymbol);
        }

        public async Task<GetShipyard200Response> GetShipyard(string systemSymbol, string waypointSymbol)
        {
            return await _client.Systems.GetShipyardAsync(systemSymbol, waypointSymbol);
        }

        public async Task<GetConstruction200Response> GetConstruction(string systemSymbol, string waypointSymbol)
        {
            return await _client.Systems.GetConstructionAsync(systemSymbol, waypointSymbol);
        }

        public async Task<GetJumpGate200Response> GetJumpGate(string systemSymbol, string waypointSymbol)
        {
            return await _client.Systems.GetJumpGateAsync(systemSymbol, waypointSymbol);
        }

        public async Task<GetFactions200Response> GetFactions(int page = 1, int limit = 10)
        {
            string cacheKey = $"factions_p{page}_l{limit}";
            string cachedJson = _dbManager.GetCache(cacheKey, PersistentCacheMaxAge);

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
            var response = await _client.Factions.GetFactionsAsync(page, limit);
            
            Log.Info("[APIService] Received factions from network. Caching...", page);
            string json = JsonConvert.SerializeObject(response);
            _dbManager.SetCache(cacheKey, json);
            return response;
        }

        public async Task<AcceptContract200Response> AcceptContract(string contractId)
        {
            return await _client.Contracts.AcceptContractAsync(contractId);
        }

        public async Task<PurchaseShip201Response> PurchaseShip(string shipType, string waypointSymbol)
        {
            var req = new PurchaseShipRequest((ShipType)Enum.Parse(typeof(ShipType), shipType), waypointSymbol);
            return await _client.Fleet.PurchaseShipAsync(req);
        }

        public async Task<OrbitShip200Response> OrbitShip(string shipSymbol)
        {
            return await _client.Fleet.OrbitShipAsync(shipSymbol);
        }

        public async Task<DockShip200Response> DockShip(string shipSymbol)
        {
            return await _client.Fleet.DockShipAsync(shipSymbol);
        }

        public async Task<NavigateShip200Response> NavigateShip(string shipSymbol, string waypointSymbol)
        {
            var req = new NavigateShipRequest(waypointSymbol);
            return await _client.Fleet.NavigateShipAsync(shipSymbol, req);
        }

        public async Task<ExtractResources201Response> ExtractResources(string shipSymbol)
        {
            return await _client.Fleet.ExtractResourcesAsync(shipSymbol);
        }

        public async Task<SellCargo201Response> SellCargo(string shipSymbol, string tradeSymbol, int units)
        {
            // SellCargoRequest(TradeSymbol symbol = default, int units = default)
            var req = new SellCargoRequest(symbol: (TradeSymbol)Enum.Parse(typeof(TradeSymbol), tradeSymbol), units: units);
            return await _client.Fleet.SellCargoAsync(shipSymbol, req);
        }

        public async Task<RefuelShip200Response> RefuelShip(string shipSymbol, int units = 0)
        {
            var req = new RefuelShipRequest(units: units);
            return await _client.Fleet.RefuelShipAsync(shipSymbol, req);
        }

        public async Task<DeliverContract200Response> DeliverContractCargo(string contractId, string shipSymbol, string tradeSymbol, int units)
        {
            var req = new DeliverContractRequest(shipSymbol, tradeSymbol, units);
            return await _client.Contracts.DeliverContractAsync(contractId, req);
        }

        public async Task<FulfillContract200Response> FulfillContract(string contractId)
        {
            return await _client.Contracts.FulfillContractAsync(contractId);
        }
    }
}
