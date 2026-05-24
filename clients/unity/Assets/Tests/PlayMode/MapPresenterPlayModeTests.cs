using NUnit.Framework;
using SpaceTraders.UI;

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
    }
}
