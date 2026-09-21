using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneNodeGroupViewTests
    {
        // State
        private EditorWindow window;
        private ZoneNodeGroup group;
        private TestZoneGraphView graphView;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            group = new ZoneNodeGroup("ZoneA");
            graphView = new TestZoneGraphView();
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
        }
        #endregion

        #region PrivateMethods
        private ZoneNodeGroupView CreateAttachedView()
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            var view = new ZoneNodeGroupView(group, graphView);
            window.rootVisualElement.Add(view);
            return view;
        }
        #endregion

        #region Tests
        [Test]
        public void Constructor_NullGroup_DoesNotThrowOrAddChildren()
        {
            var view = new ZoneNodeGroupView(null, graphView);

            Assert.AreEqual(0, view.childCount);
        }

        [Test]
        public void Constructor_NullZoneGraphView_DoesNotThrowOrAddChildren()
        {
            var view = new ZoneNodeGroupView(group, null);

            Assert.AreEqual(0, view.childCount);
        }

        [Test]
        public void Constructor_AppliesRectFromGroupData()
        {
            group.SetRect(new Rect(10f, 20f, 30f, 40f));

            var view = new ZoneNodeGroupView(group, graphView);

            Assert.AreEqual(10f, view.style.left.value.value);
            Assert.AreEqual(20f, view.style.top.value.value);
            Assert.AreEqual(30f, view.style.width.value.value);
            Assert.AreEqual(40f, view.style.height.value.value);
        }

        [Test]
        public void ApplyRectFromData_ReflectsRectChangedAfterConstruction()
        {
            var view = new ZoneNodeGroupView(group, graphView);
            group.SetRect(new Rect(1f, 2f, 3f, 4f));

            view.ApplyRectFromData();

            Assert.AreEqual(1f, view.style.left.value.value);
            Assert.AreEqual(2f, view.style.top.value.value);
        }

        [UnityTest]
        public IEnumerator DeleteButtonClick_CallsRequestDeleteGroup()
        {
            ZoneNodeGroupView view = CreateAttachedView();
            yield return null;

            var deleteButton = view.Q<Button>();
            HeadlessEditorWindow.SendClick(deleteButton);
            yield return null;

            Assert.AreSame(group, graphView.requestDeleteGroupArg);
        }

        [UnityTest]
        public IEnumerator DoubleClickHeader_EnablesNameFieldEditing()
        {
            ZoneNodeGroupView view = CreateAttachedView();
            yield return null;

            var header = view.Q<VisualElement>(name: "zone-group-header");
            HeadlessEditorWindow.SendMouseDown(header, header.worldBound.center, clickCount: 2);
            yield return null;

            var nameField = view.Q<TextField>();
            Assert.IsTrue(nameField.enabledSelf);
        }

        [UnityTest]
        public IEnumerator SingleClickHeader_DoesNotEnableNameFieldEditing()
        {
            ZoneNodeGroupView view = CreateAttachedView();
            yield return null;

            var header = view.Q<VisualElement>(name: "zone-group-header");
            HeadlessEditorWindow.SendMouseDown(header, header.worldBound.center, clickCount: 1);
            yield return null;

            var nameField = view.Q<TextField>();
            Assert.IsFalse(nameField.enabledSelf);
        }

        [UnityTest]
        public IEnumerator NameFieldFocusOut_DisablesNameFieldEditing()
        {
            ZoneNodeGroupView view = CreateAttachedView();
            yield return null;
            var header = view.Q<VisualElement>(name: "zone-group-header");
            HeadlessEditorWindow.SendMouseDown(header, header.worldBound.center, clickCount: 2);
            yield return null;

            var nameField = view.Q<TextField>();
            // Use .Focus()/.Blur() methods to focus out (N.B. cannot send FocusOut event directly)
            nameField.Focus();
            yield return null;
            nameField.Blur();
            yield return null;

            Assert.IsFalse(nameField.enabledSelf);
        }

        [UnityTest]
        public IEnumerator NameFieldChanged_CallsSetZoneNodeGroupName()
        {
            ZoneNodeGroupView view = CreateAttachedView();
            yield return null;

            var nameField = view.Q<TextField>();
            nameField.value = "New Group Name";
            yield return null;

            Assert.AreEqual("New Group Name", group.GetZoneNodeGroupName());
        }

        [UnityTest]
        public IEnumerator HeaderDrag_MovesGroupRectAndNotifiesGraphView()
        {
            group.SetRect(new Rect(0f, 0f, 250f, 100f));
            var view = CreateAttachedView();
            yield return null;

            var header = view.Q<VisualElement>(name: "zone-group-header");
            HeadlessEditorWindow.SendMouseDown(header, header.worldBound.center);
            yield return null;
            HeadlessEditorWindow.SendMouseDrag(header, header.worldBound.center + new Vector2(20, 10), delta: new Vector2(20, 10));
            yield return null;

            Assert.AreSame(group, graphView.setGroupRectGroupArg);
            Assert.AreEqual(new Vector2(20, 10), graphView.setGroupRectRectArg.position);
            Assert.AreEqual(1, graphView.notifyNodeMovedCallCount);
        }

        [UnityTest]
        public IEnumerator HeaderDrag_MovesContainedNodeViews()
        {
            var nestedNode = ScriptableObject.CreateInstance<ZoneNode>();
            nestedNode.preventLocalizationForTests = true;
            nestedNode.name = "nested-node";
            nestedNode.SetPosition(new Vector2(100f, 100f));
            
            var throwawayZone = ScriptableObject.CreateInstance<Zone>();
            throwawayZone.preventLocalizationForTests = true;
            
            var nestedNodeView = new ZoneNodeView(nestedNode, throwawayZone, new TestZoneGraphView());
            group.AddNodeID(nestedNode.GetNodeID());
            graphView.nodeViewsByID[nestedNode.GetNodeID()] = nestedNodeView;

            ZoneNodeGroupView view = CreateAttachedView();
            yield return null;

            var header = view.Q<VisualElement>(name: "zone-group-header");
            HeadlessEditorWindow.SendMouseDown(header, header.worldBound.center);
            yield return null;
            HeadlessEditorWindow.SendMouseDrag(header, header.worldBound.center + new Vector2(5, 7), delta: new Vector2(5, 7));
            yield return null;

            Assert.AreEqual(new Vector2(105f, 107f), nestedNode.GetPosition());

            Object.DestroyImmediate(nestedNode);
            Object.DestroyImmediate(throwawayZone);
        }
        #endregion
    }
}
