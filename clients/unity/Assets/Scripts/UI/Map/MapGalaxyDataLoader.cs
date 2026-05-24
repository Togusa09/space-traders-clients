using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceTraders.API;
using SpaceTraders.Core;

namespace SpaceTraders.UI.Map
{
    internal static class MapGalaxyDataLoader
    {
        public static async Task<List<IndexedSystem>> LoadIndexedSystemsAsync(APIService apiService, int pageSize = 100)
        {
            var loaded = new List<IndexedSystem>();
            int page = 1;

            while (true)
            {
                var response = await apiService.GetSystems(page, pageSize);
                if (response?.Data == null || response.Data.Count == 0)
                {
                    break;
                }

                loaded.AddRange(MapDataProjection.ToIndexedSystems(response.Data));
                if (response.Meta != null && loaded.Count >= response.Meta.Total)
                {
                    break;
                }

                page++;
            }

            return loaded;
        }

        public static Dictionary<string, IndexedSystem> BuildLookup(IEnumerable<IndexedSystem> systems)
        {
            return systems?.ToDictionary(system => system.Symbol, system => system) ?? new Dictionary<string, IndexedSystem>();
        }

        public static List<(string FromSystem, string ToSystem)> BuildJumpGateSystemLinks(IEnumerable<IndexedJumpGate> gates)
        {
            var links = new List<(string FromSystem, string ToSystem)>();
            if (gates == null)
            {
                return links;
            }

            var seen = new HashSet<string>();
            foreach (var gate in gates)
            {
                if (string.IsNullOrEmpty(gate?.SystemSymbol) || string.IsNullOrEmpty(gate.ConnectionsJson))
                {
                    continue;
                }

                foreach (var connectionWaypoint in gate.ConnectionsJson.Split(','))
                {
                    var otherSystem = ExtractSystemSymbol(connectionWaypoint);
                    if (string.IsNullOrEmpty(otherSystem))
                    {
                        continue;
                    }

                    if (otherSystem == gate.SystemSymbol)
                    {
                        continue;
                    }

                    var pair = string.Compare(gate.SystemSymbol, otherSystem, StringComparison.Ordinal) < 0
                        ? $"{gate.SystemSymbol}-{otherSystem}"
                        : $"{otherSystem}-{gate.SystemSymbol}";

                    if (seen.Add(pair))
                    {
                        links.Add((gate.SystemSymbol, otherSystem));
                    }
                }
            }

            return links;
        }

        private static string ExtractSystemSymbol(string waypointSymbol)
        {
            if (string.IsNullOrWhiteSpace(waypointSymbol))
            {
                return null;
            }

            var parts = waypointSymbol.Split('-');
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
            {
                return $"{parts[0]}-{parts[1]}";
            }

            return parts.Length > 0 ? parts[0] : null;
        }
    }
}
