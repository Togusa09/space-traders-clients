using System;
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpaceTraders.UI;
using UnityEngine;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterViewportMathTests
    {
        private static Type GetViewportMathType()
        {
            var viewportType = typeof(MapPresenter).Assembly.GetType("SpaceTraders.UI.MapViewportMath");
            Assert.NotNull(viewportType, "Expected MapViewportMath type to exist.");
            return viewportType;
        }

        [Test]
        public void WorldToScreen_And_ScreenToWorld_RoundTrip()
        {
            var viewportType = GetViewportMathType();
            var worldToScreen = viewportType.GetMethod("WorldToScreen", BindingFlags.Public | BindingFlags.Static);
            var screenToWorld = viewportType.GetMethod("ScreenToWorld", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(worldToScreen, "Expected WorldToScreen method to exist.");
            Assert.NotNull(screenToWorld, "Expected ScreenToWorld method to exist.");

            var world = new Vector2(10f, -4f);
            const float zoom = 2.5f;
            var offset = new Vector2(100f, 50f);

            var screen = (Vector2)worldToScreen.Invoke(null, new object[] { world, zoom, offset });
            var roundTrip = (Vector2)screenToWorld.Invoke(null, new object[] { screen, zoom, offset });

            Assert.That(roundTrip.x, Is.EqualTo(world.x).Within(0.0001f));
            Assert.That(roundTrip.y, Is.EqualTo(world.y).Within(0.0001f));
        }

        [Test]
        public void FitBounds_WithNoPoints_ReturnsCenteredDefaultZoom()
        {
            var viewportType = GetViewportMathType();
            var fitBounds = viewportType.GetMethod("FitBounds", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(fitBounds, "Expected FitBounds method to exist.");

            var rect = new Rect(0f, 0f, 400f, 300f);
            var result = fitBounds.Invoke(null, new object[] { new List<Vector2>(), rect, 0.1f, 10f });

            var offsetProp = result.GetType().GetProperty("Item1") ?? result.GetType().GetProperty("Offset");
            var zoomProp = result.GetType().GetProperty("Item2") ?? result.GetType().GetProperty("Zoom");
            Assert.NotNull(offsetProp, "Expected offset value in FitBounds result.");
            Assert.NotNull(zoomProp, "Expected zoom value in FitBounds result.");

            var offset = (Vector2)offsetProp.GetValue(result);
            var zoom = Convert.ToSingle(zoomProp.GetValue(result));

            Assert.That(offset.x, Is.EqualTo(200f).Within(0.0001f));
            Assert.That(offset.y, Is.EqualTo(150f).Within(0.0001f));
            Assert.That(zoom, Is.EqualTo(1.0f).Within(0.0001f));
        }
    }
}
