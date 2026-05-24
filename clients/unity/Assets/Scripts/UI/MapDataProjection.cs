using System.Collections.Generic;
using System.Linq;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;

namespace SpaceTraders.UI
{
    internal static class MapDataProjection
    {
        public static List<IndexedSystem> ToIndexedSystems(IEnumerable<SpaceTraders.Generated.Model.System> systems)
        {
            if (systems == null) return new List<IndexedSystem>();

            return systems
                .Select(system => new IndexedSystem
                {
                    Symbol = system.Symbol,
                    SectorSymbol = system.SectorSymbol,
                    Type = system.Type.ToString(),
                    X = system.X,
                    Y = system.Y,
                    WaypointCount = system.Waypoints?.Count ?? 0
                })
                .ToList();
        }

        public static List<SystemWaypoint> ToSystemWaypoints(IEnumerable<Waypoint> detailedWaypoints)
        {
            if (detailedWaypoints == null) return new List<SystemWaypoint>();

            return detailedWaypoints
                .Select(waypoint => new SystemWaypoint(
                    symbol: waypoint.Symbol,
                    type: waypoint.Type,
                    x: waypoint.X,
                    y: waypoint.Y,
                    orbitals: waypoint.Orbitals ?? new List<WaypointOrbital>(),
                    orbits: waypoint.Orbits))
                .ToList();
        }

        public static string ExtractKnownFacilitiesCsv(IEnumerable<Waypoint> waypoints)
        {
            if (waypoints == null) return string.Empty;

            bool hasMarketplace = false;
            bool hasShipyard = false;
            bool hasConstruction = false;

            foreach (var waypoint in waypoints)
            {
                if (waypoint?.Traits == null) continue;

                foreach (var trait in waypoint.Traits)
                {
                    if (trait == null) continue;

                    if (trait.Symbol == WaypointTraitSymbol.MARKETPLACE) hasMarketplace = true;
                    else if (trait.Symbol == WaypointTraitSymbol.SHIPYARD) hasShipyard = true;
                    else if (trait.Symbol == WaypointTraitSymbol.UNDERCONSTRUCTION) hasConstruction = true;
                }
            }

            var facilities = new List<string>();
            if (hasMarketplace) facilities.Add("MARKETPLACE");
            if (hasShipyard) facilities.Add("SHIPYARD");
            if (hasConstruction) facilities.Add("CONSTRUCTION");

            return string.Join(",", facilities);
        }
    }
}
