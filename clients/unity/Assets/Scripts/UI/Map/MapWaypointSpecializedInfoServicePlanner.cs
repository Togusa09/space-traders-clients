using System.Threading.Tasks;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;

namespace SpaceTraders.UI.Map
{
    internal readonly struct MapWaypointServiceRequestPlan
    {
        public MapWaypointServiceRequestPlan(bool loadMarket, bool loadShipyard, bool loadConstruction)
        {
            LoadMarket = loadMarket;
            LoadShipyard = loadShipyard;
            LoadConstruction = loadConstruction;
        }

        public bool LoadMarket { get; }
        public bool LoadShipyard { get; }
        public bool LoadConstruction { get; }
    }

    internal sealed class MapWaypointServiceSnapshot
    {
        public GetMarket200Response MarketResponse { get; set; }
        public GetShipyard200Response ShipyardResponse { get; set; }
        public GetConstruction200Response ConstructionResponse { get; set; }
    }

    internal static class MapWaypointSpecializedInfoServicePlanner
    {
        public static MapWaypointServiceRequestPlan CreatePlan(Waypoint detailedWaypoint)
        {
            return new MapWaypointServiceRequestPlan(
                MapWaypointServiceFacade.HasWaypointTrait(detailedWaypoint, WaypointTraitSymbol.MARKETPLACE),
                MapWaypointServiceFacade.HasWaypointTrait(detailedWaypoint, WaypointTraitSymbol.SHIPYARD),
                MapWaypointServiceFacade.HasWaypointTrait(detailedWaypoint, WaypointTraitSymbol.UNDERCONSTRUCTION));
        }

        public static async Task<MapWaypointServiceSnapshot> LoadAsync(
            APIService apiService,
            string systemSymbol,
            string waypointSymbol,
            MapWaypointServiceRequestPlan plan)
        {
            var marketTask = plan.LoadMarket
                ? MapWaypointServiceFacade.TryGetMarketAsync(apiService, systemSymbol, waypointSymbol)
                : Task.FromResult<GetMarket200Response>(null);
            var shipyardTask = plan.LoadShipyard
                ? MapWaypointServiceFacade.TryGetShipyardAsync(apiService, systemSymbol, waypointSymbol)
                : Task.FromResult<GetShipyard200Response>(null);
            var constructionTask = plan.LoadConstruction
                ? MapWaypointServiceFacade.TryGetConstructionAsync(apiService, systemSymbol, waypointSymbol)
                : Task.FromResult<GetConstruction200Response>(null);

            await Task.WhenAll(marketTask, shipyardTask, constructionTask);

            return new MapWaypointServiceSnapshot
            {
                MarketResponse = marketTask.Result,
                ShipyardResponse = shipyardTask.Result,
                ConstructionResponse = constructionTask.Result
            };
        }
    }
}
