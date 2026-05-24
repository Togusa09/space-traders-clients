using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI.Map;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapWaypointServiceFacadeTests
    {
        [Test]
        public void HasWaypointTrait_ReturnsTrueWhenTraitExists()
        {
            var waypoint = new Waypoint(
                symbol: "X1-TEST-A1",
                type: WaypointType.PLANET,
                systemSymbol: "X1-TEST",
                x: 0,
                y: 0,
                orbitals: new List<WaypointOrbital>(),
                traits: new List<WaypointTrait>
                {
                    new WaypointTrait(WaypointTraitSymbol.MARKETPLACE, "Marketplace", "Has market")
                },
                isUnderConstruction: false);

            Assert.IsTrue(MapWaypointServiceFacade.HasWaypointTrait(waypoint, WaypointTraitSymbol.MARKETPLACE));
            Assert.IsFalse(MapWaypointServiceFacade.HasWaypointTrait(waypoint, WaypointTraitSymbol.SHIPYARD));
        }

        [Test]
        public void EstimateFuelRequired_UsesSystemWaypointCoordinates()
        {
            var systemWaypoints = new List<SystemWaypoint>
            {
                new SystemWaypoint("X1-TEST-A1", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>()),
                new SystemWaypoint("X1-TEST-A2", WaypointType.PLANET, 3, 4, new List<WaypointOrbital>())
            };

            var fuel = MapWaypointServiceFacade.EstimateFuelRequired(
                "X1-TEST-A1",
                "X1-TEST-A2",
                systemWaypoints,
                new List<Waypoint>());

            Assert.AreEqual(5, fuel);
        }

        [Test]
        public void EstimateFuelRequired_FallsBackToDetailedWaypointCoordinates()
        {
            var detailed = new List<Waypoint>
            {
                new Waypoint("X1-TEST-A1", WaypointType.PLANET, "X1-TEST", 1, 1, new List<WaypointOrbital>(), traits: new List<WaypointTrait>(), isUnderConstruction: false),
                new Waypoint("X1-TEST-A2", WaypointType.PLANET, "X1-TEST", 4, 5, new List<WaypointOrbital>(), traits: new List<WaypointTrait>(), isUnderConstruction: false)
            };

            var fuel = MapWaypointServiceFacade.EstimateFuelRequired(
                "X1-TEST-A1",
                "X1-TEST-A2",
                new List<SystemWaypoint>(),
                detailed);

            Assert.AreEqual(5, fuel);
        }
    }
}
