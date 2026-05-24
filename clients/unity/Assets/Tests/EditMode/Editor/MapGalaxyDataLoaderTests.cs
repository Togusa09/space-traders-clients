using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.UI.Map;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapGalaxyDataLoaderTests
    {
        [Test]
        public void BuildLookup_ReturnsEmptyForNullInput()
        {
            var lookup = MapGalaxyDataLoader.BuildLookup(null);

            Assert.IsNotNull(lookup);
            Assert.AreEqual(0, lookup.Count);
        }

        [Test]
        public void BuildLookup_MapsBySystemSymbol()
        {
            var systems = new List<IndexedSystem>
            {
                new IndexedSystem { Symbol = "X1-AAA" },
                new IndexedSystem { Symbol = "X1-BBB" }
            };

            var lookup = MapGalaxyDataLoader.BuildLookup(systems);

            Assert.IsTrue(lookup.ContainsKey("X1-AAA"));
            Assert.IsTrue(lookup.ContainsKey("X1-BBB"));
        }

        [Test]
        public void BuildJumpGateSystemLinks_DeduplicatesAndSkipsSelfConnections()
        {
            var gates = new List<IndexedJumpGate>
            {
                new IndexedJumpGate
                {
                    SystemSymbol = "X1-AAA",
                    ConnectionsJson = "X1-BBB-WP1,X1-AAA-WP2"
                },
                new IndexedJumpGate
                {
                    SystemSymbol = "X1-BBB",
                    ConnectionsJson = "X1-AAA-WP3"
                }
            };

            var links = MapGalaxyDataLoader.BuildJumpGateSystemLinks(gates);

            Assert.AreEqual(1, links.Count);
            Assert.AreEqual("X1-AAA", links[0].FromSystem);
            Assert.AreEqual("X1-BBB", links[0].ToSystem);
        }

        [Test]
        public void BuildJumpGateSystemLinks_SkipsMalformedConnectionEntries()
        {
            var gates = new List<IndexedJumpGate>
            {
                new IndexedJumpGate
                {
                    SystemSymbol = "X1-AAA",
                    ConnectionsJson = ",,-,X1-BBB-WP1"
                }
            };

            var links = MapGalaxyDataLoader.BuildJumpGateSystemLinks(gates);

            Assert.AreEqual(1, links.Count);
            Assert.AreEqual("X1-BBB", links[0].ToSystem);
        }

        [Test]
        public void BuildJumpGateSystemLinks_ReturnsEmptyWhenGateDataMissing()
        {
            var gates = new List<IndexedJumpGate>
            {
                new IndexedJumpGate { SystemSymbol = null, ConnectionsJson = "X1-BBB-WP1" },
                new IndexedJumpGate { SystemSymbol = "X1-AAA", ConnectionsJson = null },
                null
            };

            var links = MapGalaxyDataLoader.BuildJumpGateSystemLinks(gates);

            Assert.AreEqual(0, links.Count);
        }
    }
}
