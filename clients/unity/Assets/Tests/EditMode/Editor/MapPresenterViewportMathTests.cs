using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterViewportMathTests
    {
        [Test]
        public void WorldToScreen_And_ScreenToWorld_RoundTrip()
        {
            var world = new Vector2(10f, -4f);
            const float zoom = 2.5f;
            var offset = new Vector2(100f, 50f);

            var screen = SpaceTraders.UI.MapViewportMath.WorldToScreen(world, zoom, offset);
            var roundTrip = SpaceTraders.UI.MapViewportMath.ScreenToWorld(screen, zoom, offset);

            Assert.That(roundTrip.x, Is.EqualTo(world.x).Within(0.0001f));
            Assert.That(roundTrip.y, Is.EqualTo(world.y).Within(0.0001f));
        }

        [Test]
        public void FitBounds_WithNoPoints_ReturnsCenteredDefaultZoom()
        {
            var rect = new Rect(0f, 0f, 400f, 300f);
            var result = SpaceTraders.UI.MapViewportMath.FitBounds(new List<Vector2>(), rect, 0.1f, 10f);
            var offset = result.Offset;
            var zoom = result.Zoom;

            Assert.That(offset.x, Is.EqualTo(200f).Within(0.0001f));
            Assert.That(offset.y, Is.EqualTo(150f).Within(0.0001f));
            Assert.That(zoom, Is.EqualTo(1.0f).Within(0.0001f));
        }
    }
}
