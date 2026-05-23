using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpaceTraders.UI;
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

        private static MethodInfo GetFindClosestMethod()
        {
            var type = typeof(MapPresenter).Assembly.GetType("SpaceTraders.UI.MapSelectionMath");
            Assert.NotNull(type, "Expected MapSelectionMath type to exist.");

            var method = type.GetMethod("FindClosest", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected FindClosest method to exist.");
            return method;
        }

        [Test]
        public void FindClosest_ReturnsNearestWithinThreshold()
        {
            var method = GetFindClosestMethod().MakeGenericMethod(typeof(DummyPoint));
            var points = new List<DummyPoint>
            {
                new DummyPoint { Name = "A", Position = new Vector2(0f, 0f) },
                new DummyPoint { Name = "B", Position = new Vector2(10f, 0f) }
            };

            Func<DummyPoint, Vector2> selector = p => p.Position;
            var result = (DummyPoint)method.Invoke(null, new object[] { points, new Vector2(2f, 0f), 5f, selector });

            Assert.NotNull(result);
            Assert.AreEqual("A", result.Name);
        }

        [Test]
        public void FindClosest_ReturnsNullOutsideThreshold()
        {
            var method = GetFindClosestMethod().MakeGenericMethod(typeof(DummyPoint));
            var points = new List<DummyPoint>
            {
                new DummyPoint { Name = "A", Position = new Vector2(0f, 0f) }
            };

            Func<DummyPoint, Vector2> selector = p => p.Position;
            var result = (DummyPoint)method.Invoke(null, new object[] { points, new Vector2(100f, 100f), 2f, selector });

            Assert.IsNull(result);
        }
    }
}
