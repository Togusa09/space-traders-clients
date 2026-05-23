using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterDataProjectionTests
    {
        [Test]
        public void ToIndexedSystems_MapsCoreFieldsAndWaypointCount()
        {
            var systems = new List<SpaceTraders.Generated.Model.System>
            {
                new SpaceTraders.Generated.Model.System(
                    symbol: "X1-A",
                    sectorSymbol: "X1",
                    type: SystemType.NEUTRONSTAR,
                    x: 12,
                    y: -4,
                    waypoints: new List<SystemWaypoint>
                    {
                        new SystemWaypoint("X1-A-WP1", WaypointType.PLANET, 1, 2, new List<WaypointOrbital>()),
                        new SystemWaypoint("X1-A-WP2", WaypointType.MOON, 3, 4, new List<WaypointOrbital>())
                    },
                    factions: new List<SystemFaction>())
            };

            var result = MapDataProjection.ToIndexedSystems(systems);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("X1-A", result[0].Symbol);
            Assert.AreEqual("X1", result[0].SectorSymbol);
            Assert.AreEqual("NEUTRONSTAR", result[0].Type);
            Assert.AreEqual(12, result[0].X);
            Assert.AreEqual(-4, result[0].Y);
            Assert.AreEqual(2, result[0].WaypointCount);
        }

        [Test]
        public void ToIndexedSystems_NullInput_ReturnsEmptyList()
        {
            var result = MapDataProjection.ToIndexedSystems(null);

            Assert.NotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public void ToSystemWaypoints_MapsOrbitDataAndDefaultsNullOrbitals()
        {
            var waypointWithNullOrbitals = new Waypoint(
                symbol: "X1-WP-A",
                type: WaypointType.ORBITALSTATION,
                systemSymbol: "X1",
                x: 10,
                y: 20,
                orbitals: new List<WaypointOrbital>(),
                orbits: "X1-WP-PARENT",
                traits: new List<WaypointTrait>(),
                isUnderConstruction: false);
            waypointWithNullOrbitals.Orbitals = null;

            var detailed = new List<Waypoint>
            {
                waypointWithNullOrbitals
            };

            var result = MapDataProjection.ToSystemWaypoints(detailed);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("X1-WP-A", result[0].Symbol);
            Assert.AreEqual(WaypointType.ORBITALSTATION, result[0].Type);
            Assert.AreEqual(10, result[0].X);
            Assert.AreEqual(20, result[0].Y);
            Assert.AreEqual("X1-WP-PARENT", result[0].Orbits);
            Assert.NotNull(result[0].Orbitals);
            Assert.AreEqual(0, result[0].Orbitals.Count);
        }

        [Test]
        public void ExtractKnownFacilitiesCsv_ReturnsStableUniqueOrder()
        {
            var waypoints = new List<Waypoint>
            {
                new Waypoint(
                    symbol: "X1-WP-1",
                    type: WaypointType.PLANET,
                    systemSymbol: "X1",
                    x: 0,
                    y: 0,
                    orbitals: new List<WaypointOrbital>(),
                    traits: new List<WaypointTrait>
                    {
                        new WaypointTrait(WaypointTraitSymbol.SHIPYARD, "Shipyard", "Shipyard"),
                        new WaypointTrait(WaypointTraitSymbol.MARKETPLACE, "Marketplace", "Marketplace")
                    },
                    isUnderConstruction: false),
                new Waypoint(
                    symbol: "X1-WP-2",
                    type: WaypointType.MOON,
                    systemSymbol: "X1",
                    x: 1,
                    y: 1,
                    orbitals: new List<WaypointOrbital>(),
                    traits: new List<WaypointTrait>
                    {
                        new WaypointTrait(WaypointTraitSymbol.MARKETPLACE, "Marketplace", "Marketplace"),
                        new WaypointTrait(WaypointTraitSymbol.UNDERCONSTRUCTION, "Construction", "Construction")
                    },
                    isUnderConstruction: false)
            };

            var result = MapDataProjection.ExtractKnownFacilitiesCsv(waypoints);

            Assert.AreEqual("MARKETPLACE,SHIPYARD,CONSTRUCTION", result);
        }

        [Test]
        public void ExtractKnownFacilitiesCsv_NoMatchingTraits_ReturnsEmpty()
        {
            var waypoints = new List<Waypoint>
            {
                new Waypoint(
                    symbol: "X1-WP-1",
                    type: WaypointType.PLANET,
                    systemSymbol: "X1",
                    x: 0,
                    y: 0,
                    orbitals: new List<WaypointOrbital>(),
                    traits: new List<WaypointTrait>(),
                    isUnderConstruction: false)
            };

            var result = MapDataProjection.ExtractKnownFacilitiesCsv(waypoints);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
