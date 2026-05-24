using NUnit.Framework;
using SpaceTraders.UI;
using UnityEngine.UIElements;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPanelBindingsTests
    {
        [Test]
        public void TryCreate_ReturnsFalse_WhenRequiredElementsMissing()
        {
            var panel = new VisualElement();

            bool ok = MapPanelBindings.TryCreate(panel, out var bindings);

            Assert.IsFalse(ok);
            Assert.IsNotNull(bindings);
        }

        [Test]
        public void TryCreate_ReturnsTrue_WhenRequiredElementsPresent()
        {
            var panel = new VisualElement();
            panel.Add(new VisualElement { name = "system-list" });
            panel.Add(new VisualElement { name = "map-container" });
            panel.Add(new VisualElement { name = "waypoints-layer" });

            bool ok = MapPanelBindings.TryCreate(panel, out var bindings);

            Assert.IsTrue(ok);
            Assert.IsNotNull(bindings);
            Assert.IsNotNull(bindings.SystemList);
            Assert.IsNotNull(bindings.MapContainer);
            Assert.IsNotNull(bindings.WaypointsLayer);
        }
    }
}
