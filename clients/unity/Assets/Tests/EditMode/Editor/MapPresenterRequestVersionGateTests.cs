using NUnit.Framework;
using SpaceTraders.UI;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class MapPresenterRequestVersionGateTests
    {
        [Test]
        public void Begin_IncrementsRequestVersion()
        {
            var gate = new MapRequestVersionGate();

            var first = gate.Begin();
            var second = gate.Begin();

            Assert.AreEqual(1, first);
            Assert.AreEqual(2, second);
        }

        [Test]
        public void IsCurrent_OnlyLatestVersionReturnsTrue()
        {
            var gate = new MapRequestVersionGate();

            var first = gate.Begin();
            var second = gate.Begin();

            Assert.IsFalse(gate.IsCurrent(first));
            Assert.IsTrue(gate.IsCurrent(second));
        }
    }
}
