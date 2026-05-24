using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.UI.Map;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapListOrchestrationTests
    {
        [Test]
        public void ComputeTotalPages_ReturnsZeroForEmpty()
        {
            Assert.AreEqual(0, MapListOrchestration.ComputeTotalPages(0, 50));
        }

        [Test]
        public void ClampPage_WhenNoPages_ReturnsOne()
        {
            Assert.AreEqual(1, MapListOrchestration.ClampPage(7, 0));
        }

        [Test]
        public void PageItems_ReturnsExpectedSlice()
        {
            var items = new List<int> { 1, 2, 3, 4, 5 };

            var result = MapListOrchestration.PageItems(items, currentPage: 2, pageSize: 2);

            CollectionAssert.AreEqual(new[] { 3, 4 }, result);
        }

        [Test]
        public void TryChangePage_ClampsWithinBounds()
        {
            bool changed = MapListOrchestration.TryChangePage(1, -1, itemCount: 120, pageSize: 50, out var nextPage, out var totalPages);

            Assert.IsTrue(changed);
            Assert.AreEqual(3, totalPages);
            Assert.AreEqual(1, nextPage);
        }

        [Test]
        public void GetTypeChoices_GalaxyAndSystem_Differ()
        {
            var galaxy = MapFilterOptions.GetTypeChoices(galaxyMode: true);
            var system = MapFilterOptions.GetTypeChoices(galaxyMode: false);

            CollectionAssert.Contains(galaxy, "NEUTRON_STAR");
            CollectionAssert.DoesNotContain(galaxy, "PLANET");
            CollectionAssert.Contains(system, "PLANET");
        }
    }
}
