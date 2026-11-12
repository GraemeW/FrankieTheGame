using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class StandardNodeDragManipulatorTests
    {
        private class TestGraphNode : ScriptableObject, IStandardGraphNode
        {
            private Vector2 position;
            public ScriptableObject scriptableObject => this;
            public Vector2 GetPosition() => position;
            public void SetPosition(Vector2 newPosition) => position = newPosition;
        }

        private EditorWindow window;
        private TestGraphNode node;
        private Object previousSelection;

        [SetUp]
        public void SetUp()
        {
            previousSelection = Selection.activeObject; // to restore current selection after script execution
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = previousSelection;
            HeadlessEditorWindow.Close(window);
            if (node != null) { Object.DestroyImmediate(node); }
        }

        [Test]
        public void AddManipulator_SetsTarget_NoPanelRequired()
        {
            var element = new VisualElement();
            node = ScriptableObject.CreateInstance<TestGraphNode>();
            var manipulator = new StandardNodeDragManipulator(element, node, null, null, () => 1f);

            element.AddManipulator(manipulator);

            Assert.AreSame(element, manipulator.target);
        }

        private (StandardNodeDragManipulator manipulator, VisualElement nodeElement) SetUpAttachedManipulator(System.Action onLive = null, System.Action onComplete = null, System.Func<float> zoomProvider = null)
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            var nodeElement = new VisualElement();
            window.rootVisualElement.Add(nodeElement);
            node = ScriptableObject.CreateInstance<TestGraphNode>();
            var manipulator = new StandardNodeDragManipulator(nodeElement, node, onLive, onComplete, zoomProvider ?? (() => 1f));
            window.rootVisualElement.AddManipulator(manipulator);
            return (manipulator, nodeElement);
        }

        [UnityTest]
        public IEnumerator MouseDown_StartsDragAndCapturesMouse()
        {
            SetUpAttachedManipulator();
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));

            Assert.IsTrue(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDown_UnregisteredButton_DoesNotStartDrag()
        {
            // Only left (0) is a registered activator here
            SetUpAttachedManipulator();
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 1);

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_MovesNodeByDeltaDividedByZoom()
        {
            var (_, nodeElement) = SetUpAttachedManipulator(zoomProvider: () => 2f);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(70, 60), delta: new Vector2(20, 10));

            // delta (20,10) / zoom 2 = (10,5)
            Assert.AreEqual(new Vector2(10, 5), node.GetPosition());
            Assert.AreEqual(10f, nodeElement.style.left.value.value, 0.01f);
            Assert.AreEqual(5f, nodeElement.style.top.value.value, 0.01f);
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_InvokesOnPositionChangedLive()
        {
            int liveCallCount = 0;
            SetUpAttachedManipulator(onLive: () => liveCallCount++);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(70, 60), delta: new Vector2(20, 10));

            Assert.AreEqual(1, liveCallCount);
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_ZeroOrNegativeZoom_ClampsToMinimumRatherThanDividingByZero()
        {
            var (_, _) = SetUpAttachedManipulator(zoomProvider: () => 0f);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(51, 50), delta: new Vector2(1, 0));

            // zoom clamped to 0.01f minimum, so a delta of 1 becomes 1/0.01 = 100
            Assert.AreEqual(100f, node.GetPosition().x, 0.5f);
        }

        [UnityTest]
        public IEnumerator MouseUp_MovementBelowThreshold_SelectsNodeAssetInsteadOfCompleting()
        {
            int completeCallCount = 0;
            SetUpAttachedManipulator(onComplete: () => completeCallCount++);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            HeadlessEditorWindow.SendMouseUp(window.rootVisualElement, new Vector2(50, 50)); // same position - a click, not a drag

            Assert.AreSame(node, Selection.activeObject);
            Assert.AreEqual(0, completeCallCount);
        }

        [UnityTest]
        public IEnumerator MouseUp_MovementAboveThreshold_InvokesOnPositionChangedCompleteInsteadOfSelecting()
        {
            int completeCallCount = 0;
            var objectSelectedBeforeTest = Selection.activeObject;
            SetUpAttachedManipulator(onComplete: () => completeCallCount++);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            HeadlessEditorWindow.SendMouseUp(window.rootVisualElement, new Vector2(90, 90)); // well past the 0.25 threshold

            Assert.AreEqual(1, completeCallCount);
            Assert.AreSame(objectSelectedBeforeTest, Selection.activeObject, "Selection should be untouched on a real drag");
        }

        [UnityTest]
        public IEnumerator MouseUp_ReleasesMouseCapture()
        {
            SetUpAttachedManipulator();
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            HeadlessEditorWindow.SendMouseUp(window.rootVisualElement, new Vector2(90, 90));

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }
    }
}
