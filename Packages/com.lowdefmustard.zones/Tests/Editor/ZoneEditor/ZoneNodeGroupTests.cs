using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneNodeGroupTests
    {
        // State
        private readonly TestObjectTracker tracker = new();
        private Zone zone;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void Constructor_SetsDefaultGroupName()
        {
            var group = new ZoneNodeGroup("SomeZone");

            Assert.AreEqual("--Zone Group--", group.GetZoneNodeGroupName());
        }

        [Test]
        public void GetZoneNodeGroupName_FallsBackToDefault_WhenSetToNull()
        {
            var group = new ZoneNodeGroup("SomeZone");
            group.SetZoneNodeGroupName(null);

            Assert.AreEqual("--Zone Group--", group.GetZoneNodeGroupName());
        }

        [Test]
        public void SetRect_ThenGetRect_ReturnsSameRect()
        {
            var group = new ZoneNodeGroup("SomeZone");
            var rect = new Rect(1f, 2f, 3f, 4f);
            group.SetRect(rect);

            Assert.AreEqual(rect, group.GetRect());
        }

        [Test]
        public void AddNodeID_Twice_DoesNotDuplicate()
        {
            var group = new ZoneNodeGroup("SomeZone");
            group.AddNodeID("node1");
            group.AddNodeID("node1");

            Assert.AreEqual(1, group.GetContainedNodeIDs().Count);
            Assert.IsTrue(group.ContainsNodeID("node1"));
        }

        [Test]
        public void RemoveNodeID_RemovesContainedID()
        {
            var group = new ZoneNodeGroup("SomeZone");
            group.AddNodeID("node1");
            group.RemoveNodeID("node1");

            Assert.IsFalse(group.ContainsNodeID("node1"));
        }

        [Test]
        public void RecomputeGroupRect_EmptyContainedSet_DoesNotThrow_NeverTouchesCachedZone()
        {
            var group = new ZoneNodeGroup("SomeZone");
            var originalRect = new Rect(1f, 2f, 3f, 4f);
            group.SetRect(originalRect);

            group.RecomputeGroupRect();

            Assert.AreEqual(originalRect, group.GetRect());
        }

        [Test]
        public void RecomputeGroupRect_SingleContainedNode_ProducesPaddedBounds()
        {
            zone = tracker.Create<Zone>();
            zone.preventLocalizationForTests = true;
            zone.CreateRootNodeIfMissing();
            ZoneNode rootNode = zone.GetRootNode();
            tracker.Track(rootNode);

            var group = new ZoneNodeGroup("SomeZone");
            group.AddNodeID(rootNode.GetNodeID());
            group.cachedZone = zone;

            group.RecomputeGroupRect();

            // Root node rect defaults to (30, 30, 350, 125); padding is 20 on all sides plus a 25 header offset on top
            var expected = new Rect(10f, -15f, 390f, 190f);
            Assert.AreEqual(expected, group.GetRect());
        }
        #endregion
    }
}
