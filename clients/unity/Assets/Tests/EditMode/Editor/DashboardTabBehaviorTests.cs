using NUnit.Framework;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class DashboardTabBehaviorTests
    {
        [Test]
        public void IsPresenterManagedTab_OnlyMapIsTrue()
        {
            Assert.IsFalse(DashboardTabBehavior.IsPresenterManagedTab(DashboardController.Tab.Agent));
            Assert.IsFalse(DashboardTabBehavior.IsPresenterManagedTab(DashboardController.Tab.Contracts));
            Assert.IsFalse(DashboardTabBehavior.IsPresenterManagedTab(DashboardController.Tab.Fleet));
            Assert.IsTrue(DashboardTabBehavior.IsPresenterManagedTab(DashboardController.Tab.Map));
            Assert.IsFalse(DashboardTabBehavior.IsPresenterManagedTab(DashboardController.Tab.Factions));
        }

        [Test]
        public void ShouldPoll_OnlyDataTabsArePolled()
        {
            Assert.IsFalse(DashboardTabBehavior.ShouldPoll(DashboardController.Tab.Agent));
            Assert.IsTrue(DashboardTabBehavior.ShouldPoll(DashboardController.Tab.Contracts));
            Assert.IsTrue(DashboardTabBehavior.ShouldPoll(DashboardController.Tab.Fleet));
            Assert.IsFalse(DashboardTabBehavior.ShouldPoll(DashboardController.Tab.Map));
            Assert.IsTrue(DashboardTabBehavior.ShouldPoll(DashboardController.Tab.Factions));
        }
    }
}
