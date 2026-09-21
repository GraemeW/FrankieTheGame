using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneTests
    {
        // State
        private Zone zone;
        private bool originalIgnoreFailingMessages;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            originalIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = originalIgnoreFailingMessages;
            foreach (ZoneNode node in zone.GetAllNodes()) { Object.DestroyImmediate(node); }
            Object.DestroyImmediate(zone);
        }
        #endregion

        #region Tests
        [Test]
        public void GetSceneReference_ReturnsSeededValue()
        {
            zone.sceneReference = "MyScene";

            Assert.AreEqual("MyScene", zone.GetSceneReference().SceneName);
        }

        [Test]
        public void IsZoneAudioLooping_DefaultsTrue()
        {
            Assert.IsTrue(zone.IsZoneAudioLooping());
        }

        [Test]
        public void ShouldUpdateMap_DefaultsFalse()
        {
            Assert.IsFalse(zone.ShouldUpdateMap());
        }

        [Test]
        public void GetZoneAudio_ReturnsSeededClip()
        {
            var clip = AudioClip.Create("TestClip", 1, 1, 44100, false);
            zone.zoneAudio = clip;

            Assert.AreSame(clip, zone.GetZoneAudio());

            Object.DestroyImmediate(clip);
        }

        [Test]
        public void CreateRootNodeIfMissing_CreatesSingleNode()
        {
            zone.CreateRootNodeIfMissing();

            Assert.AreEqual(1, zone.GetAllNodes().Count());
            Assert.AreSame(zone.GetRootNode(), zone.GetNodeFromID(zone.GetRootNode().GetNodeID()));
        }

        [Test]
        public void CreateRootNodeIfMissing_CalledTwice_DoesNotDuplicate()
        {
            zone.CreateRootNodeIfMissing();
            zone.CreateRootNodeIfMissing();

            Assert.AreEqual(1, zone.GetAllNodes().Count());
        }

        [Test]
        public void CreateChildNode_AddsRelationAndOffsetsPosition()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();

            zone.CreateChildNode(root);
            ZoneNode child = zone.GetAllNodes().Last();

            Assert.IsTrue(Zone.IsRelated(root, child));
            Assert.AreEqual(new Vector2(480f, 30f), child.GetPosition());
        }

        [Test]
        public void IsRelated_UnrelatedNodes_ReturnsFalse()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            var unrelated = ScriptableObject.CreateInstance<ZoneNode>();
            unrelated.preventLocalizationForTests = true;
            unrelated.name = "unrelated";

            Assert.IsFalse(Zone.IsRelated(root, unrelated));

            Object.DestroyImmediate(unrelated);
        }

        [Test]
        public void ToggleRelation_AddsThenRemovesRelation()
        {
            var parent = ScriptableObject.CreateInstance<ZoneNode>();
            parent.preventLocalizationForTests = true;
            var child = ScriptableObject.CreateInstance<ZoneNode>();
            child.preventLocalizationForTests = true;
            
            parent.name = "parent";
            child.name = "child";

            zone.ToggleRelation(parent, child);
            Assert.IsTrue(Zone.IsRelated(parent, child));

            zone.ToggleRelation(parent, child);
            Assert.IsFalse(Zone.IsRelated(parent, child));

            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(child);
        }

        [Test]
        public void DeleteNode_RemovesFromListAndCleansDanglingChildren()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            zone.CreateChildNode(root);
            ZoneNode child = zone.GetAllNodes().Last();

            LogAssert.ignoreFailingMessages = true;
            zone.DeleteNode(child);
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, zone.GetAllNodes().Count());
            Assert.IsNull(root.GetChildren());
        }

        [Test]
        public void UpdateNodeID_PropagatesIntoChildrenLists()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            zone.CreateChildNode(root);
            ZoneNode child = zone.GetAllNodes().Last();
            string oldID = child.GetNodeID();

            zone.UpdateNodeID(oldID, "manual-new-id");

            CollectionAssert.Contains(root.GetChildren(), "manual-new-id");
            CollectionAssert.DoesNotContain(root.GetChildren(), oldID);
        }

        [Test]
        public void UpdateNodeID_PropagatesIntoGroups()
        {
            ZoneNodeGroup group = zone.CreateZoneNodeGroup(Vector2.zero);
            group.AddNodeID("old-id");

            zone.UpdateNodeID("old-id", "new-id");

            Assert.IsTrue(group.ContainsNodeID("new-id"));
            Assert.IsFalse(group.ContainsNodeID("old-id"));
        }

        [Test]
        public void CreateZoneNodeGroup_UsesDefaultDimensions()
        {
            ZoneNodeGroup group = zone.CreateZoneNodeGroup(new Vector2(10f, 20f));

            Assert.AreEqual(new Rect(10f, 20f, 250f, 100f), group.GetRect());
        }

        [Test]
        public void DeleteGroup_RemovesFromAllGroups()
        {
            ZoneNodeGroup group = zone.CreateZoneNodeGroup(Vector2.zero);
            zone.DeleteGroup(group);

            Assert.IsFalse(zone.GetAllGroups().Contains(group));
        }

        [Test]
        public void SetGroupRect_UpdatesRect()
        {
            ZoneNodeGroup group = zone.CreateZoneNodeGroup(Vector2.zero);
            var newRect = new Rect(5f, 6f, 7f, 8f);

            zone.SetGroupRect(group, newRect);

            Assert.AreEqual(newRect, group.GetRect());
        }

        [Test]
        public void GetLocalizationEntries_IncludesEachNodesEntries()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode root = zone.GetRootNode();
            zone.CreateChildNode(root);

            // One entry for the Zone's own display name, plus one per node (root + child)
            Assert.AreEqual(3, zone.GetLocalizationEntries().Count);
        }
        #endregion
    }
}
