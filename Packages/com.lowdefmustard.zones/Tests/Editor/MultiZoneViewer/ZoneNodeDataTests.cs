using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneNodeDataTests
    {
        [Test]
        public void Constructor_SetsIDAndPosition_ClearsLinkFields()
        {
            var data = new ZoneNodeData("node1", new Vector2(1f, 2f));

            Assert.AreEqual("node1", data.zoneNodeID);
            Assert.AreEqual(new Vector2(1f, 2f), data.relativePosition);
            Assert.IsFalse(data.HasLink());
        }

        [Test]
        public void SetLink_ThenHasLink_ReturnsTrue()
        {
            var data = new ZoneNodeData("node1", Vector2.zero);
            data.SetLink("otherZone", "otherNode", new Vector2(0.5f, 0.5f));

            Assert.IsTrue(data.HasLink());
            Assert.AreEqual("otherZone", data.linkedZoneName);
            Assert.AreEqual("otherNode", data.linkedZoneNodeID);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), data.linkedRelativePosition);
        }

        [Test]
        public void ClearLink_AfterSetLink_HasLinkReturnsFalse()
        {
            var data = new ZoneNodeData("node1", Vector2.zero);
            data.SetLink("otherZone", "otherNode", Vector2.one);
            data.ClearLink();

            Assert.IsFalse(data.HasLink());
            Assert.AreEqual(string.Empty, data.linkedZoneName);
            Assert.AreEqual(string.Empty, data.linkedZoneNodeID);
            Assert.AreEqual(Vector2.zero, data.linkedRelativePosition);
        }
    }
}
