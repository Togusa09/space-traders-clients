using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;
using UnityEngine;

namespace SpaceTraders.UI.Map
{
    internal static class MapWaypointServiceFacade
    {
        public static bool HasWaypointTrait(Waypoint waypoint, WaypointTraitSymbol trait)
        {
            return waypoint?.Traits != null && waypoint.Traits.Any(t => t.Symbol == trait);
        }

        public static async Task<GetMarket200Response> TryGetMarketAsync(APIService apiService, string systemSymbol, string waypointSymbol)
        {
            try
            {
                return await apiService.GetMarket(systemSymbol, waypointSymbol);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<GetShipyard200Response> TryGetShipyardAsync(APIService apiService, string systemSymbol, string waypointSymbol)
        {
            try
            {
                return await apiService.GetShipyard(systemSymbol, waypointSymbol);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<GetConstruction200Response> TryGetConstructionAsync(APIService apiService, string systemSymbol, string waypointSymbol)
        {
            try
            {
                return await apiService.GetConstruction(systemSymbol, waypointSymbol);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<List<Ship>> FetchShipsInSystemAsync(APIService apiService, string systemSymbol)
        {
            const int shipPageSize = 20;
            var results = new List<Ship>();
            int page = 1;

            while (true)
            {
                var response = await apiService.GetShips(page: page, limit: shipPageSize);
                var pageShips = response?.Data;
                if (pageShips == null || pageShips.Count == 0)
                {
                    break;
                }

                results.AddRange(pageShips.Where(ship =>
                    string.Equals(ship?.Nav?.SystemSymbol, systemSymbol, StringComparison.OrdinalIgnoreCase)));

                if (pageShips.Count < shipPageSize)
                {
                    break;
                }

                page++;
            }

            return results
                .Where(ship => ship != null)
                .OrderBy(ship => ship.Symbol)
                .ToList();
        }

        public static int EstimateFuelRequired(
            string fromWaypointSymbol,
            string toWaypointSymbol,
            IReadOnlyList<SystemWaypoint> systemWaypoints,
            IReadOnlyList<Waypoint> detailedWaypoints)
        {
            if (!TryGetWaypointPosition(fromWaypointSymbol, systemWaypoints, detailedWaypoints, out var fromPos) ||
                !TryGetWaypointPosition(toWaypointSymbol, systemWaypoints, detailedWaypoints, out var toPos))
            {
                return 0;
            }

            return Mathf.CeilToInt(Vector2.Distance(fromPos, toPos));
        }

        private static bool TryGetWaypointPosition(
            string waypointSymbol,
            IReadOnlyList<SystemWaypoint> systemWaypoints,
            IReadOnlyList<Waypoint> detailedWaypoints,
            out Vector2 position)
        {
            position = Vector2.zero;
            if (string.IsNullOrWhiteSpace(waypointSymbol))
            {
                return false;
            }

            var systemWaypoint = systemWaypoints?.FirstOrDefault(wp => wp.Symbol == waypointSymbol);
            if (systemWaypoint != null)
            {
                position = new Vector2(systemWaypoint.X, systemWaypoint.Y);
                return true;
            }

            var detailedWaypoint = MapWaypointDetailLookup.FindBySymbol(detailedWaypoints, waypointSymbol);
            if (detailedWaypoint != null)
            {
                position = new Vector2(detailedWaypoint.X, detailedWaypoint.Y);
                return true;
            }

            return false;
        }
    }
}
