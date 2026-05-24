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

        [Test]
        public void FocusSystem_WhenPanelUnavailable_DefersByStoringPendingSymbol()
        {
            var presenterObject = new GameObject("MapPresenterPlayMode");
            var presenter = presenterObject.AddComponent<MapPresenter>();

            presenter.FocusSystem("X1-ALPHA");

            Assert.AreEqual("X1-ALPHA", presenter.GetPendingExternalSystemSymbolForTest());

            Object.DestroyImmediate(presenterObject);
        }

        [Test]
        public void FocusSystemFromWaypoint_WhenPanelUnavailable_DefersUsingParsedSystemSymbol()
        {
            var presenterObject = new GameObject("MapPresenterPlayMode");
            var presenter = presenterObject.AddComponent<MapPresenter>();

            presenter.FocusSystemFromWaypoint("X1-DELTA-A1");

            Assert.AreEqual("X1-DELTA", presenter.GetPendingExternalSystemSymbolForTest());

            Object.DestroyImmediate(presenterObject);
        }

        [Test]
        public void SetupMapPanel_WhenTemplateMissingRequiredElements_AddsErrorLabel()
        {
            var presenterObject = new GameObject("MapPresenterPlayMode");
            var presenter = presenterObject.AddComponent<MapPresenter>();
            var container = new VisualElement();

            presenter.systemPanelTemplate = ScriptableObject.CreateInstance<VisualTreeAsset>();
            presenter.SetupMapPanel(container);

            Assert.AreEqual(1, container.childCount);
            var label = container[0] as Label;
            Assert.NotNull(label);
            Assert.AreEqual("Error: System Panel Template missing required elements.", label.text);

            Object.DestroyImmediate(presenter.systemPanelTemplate);
            Object.DestroyImmediate(presenterObject);
        }
    }
}
