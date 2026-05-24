using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterViewportFitBoundsTests
    {
        [Test]
        public void FitBounds_ClampsZoomToMaximum()
        {
            var points = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 1f)
            };
            var rect = new Rect(0f, 0f, 1000f, 1000f);

            var result = SpaceTraders.UI.Map.MapViewportMath.FitBounds(points, rect, 0.1f, 2.0f);
            var zoom = result.Zoom;

            Assert.That(zoom, Is.EqualTo(2.0f).Within(0.0001f));
        }

        [Test]
        public void FitBounds_ClampsZoomToMinimum()
        {
            var points = new List<Vector2>
            {
                new Vector2(-5000f, -5000f),
                new Vector2(5000f, 5000f)
            };
            var rect = new Rect(0f, 0f, 100f, 100f);

            var result = SpaceTraders.UI.Map.MapViewportMath.FitBounds(points, rect, 0.25f, 100f);
            var zoom = result.Zoom;

            Assert.That(zoom, Is.EqualTo(0.25f).Within(0.0001f));
        }
    }
}
