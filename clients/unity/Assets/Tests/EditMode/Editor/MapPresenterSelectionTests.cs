using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;
using UnityEngine;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterSelectionTests
    {
        [Test]
        public void FindClosestGalaxySystem_ReturnsNearestWithinThreshold()
        {
            var presenter = new GameObject("MapPresenterTest").AddComponent<MapPresenter>();

            SetPrivateField(presenter, "_filteredSystems", new List<DatabaseManager.IndexedSystem>
            {
                new DatabaseManager.IndexedSystem { Symbol = "A-SYS", X = 0, Y = 0 },
                new DatabaseManager.IndexedSystem { Symbol = "B-SYS", X = 10, Y = 0 }
            });

            var method = typeof(MapPresenter).GetMethod("FindClosestGalaxySystem", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, "Expected private method FindClosestGalaxySystem to exist.");

            var result = method.Invoke(presenter, new object[] { new Vector2(2.0f, 0f), 5.0f }) as DatabaseManager.IndexedSystem;

            Assert.NotNull(result);
            Assert.AreEqual("A-SYS", result.Symbol);
        }

        [Test]
        public void FindClosestSystemWaypoint_ReturnsNullOutsideThreshold()
        {
            var presenter = new GameObject("MapPresenterTest").AddComponent<MapPresenter>();

            var currentSystem = new SpaceTraders.Generated.Model.System(
                symbol: "X1-TEST",
                sectorSymbol: "X1",
                type: SystemType.NEUTRONSTAR,
                x: 0,
                y: 0,
                waypoints: new List<SystemWaypoint>
                {
                    new SystemWaypoint("X1-TEST-A", WaypointType.PLANET, 0, 0),
                    new SystemWaypoint("X1-TEST-B", WaypointType.MOON, 25, 0)
                },
                factions: new List<SystemFaction>());

            SetPrivateField(presenter, "_currentSystem", currentSystem);

            var method = typeof(MapPresenter).GetMethod("FindClosestSystemWaypoint", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, "Expected private method FindClosestSystemWaypoint to exist.");

            var result = method.Invoke(presenter, new object[] { new Vector2(100f, 100f), 3.0f }) as SystemWaypoint;

            Assert.IsNull(result);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "MapPresenterTest" && go.scene.IsValid())
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }
    }
}
