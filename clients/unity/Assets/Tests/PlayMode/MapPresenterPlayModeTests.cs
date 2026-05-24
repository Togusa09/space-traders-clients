using NUnit.Framework;
using SpaceTraders.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceTraders.Tests.PlayMode
{
    public class MapPresenterPlayModeTests
    {
        [Test]
        public void GetSystemSymbolFromWaypoint_ParsesWaypointPrefix()
        {
            Assert.AreEqual("X1-DF55", MapPresenter.GetSystemSymbolFromWaypoint("X1-DF55-A1"));
            Assert.AreEqual("X1-DF55", MapPresenter.GetSystemSymbolFromWaypoint("X1-DF55"));
            Assert.IsNull(MapPresenter.GetSystemSymbolFromWaypoint(null));
            Assert.IsNull(MapPresenter.GetSystemSymbolFromWaypoint(""));
        }

        [Test]
        public void SetupMapPanel_WhenTemplateMissing_AddsErrorLabel()
        {
            var presenterObject = new GameObject("MapPresenterPlayMode");
            var presenter = presenterObject.AddComponent<MapPresenter>();
            var container = new VisualElement();

            presenter.SetupMapPanel(container);

            Assert.AreEqual(1, container.childCount);
            var label = container[0] as Label;
            Assert.NotNull(label);
            Assert.AreEqual("Error: System Panel Template missing.", label.text);

            Object.DestroyImmediate(presenterObject);
        }
    }
}
