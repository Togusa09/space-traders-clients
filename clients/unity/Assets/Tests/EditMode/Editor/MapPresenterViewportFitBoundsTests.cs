using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpaceTraders.UI;
using UnityEngine;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterViewportFitBoundsTests
    {
        private static MethodInfo GetFitBoundsMethod()
        {
            var viewportType = typeof(MapPresenter).Assembly.GetType("SpaceTraders.UI.MapViewportMath");
            Assert.NotNull(viewportType, "Expected MapViewportMath type to exist.");

            var method = viewportType.GetMethod("FitBounds", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Expected FitBounds method to exist.");
            return method;
        }

        [Test]
        public void FitBounds_ClampsZoomToMaximum()
        {
            var fitBounds = GetFitBoundsMethod();

            var points = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 1f)
            };
            var rect = new Rect(0f, 0f, 1000f, 1000f);

            var result = fitBounds.Invoke(null, new object[] { points, rect, 0.1f, 2.0f });
            float zoom = GetTupleFloat(result, "Item2", "Zoom");

            Assert.That(zoom, Is.EqualTo(2.0f).Within(0.0001f));
        }

        [Test]
        public void FitBounds_ClampsZoomToMinimum()
        {
            var fitBounds = GetFitBoundsMethod();

            var points = new List<Vector2>
            {
                new Vector2(-5000f, -5000f),
                new Vector2(5000f, 5000f)
            };
            var rect = new Rect(0f, 0f, 100f, 100f);

            var result = fitBounds.Invoke(null, new object[] { points, rect, 0.25f, 100f });
            float zoom = GetTupleFloat(result, "Item2", "Zoom");

            Assert.That(zoom, Is.EqualTo(0.25f).Within(0.0001f));
        }

        private static float GetTupleFloat(object tuple, string propertyName, string fallbackName)
        {
            var prop = tuple.GetType().GetProperty(propertyName) ?? tuple.GetType().GetProperty(fallbackName);
            if (prop != null) return Convert.ToSingle(prop.GetValue(tuple));

            var field = tuple.GetType().GetField(propertyName) ?? tuple.GetType().GetField(fallbackName);
            Assert.NotNull(field, "Expected tuple value to exist.");
            return Convert.ToSingle(field.GetValue(tuple));
        }
    }
}
