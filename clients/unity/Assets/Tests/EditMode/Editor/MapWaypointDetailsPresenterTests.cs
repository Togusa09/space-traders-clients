using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI.Map;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapWaypointDetailsPresenterTests
    {
        [Test]
        public void BuildWaypointListDetails_IncludesFacilityTagsWhenTraitsPresent()
        {
            var waypoint = new SystemWaypoint("X1-ABC-A1", WaypointType.PLANET, 0, 0, new List<WaypointOrbital>());
            var detailed = new Waypoint(
                symbol: "X1-ABC-A1",
                type: WaypointType.PLANET,
                systemSymbol: "X1-ABC",
                x: 0,
                y: 0,
                orbitals: new List<WaypointOrbital>(),
                traits: new List<WaypointTrait>
                {
                    new WaypointTrait(WaypointTraitSymbol.MARKETPLACE, "Marketplace", "Has market"),
                    new WaypointTrait(WaypointTraitSymbol.SHIPYARD, "Shipyard", "Has shipyard")
                },
                isUnderConstruction: false);

            var text = MapWaypointDetailsPresenter.BuildWaypointListDetails(waypoint, detailed);

            Assert.That(text, Does.Contain("PLANET"));
            Assert.That(text, Does.Contain("[MARKET]"));
            Assert.That(text, Does.Contain("[SHIPYARD]"));
        }

        [Test]
        public void SummarizeTradeGoods_ReturnsDashForNullOrEmpty()
        {
            Assert.AreEqual("-", MapWaypointDetailsPresenter.SummarizeTradeGoods(null));
            Assert.AreEqual("-", MapWaypointDetailsPresenter.SummarizeTradeGoods(new List<TradeGood>()));
        }
    }
}
