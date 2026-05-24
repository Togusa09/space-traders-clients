using NUnit.Framework;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;
using SpaceTraders.UI.Map;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterModeTransitionResolverTests
    {
        [Test]
        public void GetSystemLoadTarget_PrefersSelectedSystemSymbol()
        {
            var result = MapModeTransitionResolver.GetSystemLoadTarget("X1-A", "X1-B", null);

            Assert.AreEqual("X1-A", result);
        }

        [Test]
        public void GetSystemLoadTarget_FallsBackToSelectedSymbol()
        {
            var result = MapModeTransitionResolver.GetSystemLoadTarget(null, "X1-B", null);

            Assert.AreEqual("X1-B", result);
        }

        [Test]
        public void GetSystemLoadTarget_ReturnsNullWhenNoSelection()
        {
            var result = MapModeTransitionResolver.GetSystemLoadTarget(null, null, null);

            Assert.IsNull(result);
        }

        [Test]
        public void GetSystemLoadTarget_ReturnsNullWhenAlreadyLoaded()
        {
            var currentSystem = new SpaceTraders.Generated.Model.System(
                symbol: "X1-A",
                sectorSymbol: "X1",
                type: SystemType.NEUTRONSTAR,
                x: 0,
                y: 0,
                waypoints: new System.Collections.Generic.List<SystemWaypoint>(),
                factions: new System.Collections.Generic.List<SystemFaction>());

            var result = MapModeTransitionResolver.GetSystemLoadTarget("X1-A", "X1-A", currentSystem);

            Assert.IsNull(result);
        }
    }
}
