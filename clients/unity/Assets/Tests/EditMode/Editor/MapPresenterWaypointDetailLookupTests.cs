using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterWaypointDetailLookupTests
    {
        [Test]
        public void FindBySymbol_ReturnsMatchingWaypoint()
        {
            var waypoints = new List<Waypoint>
            {
                new Waypoint(
                    symbol: "X1-A",
                    type: WaypointType.PLANET,
                    systemSymbol: "X1",
                    x: 0,
                    y: 0,
                    orbitals: new List<WaypointOrbital>(),
                    traits: new List<WaypointTrait>(),
                    isUnderConstruction: false),
                new Waypoint(
                    symbol: "X1-B",
                    type: WaypointType.MOON,
                    systemSymbol: "X1",
                    x: 1,
                    y: 1,
                    orbitals: new List<WaypointOrbital>(),
                    traits: new List<WaypointTrait>(),
                    isUnderConstruction: false)
            };

                    var result = MapWaypointDetailLookup.FindBySymbol(waypoints, "X1-B");

            Assert.NotNull(result);
            Assert.AreEqual("X1-B", result.Symbol);
        }

        [Test]
        public void FindBySymbol_ReturnsNullWhenNoMatch()
        {
            var waypoints = new List<Waypoint>
            {
                new Waypoint(
                    symbol: "X1-A",
                    type: WaypointType.PLANET,
                    systemSymbol: "X1",
                    x: 0,
                    y: 0,
                    orbitals: new List<WaypointOrbital>(),
                    traits: new List<WaypointTrait>(),
                    isUnderConstruction: false)
            };

                    var result = MapWaypointDetailLookup.FindBySymbol(waypoints, "X1-NOT-FOUND");

            Assert.IsNull(result);
        }

        [Test]
        public void FindBySymbol_NullInputs_ReturnNull()
        {
            var resultWithNullList = MapWaypointDetailLookup.FindBySymbol(null, "X1-A");
            var resultWithNullSymbol = MapWaypointDetailLookup.FindBySymbol(new List<Waypoint>(), null);

            Assert.IsNull(resultWithNullList);
            Assert.IsNull(resultWithNullSymbol);
        }
    }
}
