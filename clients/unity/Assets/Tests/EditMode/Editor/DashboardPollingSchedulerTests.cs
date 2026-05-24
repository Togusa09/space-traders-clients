using NUnit.Framework;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class DashboardPollingSchedulerTests
    {
        [Test]
        public void ShouldPoll_BeforeInterval_ReturnsFalse()
        {
            var scheduler = new DashboardPollingScheduler(15f);

            var result = scheduler.ShouldPoll(5f, DashboardController.Tab.Contracts);

            Assert.IsFalse(result);
        }

        [Test]
        public void ShouldPoll_AfterInterval_ForPollingTab_ReturnsTrue()
        {
            var scheduler = new DashboardPollingScheduler(15f);

            scheduler.ShouldPoll(10f, DashboardController.Tab.Contracts);
            var result = scheduler.ShouldPoll(6f, DashboardController.Tab.Contracts);

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldPoll_AfterInterval_ForNonPollingTab_ReturnsFalse()
        {
            var scheduler = new DashboardPollingScheduler(15f);

            scheduler.ShouldPoll(10f, DashboardController.Tab.Map);
            var result = scheduler.ShouldPoll(6f, DashboardController.Tab.Map);

            Assert.IsFalse(result);
        }
    }
}
