using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneNodeViewTests
    {
        // State
        private EditorWindow window;
        private ZoneNode zoneNode;
        private Zone zone;
        private TestZoneGraphView graphView;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            zoneNode = ScriptableObject.CreateInstance<ZoneNode>();
            zoneNode.preventLocalizationForTests = true;
            zoneNode.name = "node-1";
            graphView = new TestZoneGraphView();
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
            Object.DestroyImmediate(zoneNode);
            Object.DestroyImmediate(zone);
        }
        #endregion
        
        #region PrivateMethods
        private (ZoneNodeView view, Button linkButton, Button addButton, Button deleteButton) CreateAttachedView()
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            var view = new ZoneNodeView(zoneNode, zone, graphView);
            window.rootVisualElement.Add(view);

            Button linkButton = QueryLinkButton(view);
            Button addButton = QueryAddButton(view);
            Button deleteButton = QueryDeleteButton(view);
            return (view, linkButton, addButton, deleteButton);
        }
        
        private static Button QueryLinkButton(VisualElement view) => view.Q<Button>("linkButton");
        private static Button QueryAddButton(VisualElement view) => view.Q<Button>("addButton");
        private static Button QueryDeleteButton(ZoneNodeView view) => view.Q<Button>("removeButton");
        #endregion

        #region Tests
        [Test]
        public void Constructor_NullZoneNode_DoesNotThrowOrAddChildren()
        {
            var view = new ZoneNodeView(null, zone, graphView);

            Assert.AreEqual(0, view.childCount);
        }

        [Test]
        public void Constructor_NullZoneGraphView_DoesNotThrowOrAddChildren()
        {
            var view = new ZoneNodeView(zoneNode, zone, null);

            Assert.AreEqual(0, view.childCount);
        }

        [Test]
        public void Constructor_SetsStyleFromNodeRect()
        {
            zoneNode.SetPosition(new Vector2(12f, 34f));
            var view = new ZoneNodeView(zoneNode, zone, graphView);

            Assert.AreEqual(12f, view.style.left.value.value);
            Assert.AreEqual(34f, view.style.top.value.value);
            Assert.AreEqual(zoneNode.GetRect().width, view.style.width.value.value);
            Assert.AreEqual(zoneNode.GetRect().height, view.style.height.value.value);
        }

        [Test]
        public void Constructor_IsRootNode_OmitsDeleteButton()
        {
            graphView.isRootNodeResult = true;

            var view = new ZoneNodeView(zoneNode, zone, graphView);

            Button deleteButton = QueryDeleteButton(view);
            Assert.IsNull(deleteButton);
        }

        [Test]
        public void Constructor_NotRootNode_IncludesDeleteButton()
        {
            graphView.isRootNodeResult = false;

            var view = new ZoneNodeView(zoneNode, zone, graphView);

            Button deleteButton = QueryDeleteButton(view);
            Assert.IsNotNull(deleteButton);
        }

        [Test]
        public void RefreshLinkButton_NotLinking_ShowsLinkText()
        {
            graphView.isLinking = false;
            var view = new ZoneNodeView(zoneNode, zone, graphView);

            view.RefreshLinkButton();

            Assert.AreEqual("link", QueryLinkButton(view).text);
        }

        [Test]
        public void RefreshLinkButton_LinkingFromThisNode_ShowsCancelText()
        {
            graphView.isLinking = true;
            graphView.linkingParentNode = zoneNode;
            var view = new ZoneNodeView(zoneNode, zone, graphView);

            view.RefreshLinkButton();

            Assert.AreEqual("---", QueryLinkButton(view).text);
        }

        [Test]
        public void RefreshLinkButton_LinkingFromUnrelatedNode_ShowsChildText()
        {
            var otherNode = ScriptableObject.CreateInstance<ZoneNode>();
            otherNode.preventLocalizationForTests = true;
            
            otherNode.name = "other-node";
            graphView.isLinking = true;
            graphView.linkingParentNode = otherNode;
            var view = new ZoneNodeView(zoneNode, zone, graphView);

            view.RefreshLinkButton();

            Assert.AreEqual("child", QueryLinkButton(view).text);

            Object.DestroyImmediate(otherNode);
        }

        [Test]
        public void RefreshLinkButton_LinkingFromRelatedNode_ShowsUnlinkText()
        {
            var parentNode = ScriptableObject.CreateInstance<ZoneNode>();
            parentNode.preventLocalizationForTests = true;
            
            parentNode.name = "parent-node";
            parentNode.AddChild(zoneNode.GetNodeID());
            graphView.isLinking = true;
            graphView.linkingParentNode = parentNode;
            var view = new ZoneNodeView(zoneNode, zone, graphView);

            view.RefreshLinkButton();

            Assert.AreEqual("unlink", QueryLinkButton(view).text);

            Object.DestroyImmediate(parentNode);
        }

        [Test]
        public void ManualMoveZoneNode_MovesNodeAndUpdatesStyle()
        {
            zoneNode.SetPosition(new Vector2(10f, 10f));
            var view = new ZoneNodeView(zoneNode, zone, graphView);

            view.ManualMoveZoneNode(new Vector2(5f, -2f));

            Assert.AreEqual(new Vector2(15f, 8f), zoneNode.GetPosition());
            Assert.AreEqual(15f, view.style.left.value.value);
            Assert.AreEqual(8f, view.style.top.value.value);
        }

        [UnityTest]
        public IEnumerator LinkButtonClick_NotLinking_CallsBeginLinking()
        {
            (_, Button linkButton, _, _) = CreateAttachedView();
            yield return null;

            HeadlessEditorWindow.SendClick(linkButton);
            yield return null;

            Assert.AreSame(zoneNode, graphView.beginLinkingNode);
        }

        [UnityTest]
        public IEnumerator LinkButtonClick_LinkingFromSelf_CallsCancelLinking()
        {
            graphView.isLinking = true;
            graphView.linkingParentNode = zoneNode;
            (_, Button linkButton, _, _) = CreateAttachedView();
            yield return null;

            HeadlessEditorWindow.SendClick(linkButton);
            yield return null;

            Assert.AreEqual(1, graphView.cancelLinkingCallCount);
        }

        [UnityTest]
        public IEnumerator LinkButtonClick_LinkingFromOtherNode_CallsCompleteLinking()
        {
            var otherNode = ScriptableObject.CreateInstance<ZoneNode>();
            otherNode.preventLocalizationForTests = true;
            
            otherNode.name = "other-node";
            graphView.isLinking = true;
            graphView.linkingParentNode = otherNode;
            (_, Button linkButton, _, _) = CreateAttachedView();
            yield return null;

            HeadlessEditorWindow.SendClick(linkButton);
            yield return null;

            Assert.AreSame(zoneNode, graphView.completeLinkingNode);

            Object.DestroyImmediate(otherNode);
        }

        [UnityTest]
        public IEnumerator AddButtonClick_CallsRequestCreateChild()
        {
            (_, _, Button addButton, _) = CreateAttachedView();
            yield return null;

            HeadlessEditorWindow.SendClick(addButton);
            yield return null;

            Assert.AreSame(zoneNode, graphView.requestCreateChildNode);
        }

        [UnityTest]
        public IEnumerator DeleteButtonClick_CallsRequestDelete()
        {
            graphView.isRootNodeResult = false;
            (_, _, _, Button deleteButton) = CreateAttachedView();
            yield return null;

            HeadlessEditorWindow.SendClick(deleteButton);
            yield return null;

            Assert.AreSame(zoneNode, graphView.requestDeleteNode);
        }

        [UnityTest]
        public IEnumerator OverrideIDField_ChangedToNewValue_CallsRequestNodeIDChange()
        {
            (ZoneNodeView view, _, _, _) = CreateAttachedView();
            yield return null;

            var idField = view.Q<TextField>();
            idField.value = "new-id"; // TextField's own value setter dispatches a real ChangeEvent through the panel
            yield return null;

            Assert.AreSame(zoneNode, graphView.requestNodeIDChangeNode);
            Assert.AreEqual("new-id", graphView.requestNodeIDChangeNewID);
        }

        [UnityTest]
        public IEnumerator OverrideIDField_ChangedToSameValue_DoesNotCallRequestNodeIDChange()
        {
            (ZoneNodeView view, _, _, _) = CreateAttachedView();
            yield return null;

            var idField = view.Q<TextField>();
            idField.value = zoneNode.GetNodeID(); // Setting to its own already-current value
            yield return null;

            Assert.IsNull(graphView.requestNodeIDChangeNode);
        }
        #endregion
    }
}
