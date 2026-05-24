using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class UniverseSyncPageWorkerTests
    {
        [Test]
        public void ProcessPage_MapsSystemsPreservesFacilitiesAndExtractsJumpGates()
        {
            var response = new GetSystems200Response(
                data: new List<SpaceTraders.Generated.Model.System>
                {
                    new SpaceTraders.Generated.Model.System(
                        symbol: "X1-TEST",
                        sectorSymbol: "X1",
                        type: SystemType.NEUTRONSTAR,
                        x: 5,
                        y: -2,
                        waypoints: new List<SystemWaypoint>
                        {
                            new SystemWaypoint("X1-TEST-PLANET", WaypointType.PLANET, 1, 1, new List<WaypointOrbital>()),
                            new SystemWaypoint("X1-TEST-JG", WaypointType.JUMPGATE, 2, 2, new List<WaypointOrbital>())
                        },
                        factions: new List<SystemFaction>())
                },
                meta: new Meta(total: 95, page: 1, limit: 20));

            var existing = new Dictionary<string, DatabaseManager.IndexedSystem>
            {
                ["X1-TEST"] = new DatabaseManager.IndexedSystem
                {
                    Symbol = "X1-TEST",
                    KnownFacilities = "MARKETPLACE,SHIPYARD"
                }
            };

            var result = UniverseSyncPageWorker.ProcessPage(response, existing);

            Assert.AreEqual(95, result.TotalSystemsExpected);
            Assert.AreEqual(5, result.TotalPages);
            Assert.AreEqual(1, result.IndexedSystems.Count);
            Assert.AreEqual("X1-TEST", result.IndexedSystems[0].Symbol);
            Assert.AreEqual("MARKETPLACE,SHIPYARD", result.IndexedSystems[0].KnownFacilities);
            Assert.AreEqual(1, result.JumpGateWaypoints.Count);
            Assert.AreEqual("X1-TEST-JG", result.JumpGateWaypoints[0].waypointSymbol);
            Assert.AreEqual("X1-TEST", result.JumpGateWaypoints[0].systemSymbol);
        }

        [Test]
        public void ProcessPage_NullMetaOrInvalidLimit_DefaultsTotalPagesToOne()
        {
            var response = new GetSystems200Response(
                data: new List<SpaceTraders.Generated.Model.System>(),
                meta: new Meta(total: 0, page: 1, limit: 0));

            var result = UniverseSyncPageWorker.ProcessPage(response, null);

            Assert.AreEqual(1, result.TotalPages);
        }
    }
}
