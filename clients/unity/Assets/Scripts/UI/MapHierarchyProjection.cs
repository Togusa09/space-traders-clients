using System.Collections.Generic;
using System.Linq;
using SpaceTraders.Generated.Model;

namespace SpaceTraders.UI.Map
{
    internal static class MapHierarchyProjection
    {
        public static Dictionary<string, List<SystemWaypoint>> BuildChildMap(IEnumerable<SystemWaypoint> waypoints)
        {
            if (waypoints == null)
            {
                return new Dictionary<string, List<SystemWaypoint>>();
            }

            return waypoints
                .GroupBy(waypoint => waypoint.Orbits ?? string.Empty)
                .ToDictionary(group => group.Key, group => group.OrderBy(waypoint => waypoint.Symbol).ToList());
        }

        public static List<SystemWaypoint> BuildRootWaypoints(IEnumerable<SystemWaypoint> waypoints, Dictionary<string, List<SystemWaypoint>> childMap)
        {
            if (childMap != null && childMap.TryGetValue(string.Empty, out var rootsFromMap))
            {
                return rootsFromMap;
            }

            if (waypoints == null)
            {
                return new List<SystemWaypoint>();
            }

            return waypoints
                .Where(waypoint => string.IsNullOrEmpty(waypoint.Orbits))
                .OrderBy(waypoint => waypoint.Symbol)
                .ToList();
        }
    }
}
