using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI.Map;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapWaypointSpecializedInfoServicePlannerTests
    {
        [Test]
        public void CreatePlan_ReturnsAllFalseWhenNoTraits()
        {
            var waypoint = new Waypoint(
                symbol: "X1-TEST-A1",
                type: WaypointType.PLANET,
                systemSymbol: "X1-TEST",
                x: 0,
                y: 0,
                orbitals: new List<WaypointOrbital>(),
                traits: new List<WaypointTrait>(),
                isUnderConstruction: false);

            var plan = MapWaypointSpecializedInfoServicePlanner.CreatePlan(waypoint);

            Assert.IsFalse(plan.LoadMarket);
            Assert.IsFalse(plan.LoadShipyard);
            Assert.IsFalse(plan.LoadConstruction);
        }

        [Test]
        public void CreatePlan_MapsKnownTraitsToRequests()
        {
            var waypoint = new Waypoint(
                symbol: "X1-TEST-A1",
                type: WaypointType.PLANET,
                systemSymbol: "X1-TEST",
                x: 0,
                y: 0,
                orbitals: new List<WaypointOrbital>(),
                traits: new List<WaypointTrait>
                {
                    new WaypointTrait(WaypointTraitSymbol.MARKETPLACE, "Marketplace", "Has market"),
                    new WaypointTrait(WaypointTraitSymbol.SHIPYARD, "Shipyard", "Has shipyard"),
                    new WaypointTrait(WaypointTraitSymbol.UNDERCONSTRUCTION, "Construction", "Under construction")
                },
                isUnderConstruction: true);

            var plan = MapWaypointSpecializedInfoServicePlanner.CreatePlan(waypoint);

            Assert.IsTrue(plan.LoadMarket);
            Assert.IsTrue(plan.LoadShipyard);
            Assert.IsTrue(plan.LoadConstruction);
        }
    }
}
