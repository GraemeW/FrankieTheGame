using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneGraphViewTests
    {
        // State
        private EditorWindow window;
        private Zone zone;
        private ZoneGraphView graphView;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            graphView = new ZoneGraphView();
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
            foreach (ZoneNode node in zone.GetAllNodes()) { Object.DestroyImmediate(node); }
            Object.DestroyImmediate(zone);
        }

        private void Attach()
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            window.rootVisualElement.Add(graphView);
        }
        #endregion

        #region Tests
        [Test]
        public void Constructor_BuildsExpectedLayerStructure()
        {
            Assert.IsNotNull(graphView.canvasContent);
            Assert.AreEqual("zone-group-layer", graphView.groupLayer.name);
            Assert.AreEqual("zone-node-layer", graphView.nodeLayer.name);
            Assert.IsTrue(graphView.canvasContent.Contains(graphView.groupLayer));
            Assert.IsTrue(graphView.canvasContent.Contains(graphView.nodeLayer));
            Assert.IsTrue(graphView.canvasContent.Contains(graphView.edgesLayer));
        }

        [Test]
        public void IsRootNode_RootNode_ReturnsTrue()
        {
            zone.CreateRootNodeIfMissing();
            graphView.SetZone(zone);

            Assert.IsTrue(graphView.IsRootNode(zone.GetRootNode()));
        }

        [Test]
        public void IsRootNode_ChildNode_ReturnsFalse()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            zone.CreateChildNode(root);
            ZoneNode child = zone.GetAllNodes().Last();
            graphView.SetZone(zone);

            Assert.IsFalse(graphView.IsRootNode(child));
        }

        [Test]
        public void LinkingState_DefaultsToNotLinking()
        {
            Assert.IsFalse(graphView.isLinking);
            Assert.IsNull(graphView.GetLinkingParentNode());
        }

        [Test]
        public void BeginLinking_SetsLinkingStateToParentNode()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            graphView.SetZone(zone);

            graphView.BeginLinking(root);

            Assert.IsTrue(graphView.isLinking);
            Assert.AreSame(root, graphView.GetLinkingParentNode());
        }

        [Test]
        public void CancelLinking_ResetsLinkingState()
        {
            zone.CreateRootNodeIfMissing();
            graphView.SetZone(zone);
            graphView.BeginLinking(zone.GetRootNode());

            graphView.CancelLinking();

            Assert.IsFalse(graphView.isLinking);
            Assert.IsNull(graphView.GetLinkingParentNode());
        }

        [Test]
        public void CompleteLinking_TogglesRelationAndResetsLinkingState()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            zone.CreateChildNode(root); // CreateChildNode already establishes the relation between root and child
            ZoneNode child = zone.GetAllNodes().Last();
            graphView.SetZone(zone);
            graphView.BeginLinking(root);

            graphView.CompleteLinking(child); // toggles the existing relation off

            Assert.IsFalse(Zone.IsRelated(root, child));
            Assert.IsFalse(graphView.isLinking);
        }

        [Test]
        public void SetZone_RebuildsNodeAndGroupViews()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            ZoneNodeGroup group = zone.CreateZoneNodeGroup(Vector2.zero);
            group.cachedZone = zone; // bypasses the Addressables-backed Zone.GetFromName lookup

            graphView.SetZone(zone);

            Assert.AreEqual(1, graphView.nodeViewLookup.Count);
            Assert.IsTrue(graphView.TryGetZoneNodeView(root.GetNodeID(), out _));
            Assert.AreEqual(1, graphView.groupViewLookup.Count);
            Assert.IsTrue(graphView.groupViewLookup.ContainsKey(group));
        }

        [Test]
        public void SetZone_ResetsLinkingAndPlacingGroupState()
        {
            zone.CreateRootNodeIfMissing();
            graphView.SetZone(zone);
            graphView.BeginLinking(zone.GetRootNode());
            graphView.BeginPlacingGroup();

            graphView.SetZone(zone);

            Assert.IsFalse(graphView.isLinking);
            Assert.IsFalse(graphView.isPlacingGroup);
        }

        [Test]
        public void RequestNodeIDChange_ValidNewID_RenamesNodeAndRebuilds()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            string oldID = root.GetNodeID();
            graphView.SetZone(zone);

            graphView.RequestNodeIDChange(root, "new-root-id");

            Assert.IsFalse(graphView.TryGetZoneNodeView(oldID, out _));
            Assert.IsTrue(graphView.TryGetZoneNodeView("new-root-id", out _));
        }

        [Test]
        public void RequestNodeIDChange_BlankNewID_LeavesIDUnchanged()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            string oldID = root.GetNodeID();
            graphView.SetZone(zone);

            graphView.RequestNodeIDChange(root, "   ");

            Assert.AreEqual(oldID, root.GetNodeID());
        }

        [Test]
        public void RequestNodeIDChange_IDAlreadyTaken_LeavesIDUnchanged()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            zone.CreateChildNode(root);
            ZoneNode child = zone.GetAllNodes().Last();
            string childOldID = child.GetNodeID();
            graphView.SetZone(zone);

            graphView.RequestNodeIDChange(child, root.GetNodeID());

            Assert.AreEqual(childOldID, child.GetNodeID());
        }

        [Test]
        public void RequestCreateChild_AddsChildAndRebuildsNodeViews()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            graphView.SetZone(zone);

            graphView.RequestCreateChild(root);

            Assert.AreEqual(2, graphView.nodeViewLookup.Count);
        }

        [Test]
        public void RequestDelete_RemovesNodeFromLookup()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            zone.CreateChildNode(root);
            ZoneNode child = zone.GetAllNodes().Last();
            string childID = child.GetNodeID();
            graphView.SetZone(zone);

            graphView.RequestDelete(child);

            Assert.IsFalse(graphView.TryGetZoneNodeView(childID, out _));
        }

        [Test]
        public void NotifyNodeMoved_RefreshesEdgesFromCurrentNodeRects()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            zone.CreateChildNode(root);
            ZoneNode child = zone.GetAllNodes().Last();
            graphView.SetZone(zone);

            child.SetPosition(new Vector2(999f, 999f));
            graphView.NotifyNodeMoved();

            Assert.AreEqual(1, graphView.edgesLayer.edges.Count);
            Assert.AreEqual(child.GetRect(), graphView.edgesLayer.edges[0].to);
        }

        [Test]
        public void UpdateGroupsForNode_MembershipReflectsNodesCurrentPosition()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            root.SetPosition(new Vector2(100f, 40f)); // rect (100,40,350,125); checkRect covers x:[187.5,362.5], y:[71.25,133.75]
            ZoneNodeGroup group = zone.CreateZoneNodeGroup(new Vector2(100f, 40f)); // rect (100,40,250,100) - overlaps the check rect above
            group.cachedZone = zone;
            graphView.SetZone(zone);

            graphView.UpdateGroupsForNode(root);

            Assert.IsTrue(group.ContainsNodeID(root.GetNodeID()));
        }

        [UnityTest]
        public IEnumerator BeginPlacingGroup_ThenLeftClick_CreatesGroupAtClickPositionAndResetsFlag()
        {
            zone.CreateRootNodeIfMissing();
            graphView.SetZone(zone);
            Attach();
            yield return null;
            int groupCountBefore = zone.GetAllGroups().Count();

            graphView.BeginPlacingGroup();
            HeadlessEditorWindow.SendMouseDown(graphView, new Vector2(50f, 60f));
            yield return null;

            Assert.AreEqual(groupCountBefore + 1, zone.GetAllGroups().Count());
            Assert.IsFalse(graphView.isPlacingGroup);
        }

        [UnityTest]
        public IEnumerator LeftClick_WithoutBeginPlacingGroup_DoesNotCreateGroup()
        {
            zone.CreateRootNodeIfMissing();
            graphView.SetZone(zone);
            Attach();
            yield return null;
            int groupCountBefore = zone.GetAllGroups().Count();

            HeadlessEditorWindow.SendMouseDown(graphView, new Vector2(50f, 60f));
            yield return null;

            Assert.AreEqual(groupCountBefore, zone.GetAllGroups().Count());
        }

        [Test]
        public void RequestDeleteGroup_RemovesGroupFromZone()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNodeGroup group = zone.CreateZoneNodeGroup(Vector2.zero);
            graphView.SetZone(zone);

            graphView.RequestDeleteGroup(group);

            Assert.IsFalse(zone.GetAllGroups().Contains(group));
        }

        [Test]
        public void SetGroupRect_UpdatesGroupsRect()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNodeGroup group = zone.CreateZoneNodeGroup(Vector2.zero);
            graphView.SetZone(zone);
            var newRect = new Rect(5f, 6f, 7f, 8f);

            graphView.SetGroupRect(group, newRect);

            Assert.AreEqual(newRect, group.GetRect());
        }

        [UnityTest]
        public IEnumerator FocusOnNode_OffsetsCanvasContentUsingResolvedSize()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            graphView.SetZone(zone);
            Attach();
            yield return null;

            graphView.FocusOnNode(root);
            yield return null;

            Vector2 targetPosition = root.GetRect().center;
            var expected = new Vector2(
                -targetPosition.x * graphView.zoomFactor + graphView.resolvedStyle.width * 0.5f,
                -targetPosition.y * graphView.zoomFactor + graphView.resolvedStyle.height * 0.5f);
            Assert.AreEqual(expected.x, graphView.canvasContent.style.left.value.value, 0.5f);
            Assert.AreEqual(expected.y, graphView.canvasContent.style.top.value.value, 0.5f);
        }
        #endregion
    }
}
