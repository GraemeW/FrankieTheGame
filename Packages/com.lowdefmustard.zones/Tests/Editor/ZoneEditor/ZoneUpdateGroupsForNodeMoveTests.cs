using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneUpdateGroupsForNodeMoveTests
    {
        // State
        private Zone zone;
        private ZoneNode node;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            zone.CreateRootNodeIfMissing();
            node = zone.GetRootNode();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ZoneNode zoneNode in zone.GetAllNodes()) { Object.DestroyImmediate(zoneNode); }
            Object.DestroyImmediate(zone);
        }
        #endregion

        #region Tests
        [Test]
        public void UpdateGroupsForNodeMove_GroupNoLongerOverlapping_RemovesNodeFromGroup()
        {
            node.SetPosition(Vector2.zero); // rect becomes (0, 0, 350, 125); checkRect covers roughly x:[87.5,262.5], y:[31.25,93.75]

            ZoneNodeGroup group = zone.CreateZoneNodeGroup(new Vector2(1000f, 1000f)); // far away - never overlaps the check rect
            group.AddNodeID(node.GetNodeID());

            zone.UpdateGroupsForNodeMove(node);

            Assert.IsFalse(group.ContainsNodeID(node.GetNodeID()));
        }

        [Test]
        public void UpdateGroupsForNodeMove_GroupNowOverlapping_AddsNodeToGroup()
        {
            node.SetPosition(Vector2.zero); // checkRect covers roughly x:[87.5,262.5], y:[31.25,93.75]

            ZoneNodeGroup group = zone.CreateZoneNodeGroup(new Vector2(100f, 40f)); // squarely inside the check rect

            zone.UpdateGroupsForNodeMove(node);

            Assert.IsTrue(group.ContainsNodeID(node.GetNodeID()));
        }

        [Test]
        public void UpdateGroupsForNodeMove_MultipleOverlappingGroups_OnlyAddsToFirstThenStops()
        {
            node.SetPosition(Vector2.zero);

            ZoneNodeGroup firstGroup = zone.CreateZoneNodeGroup(new Vector2(100f, 40f));
            ZoneNodeGroup secondGroup = zone.CreateZoneNodeGroup(new Vector2(110f, 45f)); // also overlapping

            zone.UpdateGroupsForNodeMove(node);

            Assert.IsTrue(firstGroup.ContainsNodeID(node.GetNodeID()));
            Assert.IsFalse(secondGroup.ContainsNodeID(node.GetNodeID()));
        }

        [Test]
        public void UpdateGroupsForNodeMove_AlreadyCorrectlyGrouped_LeavesMembershipUnchanged()
        {
            node.SetPosition(Vector2.zero);
            ZoneNodeGroup group = zone.CreateZoneNodeGroup(new Vector2(100f, 40f));
            group.AddNodeID(node.GetNodeID());

            zone.UpdateGroupsForNodeMove(node);

            Assert.IsTrue(group.ContainsNodeID(node.GetNodeID()));
        }
        #endregion
    }
}
