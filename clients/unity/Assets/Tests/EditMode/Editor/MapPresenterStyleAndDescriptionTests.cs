using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterStyleAndDescriptionTests
    {
        [Test]
        public void MapStyleResolver_GetSystemStyle_BlackHoleHasStroke()
        {
            var system = new DatabaseManager.IndexedSystem { Symbol = "X1", Type = "BLACK_HOLE" };
            var style = MapStyleResolver.GetSystemStyle(system);

            var strokeWidth = style.StrokeWidth;
            Assert.That(strokeWidth, Is.GreaterThan(0f));
        }

        [Test]
        public void MapStyleResolver_GetWaypointStyle_OrbitalStationUsesSquareShape()
        {
            var waypoint = new SystemWaypoint("X1-TEST", WaypointType.ORBITALSTATION, 0, 0, new List<WaypointOrbital>());
            var style = MapStyleResolver.GetWaypointStyle(waypoint);

            Assert.AreEqual(MapPresenter.IconShape.Square, style.Shape);
        }

        [Test]
        public void WaypointDescriptionBuilder_Build_IncludesTraitNames()
        {
            var waypoint = new Waypoint(
                symbol: "X1-TEST-A",
                type: WaypointType.PLANET,
                systemSymbol: "X1-TEST",
                x: 0,
                y: 0,
                orbitals: new List<WaypointOrbital>(),
                traits: new List<WaypointTrait>
                {
                    new WaypointTrait(WaypointTraitSymbol.MARKETPLACE, "Marketplace", "Has a marketplace")
                },
                isUnderConstruction: false);

            var description = WaypointDescriptionBuilder.Build(waypoint);

            Assert.That(description, Does.Contain("Traits:"));
            Assert.That(description, Does.Contain("Marketplace"));
        }
    }
}
