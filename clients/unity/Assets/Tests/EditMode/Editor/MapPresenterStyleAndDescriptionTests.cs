using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;
using UnityEngine;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterStyleAndDescriptionTests
    {
        [Test]
        public void MapStyleResolver_GetSystemStyle_BlackHoleHasStroke()
        {
            var resolverType = typeof(MapPresenter).Assembly.GetType("SpaceTraders.UI.MapStyleResolver");
            Assert.NotNull(resolverType, "Expected MapStyleResolver type to exist.");

            var method = resolverType.GetMethod("GetSystemStyle", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected GetSystemStyle method to exist.");

            var system = new DatabaseManager.IndexedSystem { Symbol = "X1", Type = "BLACK_HOLE" };
            object style = method.Invoke(null, new object[] { system });

            float strokeWidth = Convert.ToSingle(style.GetType().GetField("StrokeWidth", BindingFlags.Public | BindingFlags.Instance)?.GetValue(style));
            Assert.That(strokeWidth, Is.GreaterThan(0f));
        }

        [Test]
        public void MapStyleResolver_GetWaypointStyle_OrbitalStationUsesSquareShape()
        {
            var resolverType = typeof(MapPresenter).Assembly.GetType("SpaceTraders.UI.MapStyleResolver");
            Assert.NotNull(resolverType, "Expected MapStyleResolver type to exist.");

            var method = resolverType.GetMethod("GetWaypointStyle", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected GetWaypointStyle method to exist.");

            var waypoint = new SystemWaypoint("X1-TEST", WaypointType.ORBITALSTATION, 0, 0, new List<WaypointOrbital>());
            object style = method.Invoke(null, new object[] { waypoint });

            var shapeValue = style.GetType().GetField("Shape", BindingFlags.Public | BindingFlags.Instance)?.GetValue(style)?.ToString();
            Assert.AreEqual("Square", shapeValue);
        }

        [Test]
        public void WaypointDescriptionBuilder_Build_IncludesTraitNames()
        {
            var builderType = typeof(MapPresenter).Assembly.GetType("SpaceTraders.UI.WaypointDescriptionBuilder");
            Assert.NotNull(builderType, "Expected WaypointDescriptionBuilder type to exist.");

            var method = builderType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected Build method to exist.");

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

            string description = (string)method.Invoke(null, new object[] { waypoint });

            Assert.That(description, Does.Contain("Traits:"));
            Assert.That(description, Does.Contain("Marketplace"));
        }
    }
}
