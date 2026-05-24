using System;
using System.Linq;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;

namespace SpaceTraders.UI
{
    internal static class MapFilterMatcher
    {
        public static bool MatchesGalaxySystem(IndexedSystem system, string query, string typeFilter, string facilityFilter)
        {
            bool matchesQuery = string.IsNullOrEmpty(query) || system.Symbol.Contains(query);
            bool matchesType = IsTypeMatch(system.Type, typeFilter);
            bool matchesFacility = IsFacilityStringMatch(system.KnownFacilities, facilityFilter);
            return matchesQuery && matchesType && matchesFacility;
        }

        public static bool MatchesWaypoint(SystemWaypoint waypoint, Waypoint detailedWaypoint, string search, string typeFilter, string facilityFilter)
        {
            bool matchesSearch = string.IsNullOrEmpty(search) || waypoint.Symbol.Contains(search);
            bool matchesType = IsTypeMatch(waypoint.Type.ToString(), typeFilter);
            bool matchesFacility = IsWaypointFacilityMatch(detailedWaypoint, facilityFilter);
            return matchesSearch && matchesType && matchesFacility;
        }

        private static bool IsTypeMatch(string candidateType, string typeFilter)
        {
            if (typeFilter == "ALL") return true;
            return NormalizeToken(candidateType) == NormalizeToken(typeFilter);
        }

        private static bool IsFacilityStringMatch(string knownFacilities, string facilityFilter)
        {
            if (facilityFilter == "ALL") return true;
            return !string.IsNullOrEmpty(knownFacilities) && knownFacilities.Contains(facilityFilter);
        }

        private static bool IsWaypointFacilityMatch(Waypoint detailedWaypoint, string facilityFilter)
        {
            if (facilityFilter == "ALL") return true;
            if (detailedWaypoint?.Traits == null) return false;

            string normalizedFacility = NormalizeToken(facilityFilter);
            return detailedWaypoint.Traits.Any(t => NormalizeToken(t.Symbol.ToString()) == normalizedFacility);
        }

        private static string NormalizeToken(string value)
        {
            return (value ?? string.Empty).Replace("_", string.Empty).ToUpperInvariant();
        }
    }
}
