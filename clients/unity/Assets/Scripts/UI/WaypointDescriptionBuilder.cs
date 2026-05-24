using System.Linq;
using SpaceTraders.Generated.Model;

namespace SpaceTraders.UI
{
    internal static class WaypointDescriptionBuilder
    {
        public static string Build(Waypoint waypoint)
        {
            string description = waypoint.Type switch
            {
                WaypointType.PLANET => "Celestial body orbiting a star.",
                WaypointType.MOON => "Satellite orbiting a planet.",
                WaypointType.ORBITALSTATION => "Man-made orbital construct.",
                WaypointType.JUMPGATE => "Fast travel gateway.",
                WaypointType.ASTEROIDFIELD => "Mining region.",
                WaypointType.NEBULA => "Cloud of gas and dust.",
                WaypointType.GASGIANT => "Large gaseous planet.",
                _ => "Location in space."
            };

            if (waypoint.Traits?.Count > 0)
            {
                description += "\n\nTraits: " + string.Join(", ", waypoint.Traits.Select(t => t.Name));
            }

            return description;
        }
    }
}
