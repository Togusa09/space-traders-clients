using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterFilterMatcherTests
    {
        private static Type GetMatcherType()
        {
            var matcherType = typeof(MapPresenter).Assembly.GetType("SpaceTraders.UI.MapFilterMatcher");
            Assert.NotNull(matcherType, "Expected MapFilterMatcher type to exist.");
            return matcherType;
        }

        [Test]
        public void MatchesGalaxySystem_NormalizesTypeFilterToken()
        {
            var matcherType = GetMatcherType();
            var method = matcherType.GetMethod("MatchesGalaxySystem", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected MatchesGalaxySystem method to exist.");

            var system = new DatabaseManager.IndexedSystem
            {
                Symbol = "X1-TEST",
                Type = "NEUTRON_STAR",
                KnownFacilities = "MARKETPLACE,SHIPYARD"
            };

            var result = (bool)method.Invoke(null, new object[] { system, "X1", "NEUTRONSTAR", "ALL" });
            Assert.IsTrue(result);
        }

        [Test]
        public void MatchesGalaxySystem_RespectsFacilityFilter()
        {
            var matcherType = GetMatcherType();
            var method = matcherType.GetMethod("MatchesGalaxySystem", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected MatchesGalaxySystem method to exist.");

            var system = new DatabaseManager.IndexedSystem
            {
                Symbol = "X1-TEST",
                Type = "NEUTRON_STAR",
                KnownFacilities = "MARKETPLACE"
            };

            var result = (bool)method.Invoke(null, new object[] { system, "", "ALL", "SHIPYARD" });
            Assert.IsFalse(result);
        }

        [Test]
        public void MatchesWaypoint_RespectsTraitFacilityFilter()
        {
            var matcherType = GetMatcherType();
            var method = matcherType.GetMethod("MatchesWaypoint", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected MatchesWaypoint method to exist.");

            var waypoint = new SystemWaypoint("X1-TEST-A", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());
            var detailedWaypoint = new Waypoint(
                symbol: "X1-TEST-A",
                type: WaypointType.PLANET,
                systemSymbol: "X1-TEST",
                x: 0,
                y: 0,
                orbitals: new List<WaypointOrbital>(),
                traits: new List<WaypointTrait>
                {
                    new WaypointTrait(WaypointTraitSymbol.MARKETPLACE, "Marketplace", "Has a marketplace")
                },
                isUnderConstruction: false);

            var result = (bool)method.Invoke(null, new object[] { waypoint, detailedWaypoint, "X1-TEST", "PLANET", "MARKETPLACE" });
            Assert.IsTrue(result);
        }

        [Test]
        public void MatchesWaypoint_FacilityFilterWithoutDetailedWaypoint_ReturnsFalse()
        {
            var matcherType = GetMatcherType();
            var method = matcherType.GetMethod("MatchesWaypoint", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected MatchesWaypoint method to exist.");

            var waypoint = new SystemWaypoint("X1-TEST-A", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());

            var result = (bool)method.Invoke(null, new object[] { waypoint, null, "X1-TEST", "PLANET", "MARKETPLACE" });
            Assert.IsFalse(result);
        }

        [Test]
        public void MatchesWaypoint_NormalizesTypeFilterToken()
        {
            var matcherType = GetMatcherType();
            var method = matcherType.GetMethod("MatchesWaypoint", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected MatchesWaypoint method to exist.");

            var waypoint = new SystemWaypoint("X1-TEST-A", WaypointType.ORBITALSTATION, 0, 0, new List<WaypointOrbital>());
            var detailedWaypoint = new Waypoint(
                symbol: "X1-TEST-A",
                type: WaypointType.ORBITALSTATION,
                systemSymbol: "X1-TEST",
                x: 0,
                y: 0,
                orbitals: new List<WaypointOrbital>(),
                traits: new List<WaypointTrait>(),
                isUnderConstruction: false);

            var result = (bool)method.Invoke(null, new object[] { waypoint, detailedWaypoint, "X1", "ORBITAL_STATION", "ALL" });
            Assert.IsTrue(result);
        }
    }
}
