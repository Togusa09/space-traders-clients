using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceTraders.UI.Map
{
    internal static class MapListOrchestration
    {
        public static int ComputeTotalPages(int itemCount, int pageSize)
        {
            if (itemCount <= 0)
            {
                return 0;
            }

            return Mathf.CeilToInt((float)itemCount / Mathf.Max(1, pageSize));
        }

        public static int ClampPage(int currentPage, int totalPages)
        {
            if (totalPages <= 0)
            {
                return 1;
            }

            return Mathf.Clamp(currentPage, 1, totalPages);
        }

        public static List<T> PageItems<T>(IEnumerable<T> items, int currentPage, int pageSize)
        {
            if (items == null)
            {
                return new List<T>();
            }

            int safePageSize = Mathf.Max(1, pageSize);
            int skip = Mathf.Max(0, currentPage - 1) * safePageSize;
            return items.Skip(skip).Take(safePageSize).ToList();
        }

        public static bool TryChangePage(int currentPage, int delta, int itemCount, int pageSize, out int nextPage, out int totalPages)
        {
            totalPages = ComputeTotalPages(itemCount, pageSize);
            if (totalPages <= 0)
            {
                nextPage = 1;
                return false;
            }

            nextPage = Mathf.Clamp(currentPage + delta, 1, totalPages);
            return true;
        }
    }

    internal static class MapFilterOptions
    {
        private static readonly List<string> GalaxyTypeChoices = new List<string>
        {
            "ALL", "NEUTRON_STAR", "RED_STAR", "ORANGE_STAR", "BLUE_STAR", "YOUNG_STAR", "WHITE_DWARF", "BLACK_HOLE", "HYPERGIANT", "NEBULA", "UNSTABLE"
        };

        private static readonly List<string> SystemTypeChoices = new List<string>
        {
            "ALL", "PLANET", "MOON", "ORBITAL_STATION", "JUMP_GATE", "ASTEROID_FIELD", "ASTEROID", "ENGINEERED_ASTEROID_OUTPOST", "ASTEROID_BASE", "NEBULA", "DEBRIS_FIELD", "GRAVITY_WELL", "ARTIFICIAL_GRAVITY_WELL", "FUEL_STATION"
        };

        private static readonly List<string> FacilityChoices = new List<string>
        {
            "ALL", "MARKETPLACE", "SHIPYARD", "CONSTRUCTION"
        };

        public static List<string> GetTypeChoices(bool galaxyMode)
        {
            return galaxyMode ? new List<string>(GalaxyTypeChoices) : new List<string>(SystemTypeChoices);
        }

        public static List<string> GetFacilityChoices()
        {
            return new List<string>(FacilityChoices);
        }
    }
}
