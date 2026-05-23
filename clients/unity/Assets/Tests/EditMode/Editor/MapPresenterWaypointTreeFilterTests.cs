using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterWaypointTreeFilterTests
    {
        private static MethodInfo GetHasMatchMethod()
        {
            var type = typeof(MapPresenter).Assembly.GetType("SpaceTraders.UI.MapWaypointTreeFilter");
            Assert.NotNull(type, "Expected MapWaypointTreeFilter type to exist.");

            var method = type.GetMethod("HasMatchInSubtree", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected HasMatchInSubtree method to exist.");
            return method;
        }

        [Test]
        public void HasMatchInSubtree_ReturnsTrueForDirectMatch()
        {
            var method = GetHasMatchMethod();
            var root = new SystemWaypoint("X1-ROOT", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());
            var childMap = new Dictionary<string, List<SystemWaypoint>>();
            Func<SystemWaypoint, bool> matcher = w => w.Symbol == "X1-ROOT";

            var result = (bool)method.Invoke(null, new object[] { root, childMap, matcher });

            Assert.IsTrue(result);
        }

        [Test]
        public void HasMatchInSubtree_ReturnsTrueForDescendantMatch()
        {
            var method = GetHasMatchMethod();
            var root = new SystemWaypoint("X1-ROOT", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());
            var child = new SystemWaypoint("X1-CHILD", WaypointType.MOON, 0, 0, new List<WaypointOrbital>(), "X1-ROOT");
            var grandchild = new SystemWaypoint("X1-GRAND", WaypointType.ASTEROID, 0, 0, new List<WaypointOrbital>(), "X1-CHILD");

            var childMap = new Dictionary<string, List<SystemWaypoint>>
            {
                ["X1-ROOT"] = new List<SystemWaypoint> { child },
                ["X1-CHILD"] = new List<SystemWaypoint> { grandchild }
            };
            Func<SystemWaypoint, bool> matcher = w => w.Symbol == "X1-GRAND";

            var result = (bool)method.Invoke(null, new object[] { root, childMap, matcher });

            Assert.IsTrue(result);
        }

        [Test]
        public void HasMatchInSubtree_ReturnsFalseWhenNoNodeMatches()
        {
            var method = GetHasMatchMethod();
            var root = new SystemWaypoint("X1-ROOT", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());
            var child = new SystemWaypoint("X1-CHILD", WaypointType.MOON, 0, 0, new List<WaypointOrbital>(), "X1-ROOT");
            var childMap = new Dictionary<string, List<SystemWaypoint>>
            {
                ["X1-ROOT"] = new List<SystemWaypoint> { child }
            };
            Func<SystemWaypoint, bool> matcher = w => w.Symbol == "X1-NOT-FOUND";

            var result = (bool)method.Invoke(null, new object[] { root, childMap, matcher });

            Assert.IsFalse(result);
        }

        [Test]
        public void HasMatchInSubtree_NullArguments_ReturnsFalse()
        {
            var method = GetHasMatchMethod();
            Func<SystemWaypoint, bool> matcher = _ => true;

            var resultWithNullWaypoint = (bool)method.Invoke(null, new object[] { null, null, matcher });
            var resultWithNullPredicate = (bool)method.Invoke(
                null,
                new object[]
                {
                    new SystemWaypoint("X1-ROOT", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>()),
                    new Dictionary<string, List<SystemWaypoint>>(),
                    null
                });

            Assert.IsFalse(resultWithNullWaypoint);
            Assert.IsFalse(resultWithNullPredicate);
        }
    }
}
