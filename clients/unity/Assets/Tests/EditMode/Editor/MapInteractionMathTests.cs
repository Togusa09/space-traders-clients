using NUnit.Framework;
using SpaceTraders.UI.Map;
using UnityEngine;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapInteractionMathTests
    {
        [Test]
        public void ApplyPan_AddsPointerDeltaToOffset()
        {
            var result = MapInteractionMath.ApplyPan(new Vector2(10f, -5f), new Vector2(2f, 3f), new Vector2(5f, 11f));

            Assert.AreEqual(new Vector2(13f, 3f), result);
        }

        [Test]
        public void ApplyZoom_ClampsZoomToMax_AndKeepsMouseAnchored()
        {
            float zoom = 2f;
            var offset = new Vector2(10f, 20f);
            var mouse = new Vector2(40f, 60f);

            var result = MapInteractionMath.ApplyZoom(
                currentZoom: zoom,
                currentOffset: offset,
                localMousePosition: mouse,
                wheelDeltaY: -50f,
                minZoom: 0.5f,
                maxZoom: 3f);

            Assert.AreEqual(3f, result.Zoom);
            Assert.AreEqual(new Vector2(-5f, 0f), result.Offset);
        }

        [Test]
        public void ApplyZoom_ClampsZoomToMin()
        {
            var result = MapInteractionMath.ApplyZoom(
                currentZoom: 1f,
                currentOffset: Vector2.zero,
                localMousePosition: Vector2.zero,
                wheelDeltaY: 100f,
                minZoom: 0.2f,
                maxZoom: 4f);

            Assert.AreEqual(0.2f, result.Zoom);
        }
    }
}
