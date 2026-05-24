using System.Collections.Generic;
using System.Linq;
using SpaceTraders.Generated.Model;

namespace SpaceTraders.UI.Map
{
    internal static class MapWaypointDetailLookup
    {
        public static Waypoint FindBySymbol(IEnumerable<Waypoint> detailedWaypoints, string symbol)
        {
            if (detailedWaypoints == null || string.IsNullOrEmpty(symbol)) return null;
            return detailedWaypoints.FirstOrDefault(x => x.Symbol == symbol);
        }
    }
}
