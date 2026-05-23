using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterWaypointTreeFilterTests
    {
        [Test]
        public void HasMatchInSubtree_ReturnsTrueForDirectMatch()
        {
            var root = new SystemWaypoint("X1-ROOT", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());
            var childMap = new Dictionary<string, List<SystemWaypoint>>();

            var result = MapWaypointTreeFilter.HasMatchInSubtree(root, childMap, w => w.Symbol == "X1-ROOT");

            Assert.IsTrue(result);
        }

        [Test]
        public void HasMatchInSubtree_ReturnsTrueForDescendantMatch()
        {
            var root = new SystemWaypoint("X1-ROOT", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());
            var child = new SystemWaypoint("X1-CHILD", WaypointType.MOON, 0, 0, new List<WaypointOrbital>(), "X1-ROOT");
            var grandchild = new SystemWaypoint("X1-GRAND", WaypointType.ASTEROID, 0, 0, new List<WaypointOrbital>(), "X1-CHILD");

            var childMap = new Dictionary<string, List<SystemWaypoint>>
            {
                ["X1-ROOT"] = new List<SystemWaypoint> { child },
                ["X1-CHILD"] = new List<SystemWaypoint> { grandchild }
            };

            var result = MapWaypointTreeFilter.HasMatchInSubtree(root, childMap, w => w.Symbol == "X1-GRAND");

            Assert.IsTrue(result);
        }

        [Test]
        public void HasMatchInSubtree_ReturnsFalseWhenNoNodeMatches()
        {
            var root = new SystemWaypoint("X1-ROOT", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());
            var child = new SystemWaypoint("X1-CHILD", WaypointType.MOON, 0, 0, new List<WaypointOrbital>(), "X1-ROOT");
            var childMap = new Dictionary<string, List<SystemWaypoint>>
            {
                ["X1-ROOT"] = new List<SystemWaypoint> { child }
            };

            var result = MapWaypointTreeFilter.HasMatchInSubtree(root, childMap, w => w.Symbol == "X1-NOT-FOUND");

            Assert.IsFalse(result);
        }

        [Test]
        public void HasMatchInSubtree_NullArguments_ReturnsFalse()
        {
            var resultWithNullWaypoint = MapWaypointTreeFilter.HasMatchInSubtree(null, null, _ => true);
            var resultWithNullPredicate = MapWaypointTreeFilter.HasMatchInSubtree(
                new SystemWaypoint("X1-ROOT", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>()),
                new Dictionary<string, List<SystemWaypoint>>(),
                null);

            Assert.IsFalse(resultWithNullWaypoint);
            Assert.IsFalse(resultWithNullPredicate);
        }
    }
}
