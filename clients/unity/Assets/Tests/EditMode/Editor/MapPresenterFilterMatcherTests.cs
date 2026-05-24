using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterFilterMatcherTests
    {
        [Test]
        public void MatchesGalaxySystem_NormalizesTypeFilterToken()
        {
            var system = new IndexedSystem
            {
                Symbol = "X1-TEST",
                Type = "NEUTRON_STAR",
                KnownFacilities = "MARKETPLACE,SHIPYARD"
            };

            var result = MapFilterMatcher.MatchesGalaxySystem(system, "X1", "NEUTRONSTAR", "ALL");
            Assert.IsTrue(result);
        }

        [Test]
        public void MatchesGalaxySystem_RespectsFacilityFilter()
        {
            var system = new IndexedSystem
            {
                Symbol = "X1-TEST",
                Type = "NEUTRON_STAR",
                KnownFacilities = "MARKETPLACE"
            };

            var result = MapFilterMatcher.MatchesGalaxySystem(system, "", "ALL", "SHIPYARD");
            Assert.IsFalse(result);
        }

        [Test]
        public void MatchesWaypoint_RespectsTraitFacilityFilter()
        {
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

            var result = MapFilterMatcher.MatchesWaypoint(waypoint, detailedWaypoint, "X1-TEST", "PLANET", "MARKETPLACE");
            Assert.IsTrue(result);
        }

        [Test]
        public void MatchesWaypoint_FacilityFilterWithoutDetailedWaypoint_ReturnsFalse()
        {
            var waypoint = new SystemWaypoint("X1-TEST-A", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());

            var result = MapFilterMatcher.MatchesWaypoint(waypoint, null, "X1-TEST", "PLANET", "MARKETPLACE");
            Assert.IsFalse(result);
        }

        [Test]
        public void MatchesWaypoint_NormalizesTypeFilterToken()
        {
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

            var result = MapFilterMatcher.MatchesWaypoint(waypoint, detailedWaypoint, "X1", "ORBITAL_STATION", "ALL");
            Assert.IsTrue(result);
        }
    }
}
