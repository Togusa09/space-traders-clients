using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterWaypointDetailLookupTests
    {
        private static MethodInfo GetFindBySymbolMethod()
        {
            var type = typeof(MapPresenter).Assembly.GetType("SpaceTraders.UI.MapWaypointDetailLookup");
            Assert.NotNull(type, "Expected MapWaypointDetailLookup type to exist.");

            var method = type.GetMethod("FindBySymbol", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected FindBySymbol method to exist.");
            return method;
        }

        [Test]
        public void FindBySymbol_ReturnsMatchingWaypoint()
        {
            var method = GetFindBySymbolMethod();
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

            var result = (Waypoint)method.Invoke(null, new object[] { waypoints, "X1-B" });

            Assert.NotNull(result);
            Assert.AreEqual("X1-B", result.Symbol);
        }

        [Test]
        public void FindBySymbol_ReturnsNullWhenNoMatch()
        {
            var method = GetFindBySymbolMethod();
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

            var result = (Waypoint)method.Invoke(null, new object[] { waypoints, "X1-NOT-FOUND" });

            Assert.IsNull(result);
        }

        [Test]
        public void FindBySymbol_NullInputs_ReturnNull()
        {
            var method = GetFindBySymbolMethod();
            var resultWithNullList = (Waypoint)method.Invoke(null, new object[] { null, "X1-A" });
            var resultWithNullSymbol = (Waypoint)method.Invoke(null, new object[] { new List<Waypoint>(), null });

            Assert.IsNull(resultWithNullList);
            Assert.IsNull(resultWithNullSymbol);
        }
    }
}
