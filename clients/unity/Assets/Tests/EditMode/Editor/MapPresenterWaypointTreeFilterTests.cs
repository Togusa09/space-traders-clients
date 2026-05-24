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
            Func<SystemWaypoint, bool> matcher = w => w.Symbol == "X1-ROOT";

            var result = MapWaypointTreeFilter.HasMatchInSubtree(root, childMap, matcher);

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
            Func<SystemWaypoint, bool> matcher = w => w.Symbol == "X1-GRAND";

            var result = MapWaypointTreeFilter.HasMatchInSubtree(root, childMap, matcher);

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
            Func<SystemWaypoint, bool> matcher = w => w.Symbol == "X1-NOT-FOUND";

            var result = MapWaypointTreeFilter.HasMatchInSubtree(root, childMap, matcher);

            Assert.IsFalse(result);
        }

        [Test]
        public void HasMatchInSubtree_NullArguments_ReturnsFalse()
        {
            Func<SystemWaypoint, bool> matcher = _ => true;

            var resultWithNullWaypoint = MapWaypointTreeFilter.HasMatchInSubtree(null, null, matcher);
            var resultWithNullPredicate = MapWaypointTreeFilter.HasMatchInSubtree(
                new SystemWaypoint("X1-ROOT", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>()),
                new Dictionary<string, List<SystemWaypoint>>(),
                null);

            Assert.IsFalse(resultWithNullWaypoint);
            Assert.IsFalse(resultWithNullPredicate);
        }
    }
}
