using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneNodeTests
    {
        // State
        private ZoneNode node;
        private ZoneNode otherNode;
        private bool originalIgnoreFailingMessages;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = originalIgnoreFailingMessages;
            if (node != null) { Object.DestroyImmediate(node); }
            if (otherNode != null) { Object.DestroyImmediate(otherNode); }
        }
        #endregion
        
        #region PrivateMethods
        private ZoneNode CreateNode(bool isOtherNode = false)
        {
            if (isOtherNode)
            {
                otherNode = ScriptableObject.CreateInstance<ZoneNode>();
                otherNode.preventLocalizationForTests = true;
                return otherNode;
            }

            node = ScriptableObject.CreateInstance<ZoneNode>();
            node.preventLocalizationForTests = true;
            return node;
        }
        #endregion

        #region Tests
        [Test]
        public void GetNodeID_ReturnsObjectName()
        {
            CreateNode();
            node.name = "node-1";

            Assert.AreEqual("node-1", node.GetNodeID());
        }

        [Test]
        public void GetChildren_ReturnsNull_WhenEmpty()
        {
            CreateNode();

            Assert.IsNull(node.GetChildren());
        }

        [Test]
        public void AddChild_ThenGetChildren_ContainsChildID()
        {
            CreateNode();
            node.AddChild("child-1");

            CollectionAssert.Contains(node.GetChildren(), "child-1");
        }

        [Test]
        public void RemoveChild_RemovesChildID()
        {
            CreateNode();
            node.AddChild("child-1");
            node.RemoveChild("child-1");

            Assert.IsNull(node.GetChildren());
        }

        [Test]
        public void UpdateChildNodeID_ReplacesMatchingID()
        {
            CreateNode();
            node.AddChild("old-id");
            node.UpdateChildNodeID("old-id", "new-id");

            CollectionAssert.Contains(node.GetChildren(), "new-id");
            CollectionAssert.DoesNotContain(node.GetChildren(), "old-id");
        }

        [Test]
        public void UpdateChildNodeID_NoMatchingID_LeavesChildrenUnchanged()
        {
            CreateNode();
            node.AddChild("existing-id");
            node.UpdateChildNodeID("missing-id", "new-id");

            CollectionAssert.Contains(node.GetChildren(), "existing-id");
            Assert.AreEqual(1, node.GetChildren().Count);
        }

        [Test]
        public void GetPosition_ReturnsRectPosition()
        {
            CreateNode();

            Assert.AreEqual(new Vector2(30f, 30f), node.GetPosition());
        }

        [Test]
        public void SetPosition_ThenGetPosition_ReturnsUpdatedPosition()
        {
            CreateNode();
            node.SetPosition(new Vector2(5f, 6f));

            Assert.AreEqual(new Vector2(5f, 6f), node.GetPosition());
        }

        [Test]
        public void Initialize_SetsRectWidthAndHeight()
        {
            CreateNode();
            node.Initialize(200, 80);

            Rect rect = node.GetRect();
            Assert.AreEqual(200f, rect.width);
            Assert.AreEqual(80f, rect.height);
        }

        [Test]
        public void SetZoneName_UpdatesZoneName()
        {
            CreateNode();

            LogAssert.ignoreFailingMessages = true;
            node.SetZoneName("MyZone");
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual("MyZone", node.GetZoneName());
        }

        [Test]
        public void SetZoneName_SameValue_IsNoOp()
        {
            CreateNode();
            
            LogAssert.ignoreFailingMessages = true;
            node.SetZoneName("MyZone");
            LogAssert.ignoreFailingMessages = false;

            // A same-value call returns early before touching the localization bridge, so no warning
            node.SetZoneName("MyZone");

            Assert.AreEqual("MyZone", node.GetZoneName());
        }

        [Test]
        public void SetNodeID_UpdatesName_ReturnsTrue()
        {
            CreateNode();

            LogAssert.ignoreFailingMessages = true;
            bool result = node.SetNodeID("new-id");
            LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(result);
            Assert.AreEqual("new-id", node.GetNodeID());
        }

        [Test]
        public void SetNodeID_SameID_ReturnsFalse()
        {
            CreateNode();
            node.name = "same-id";

            bool result = node.SetNodeID("same-id");

            Assert.IsFalse(result);
        }

        [Test]
        public void TrySetExternalLink_ToDifferentZone_Succeeds()
        {
            CreateNode();
            CreateNode(true);
            
            LogAssert.ignoreFailingMessages = true;
            node.SetZoneName("ZoneA");
            otherNode.SetZoneName("ZoneB");
            LogAssert.ignoreFailingMessages = false;

            bool result = node.TrySetExternalLink(otherNode);

            Assert.IsTrue(result);
            Assert.AreSame(otherNode, node.GetLinkedZoneNode());
        }

        [Test]
        public void TrySetExternalLink_ToSelf_Fails()
        {
            CreateNode();

            bool result = node.TrySetExternalLink(node);

            Assert.IsFalse(result);
            Assert.IsNull(node.GetLinkedZoneNode());
        }

        [Test]
        public void TrySetExternalLink_ToNull_Fails()
        {
            CreateNode();

            bool result = node.TrySetExternalLink(null);

            Assert.IsFalse(result);
        }

        [Test]
        public void TrySetExternalLink_ToSameZone_Fails()
        {
            CreateNode();
            CreateNode(true);
            
            LogAssert.ignoreFailingMessages = true;
            node.SetZoneName("ZoneA");
            otherNode.SetZoneName("ZoneA");
            LogAssert.ignoreFailingMessages = false;

            bool result = node.TrySetExternalLink(otherNode);

            Assert.IsFalse(result);
        }

        [Test]
        public void ClearExternalLink_WithExistingLink_Succeeds()
        {
            CreateNode();
            CreateNode(true);
            
            LogAssert.ignoreFailingMessages = true;
            node.SetZoneName("ZoneA");
            otherNode.SetZoneName("ZoneB");
            LogAssert.ignoreFailingMessages = false;
            node.TrySetExternalLink(otherNode);

            bool result = node.ClearExternalLink();

            Assert.IsTrue(result);
            Assert.IsNull(node.GetLinkedZoneNode());
        }

        [Test]
        public void ClearExternalLink_WithNoLink_ReturnsFalse()
        {
            CreateNode();

            bool result = node.ClearExternalLink();

            Assert.IsFalse(result);
        }

        [Test]
        public void HasLinkedSceneReference_NoLink_ReturnsFalse()
        {
            CreateNode();

            Assert.IsFalse(node.HasLinkedSceneReference());
        }

        [Test]
        public void GetLocalizationEntries_UnconfiguredNode_ReturnsSingleEntry()
        {
            CreateNode();
            node.SetNodeID("new-id"); // Need to set ID for localization to populate
            
            Assert.AreEqual(1, node.GetLocalizationEntries().Count);
        }
        #endregion
    }
}
