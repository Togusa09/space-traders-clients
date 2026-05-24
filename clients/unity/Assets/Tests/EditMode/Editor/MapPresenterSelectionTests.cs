using System.Collections.Generic;
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

            presenter.SetFilteredSystemsForTest(new List<IndexedSystem>
            {
                new IndexedSystem { Symbol = "A-SYS", X = 0, Y = 0 },
                new IndexedSystem { Symbol = "B-SYS", X = 10, Y = 0 }
            });

            var result = presenter.FindClosestGalaxySystemForTest(new Vector2(2.0f, 0f), 5.0f);

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

            presenter.SetCurrentSystemForTest(currentSystem);

            var result = presenter.FindClosestSystemWaypointForTest(new Vector2(100f, 100f), 3.0f);

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

            presenter.SetCurrentSystemForTest(currentSystem);

            var result = presenter.FindClosestSystemWaypointForTest(new Vector2(1f, 0f), 5.0f);

            Assert.NotNull(result);
            Assert.AreEqual("X1-TEST-A", result.Symbol);
        }

        [Test]
        public void FindClosestGalaxySystem_ReturnsNullOutsideThreshold()
        {
            var presenter = new GameObject("MapPresenterTest").AddComponent<MapPresenter>();

            presenter.SetFilteredSystemsForTest(new List<IndexedSystem>
            {
                new IndexedSystem { Symbol = "A-SYS", X = 0, Y = 0 }
            });

            var result = presenter.FindClosestGalaxySystemForTest(new Vector2(100f, 100f), 2.0f);

            Assert.IsNull(result);
        }

        [Test]
        public void HandleMapClick_GalaxyMode_SelectsClosestSystem()
        {
            var presenter = new GameObject("MapPresenterTest").AddComponent<MapPresenter>();

            presenter.SetFilteredSystemsForTest(new List<IndexedSystem>
            {
                new IndexedSystem { Symbol = "A-SYS", X = 0, Y = 0 },
                new IndexedSystem { Symbol = "B-SYS", X = 12, Y = 0 }
            });
            presenter.SetMapModeForTest(systemMode: false);
            presenter.MapZoom = 1.0f;
            presenter.MapOffset = Vector2.zero;

            presenter.HandleMapClickForTest(new Vector2(1f, 0f));

            Assert.AreEqual("A-SYS", presenter.GetSelectedSymbolForTest());
            Assert.AreEqual("A-SYS", presenter.GetSelectedSystemSymbolForTest());
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

            presenter.SetCurrentSystemForTest(currentSystem);
            presenter.SetMapModeForTest(systemMode: true);
            presenter.MapZoom = 1.0f;
            presenter.MapOffset = Vector2.zero;

            presenter.HandleMapClickForTest(new Vector2(2f, 0f));

            Assert.AreEqual("X1-TEST-A", presenter.GetSelectedSymbolForTest());
        }

        [Test]
        public void HandleMapClick_GalaxyMode_OutsideThreshold_DoesNotSelect()
        {
            var presenter = new GameObject("MapPresenterTest").AddComponent<MapPresenter>();

            presenter.SetFilteredSystemsForTest(new List<IndexedSystem>
            {
                new IndexedSystem { Symbol = "A-SYS", X = 0, Y = 0 }
            });
            presenter.SetMapModeForTest(systemMode: false);
            presenter.MapZoom = 1.0f;
            presenter.MapOffset = Vector2.zero;

            presenter.HandleMapClickForTest(new Vector2(100f, 100f));

            Assert.IsNull(presenter.GetSelectedSymbolForTest());
            Assert.IsNull(presenter.GetSelectedSystemSymbolForTest());
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

            presenter.SetCurrentSystemForTest(currentSystem);
            presenter.SetMapModeForTest(systemMode: true);
            presenter.MapZoom = 1.0f;
            presenter.MapOffset = Vector2.zero;

            presenter.HandleMapClickForTest(new Vector2(100f, 100f));

            Assert.IsNull(presenter.GetSelectedSymbolForTest());
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

    }
}
