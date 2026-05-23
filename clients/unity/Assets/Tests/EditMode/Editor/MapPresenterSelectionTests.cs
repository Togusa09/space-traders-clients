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
                    new SystemWaypoint("X1-TEST-A", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>()),
                    new SystemWaypoint("X1-TEST-B", WaypointType.MOON, 25, 0, new List<WaypointOrbital>())
                },
                factions: new List<SystemFaction>());

            SetPrivateField(presenter, "_currentSystem", currentSystem);

            var method = typeof(MapPresenter).GetMethod("FindClosestSystemWaypoint", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, "Expected private method FindClosestSystemWaypoint to exist.");

            var result = method.Invoke(presenter, new object[] { new Vector2(100f, 100f), 3.0f }) as SystemWaypoint;

            Assert.IsNull(result);
        }

        [Test]
        public void FindClosestSystemWaypoint_ReturnsNearestWithinThreshold()
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
                    new SystemWaypoint("X1-TEST-A", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>()),
                    new SystemWaypoint("X1-TEST-B", WaypointType.MOON, 20, 0, new List<WaypointOrbital>())
                },
                factions: new List<SystemFaction>());

            SetPrivateField(presenter, "_currentSystem", currentSystem);

            var method = typeof(MapPresenter).GetMethod("FindClosestSystemWaypoint", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, "Expected private method FindClosestSystemWaypoint to exist.");

            var result = method.Invoke(presenter, new object[] { new Vector2(1f, 0f), 5.0f }) as SystemWaypoint;

            Assert.NotNull(result);
            Assert.AreEqual("X1-TEST-A", result.Symbol);
        }

        [Test]
        public void FindClosestGalaxySystem_ReturnsNullOutsideThreshold()
        {
            var presenter = new GameObject("MapPresenterTest").AddComponent<MapPresenter>();

            SetPrivateField(presenter, "_filteredSystems", new List<DatabaseManager.IndexedSystem>
            {
                new DatabaseManager.IndexedSystem { Symbol = "A-SYS", X = 0, Y = 0 }
            });

            var method = typeof(MapPresenter).GetMethod("FindClosestGalaxySystem", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, "Expected private method FindClosestGalaxySystem to exist.");

            var result = method.Invoke(presenter, new object[] { new Vector2(100f, 100f), 2.0f }) as DatabaseManager.IndexedSystem;

            Assert.IsNull(result);
        }

        [Test]
        public void HandleMapClick_GalaxyMode_SelectsClosestSystem()
        {
            var presenter = new GameObject("MapPresenterTest").AddComponent<MapPresenter>();

            SetPrivateField(presenter, "_filteredSystems", new List<DatabaseManager.IndexedSystem>
            {
                new DatabaseManager.IndexedSystem { Symbol = "A-SYS", X = 0, Y = 0 },
                new DatabaseManager.IndexedSystem { Symbol = "B-SYS", X = 12, Y = 0 }
            });
            SetPrivateField(presenter, "_mapMode", ParseMapMode("Galaxy"));
            presenter.MapZoom = 1.0f;
            presenter.MapOffset = Vector2.zero;

            var method = typeof(MapPresenter).GetMethod("HandleMapClick", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, "Expected private method HandleMapClick to exist.");

            method.Invoke(presenter, new object[] { new Vector2(1f, 0f) });

            Assert.AreEqual("A-SYS", GetPrivateField<string>(presenter, "_selectedSymbol"));
            Assert.AreEqual("A-SYS", GetPrivateField<string>(presenter, "_selectedSystemSymbol"));
        }

        [Test]
        public void HandleMapClick_SystemMode_SelectsClosestWaypoint()
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
                    new SystemWaypoint("X1-TEST-A", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>()),
                    new SystemWaypoint("X1-TEST-B", WaypointType.MOON, 20, 0, new List<WaypointOrbital>())
                },
                factions: new List<SystemFaction>());

            SetPrivateField(presenter, "_currentSystem", currentSystem);
            SetPrivateField(presenter, "_mapMode", ParseMapMode("System"));
            presenter.MapZoom = 1.0f;
            presenter.MapOffset = Vector2.zero;

            var method = typeof(MapPresenter).GetMethod("HandleMapClick", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, "Expected private method HandleMapClick to exist.");

            method.Invoke(presenter, new object[] { new Vector2(2f, 0f) });

            Assert.AreEqual("X1-TEST-A", GetPrivateField<string>(presenter, "_selectedSymbol"));
        }

        [Test]
        public void HandleMapClick_GalaxyMode_OutsideThreshold_DoesNotSelect()
        {
            var presenter = new GameObject("MapPresenterTest").AddComponent<MapPresenter>();

            SetPrivateField(presenter, "_filteredSystems", new List<DatabaseManager.IndexedSystem>
            {
                new DatabaseManager.IndexedSystem { Symbol = "A-SYS", X = 0, Y = 0 }
            });
            SetPrivateField(presenter, "_mapMode", ParseMapMode("Galaxy"));
            presenter.MapZoom = 1.0f;
            presenter.MapOffset = Vector2.zero;

            var method = typeof(MapPresenter).GetMethod("HandleMapClick", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, "Expected private method HandleMapClick to exist.");

            method.Invoke(presenter, new object[] { new Vector2(100f, 100f) });

            Assert.IsNull(GetPrivateField<string>(presenter, "_selectedSymbol"));
            Assert.IsNull(GetPrivateField<string>(presenter, "_selectedSystemSymbol"));
        }

        [Test]
        public void HandleMapClick_SystemMode_OutsideThreshold_DoesNotSelect()
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
                    new SystemWaypoint("X1-TEST-A", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>())
                },
                factions: new List<SystemFaction>());

            SetPrivateField(presenter, "_currentSystem", currentSystem);
            SetPrivateField(presenter, "_mapMode", ParseMapMode("System"));
            presenter.MapZoom = 1.0f;
            presenter.MapOffset = Vector2.zero;

            var method = typeof(MapPresenter).GetMethod("HandleMapClick", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, "Expected private method HandleMapClick to exist.");

            method.Invoke(presenter, new object[] { new Vector2(100f, 100f) });

            Assert.IsNull(GetPrivateField<string>(presenter, "_selectedSymbol"));
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

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field, $"Expected private field '{fieldName}' to exist.");
            return (T)field.GetValue(target);
        }

        private static object ParseMapMode(string value)
        {
            var mapModeType = typeof(MapPresenter).GetNestedType("MapMode", BindingFlags.NonPublic);
            Assert.NotNull(mapModeType, "Expected private enum MapMode to exist.");
            return System.Enum.Parse(mapModeType, value);
        }
    }
}
