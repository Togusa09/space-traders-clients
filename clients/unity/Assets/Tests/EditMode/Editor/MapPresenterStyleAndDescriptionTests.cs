using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterStyleAndDescriptionTests
    {
        [TestCase("RED_STAR", 1f, 0.4f, 0.4f, "Circle", 3f)]
        [TestCase("BLUE_STAR", 0.4f, 0.4f, 1f, "Circle", 3f)]
        [TestCase("YOUNG_STAR", 0.6f, 1f, 1f, "Circle", 3f)]
        [TestCase("WHITE_DWARF", 1f, 1f, 1f, "Circle", 2f)]
        [TestCase("NEBULA", 1f, 0.2f, 1f, "Square", 5f)]
        public void MapStyleResolver_GetSystemStyle_KnownTypesUseExpectedStyle(
            string systemType,
            float expectedR,
            float expectedG,
            float expectedB,
            string expectedShape,
            float expectedRadius)
        {
            var system = new IndexedSystem { Symbol = "X1", Type = systemType };
            var style = MapStyleResolver.GetSystemStyle(system);

            Assert.AreEqual(expectedShape, style.Shape.ToString());
            Assert.AreEqual(expectedRadius, style.Radius);
            Assert.That(style.FillColor.r, Is.EqualTo(expectedR).Within(0.001f));
            Assert.That(style.FillColor.g, Is.EqualTo(expectedG).Within(0.001f));
            Assert.That(style.FillColor.b, Is.EqualTo(expectedB).Within(0.001f));
        }

        [Test]
        public void MapStyleResolver_GetSystemStyle_BlackHoleHasStroke()
        {
            var system = new IndexedSystem { Symbol = "X1", Type = "BLACK_HOLE" };
            var style = MapStyleResolver.GetSystemStyle(system);

            var strokeWidth = style.StrokeWidth;
            Assert.That(strokeWidth, Is.GreaterThan(0f));
        }

        [TestCase(WaypointType.PLANET, "Circle", 4f)]
        [TestCase(WaypointType.MOON, "Circle", 2f)]
        [TestCase(WaypointType.ORBITALSTATION, "Square", 3f)]
        [TestCase(WaypointType.JUMPGATE, "Diamond", 4f)]
        [TestCase(WaypointType.ASTEROIDFIELD, "Hexagon", 4f)]
        public void MapStyleResolver_GetWaypointStyle_KnownTypesUseExpectedShapeAndRadius(
            WaypointType waypointType,
            string expectedShape,
            float expectedRadius)
        {
            var waypoint = new SystemWaypoint("X1-TEST", waypointType, 0, 0, new List<WaypointOrbital>());
            var style = MapStyleResolver.GetWaypointStyle(waypoint);

            Assert.AreEqual(expectedShape, style.Shape.ToString());
            Assert.AreEqual(expectedRadius, style.Radius);
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
