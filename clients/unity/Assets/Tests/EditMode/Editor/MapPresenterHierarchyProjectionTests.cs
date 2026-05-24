using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterHierarchyProjectionTests
    {
        [Test]
        public void BuildChildMap_GroupsByOrbitsAndSortsBySymbol()
        {
            var waypoints = new List<SystemWaypoint>
            {
                new SystemWaypoint("X1-C", WaypointType.MOON, 0, 0, new List<WaypointOrbital>(), "X1-ROOT"),
                new SystemWaypoint("X1-A", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>()),
                new SystemWaypoint("X1-B", WaypointType.MOON, 0, 0, new List<WaypointOrbital>(), "X1-ROOT")
            };

            var childMap = MapHierarchyProjection.BuildChildMap(waypoints);

            Assert.IsTrue(childMap.ContainsKey(string.Empty));
            Assert.IsTrue(childMap.ContainsKey("X1-ROOT"));
            Assert.AreEqual("X1-B", childMap["X1-ROOT"][0].Symbol);
            Assert.AreEqual("X1-C", childMap["X1-ROOT"][1].Symbol);
        }

        [Test]
        public void BuildRootWaypoints_FallsBackWhenEmptyGroupMissing()
        {
            var waypoints = new List<SystemWaypoint>
            {
                new SystemWaypoint("X1-B", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>(), null),
                new SystemWaypoint("X1-A", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>(), string.Empty)
            };

            var roots = MapHierarchyProjection.BuildRootWaypoints(waypoints, new Dictionary<string, List<SystemWaypoint>>());

            Assert.AreEqual(2, roots.Count);
            Assert.AreEqual("X1-A", roots[0].Symbol);
            Assert.AreEqual("X1-B", roots[1].Symbol);
        }
    }
}
