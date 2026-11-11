using NUnit.Framework;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    public class ZoneHandlerKeyTests
    {
        [Test]
        public void Equals_SameZoneAndHandler_IsEqualWithSameHash()
        {
            var a = new ZoneHandlerKey("Zone", "handler-1");
            var b = new ZoneHandlerKey("Zone", "handler-1");

            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Equals_DifferentZoneOrHandler_IsNotEqual()
        {
            var key = new ZoneHandlerKey("Zone", "handler-1");

            Assert.IsFalse(key.Equals(new ZoneHandlerKey("OtherZone", "handler-1")));
            Assert.IsFalse(key.Equals(new ZoneHandlerKey("Zone", "handler-2")));
        }

        [Test]
        public void Constructor_NullZoneName_MatchesEmptyZoneName()
        {
            Assert.IsTrue(new ZoneHandlerKey(null, "handler-1").Equals(new ZoneHandlerKey("", "handler-1")));
        }
    }
}
