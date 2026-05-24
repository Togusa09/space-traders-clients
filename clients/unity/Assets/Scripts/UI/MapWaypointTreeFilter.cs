using System;
using System.Collections.Generic;
using SpaceTraders.Generated.Model;

namespace SpaceTraders.UI
{
    internal static class MapWaypointTreeFilter
    {
        public static bool HasMatchInSubtree(SystemWaypoint waypoint, Dictionary<string, List<SystemWaypoint>> childMap, Func<SystemWaypoint, bool> isMatch)
        {
            if (waypoint == null || isMatch == null) return false;

            if (isMatch(waypoint)) return true;
            if (childMap == null) return false;
            if (!childMap.TryGetValue(waypoint.Symbol, out var children) || children == null) return false;

            foreach (var child in children)
            {
                if (HasMatchInSubtree(child, childMap, isMatch)) return true;
            }

            return false;
        }
    }
}
