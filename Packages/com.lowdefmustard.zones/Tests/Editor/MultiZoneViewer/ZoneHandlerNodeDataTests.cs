using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneHandlerNodeDataTests
    {
        // State
        private ZoneNode zoneNode;

        // Setup
        [SetUp]
        public void SetUp()
        {
            zoneNode = ScriptableObject.CreateInstance<ZoneNode>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(zoneNode);
        }

        // Tests
        [Test]
        public void Constructor_AssignsNodeAndPosition()
        {
            var data = new ZoneHandlerNodeData(zoneNode, new Vector2(3f, 4f));

            Assert.AreSame(zoneNode, data.zoneNode);
            Assert.AreEqual(new Vector2(3f, 4f), data.position);
        }

        [Test]
        public void Position_IsMutableAfterConstruction()
        {
            var data = new ZoneHandlerNodeData(zoneNode, Vector2.zero) { position = new Vector2(5f, 6f) };

            Assert.AreEqual(new Vector2(5f, 6f), data.position);
        }
    }
}
