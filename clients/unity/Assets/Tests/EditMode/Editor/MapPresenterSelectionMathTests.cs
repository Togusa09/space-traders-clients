using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterSelectionMathTests
    {
        private class DummyPoint
        {
            public string Name { get; set; }
            public Vector2 Position { get; set; }
        }

        [Test]
        public void FindClosest_ReturnsNearestWithinThreshold()
        {
            var points = new List<DummyPoint>
            {
                new DummyPoint { Name = "A", Position = new Vector2(0f, 0f) },
                new DummyPoint { Name = "B", Position = new Vector2(10f, 0f) }
            };

            Func<DummyPoint, Vector2> selector = p => p.Position;
            var result = SpaceTraders.UI.MapSelectionMath.FindClosest(points, new Vector2(2f, 0f), 5f, selector);

            Assert.NotNull(result);
            Assert.AreEqual("A", result.Name);
        }

        [Test]
        public void FindClosest_ReturnsNullOutsideThreshold()
        {
            var points = new List<DummyPoint>
            {
                new DummyPoint { Name = "A", Position = new Vector2(0f, 0f) }
            };

            Func<DummyPoint, Vector2> selector = p => p.Position;
            var result = SpaceTraders.UI.MapSelectionMath.FindClosest(points, new Vector2(100f, 100f), 2f, selector);

            Assert.IsNull(result);
        }
    }
}
