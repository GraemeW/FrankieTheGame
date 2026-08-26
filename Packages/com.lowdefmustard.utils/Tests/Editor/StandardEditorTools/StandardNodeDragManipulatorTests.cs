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
            if (window != null) { window.Close(); }
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
            window = HeadlessEditorWindowTestHelper.CreateOffscreenWindow();
            var nodeElement = new VisualElement();
            window.rootVisualElement.Add(nodeElement);
            node = ScriptableObject.CreateInstance<TestGraphNode>();
            var manipulator = new StandardNodeDragManipulator(nodeElement, node, onLive, onComplete, zoomProvider ?? (() => 1f));
            window.rootVisualElement.AddManipulator(manipulator);
            return (manipulator, nodeElement);
        }

        private static void SendMouseDown(VisualElement target, Vector2 position, int button = 0)
        {
            using var evt = MouseDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = position, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }

        private static void SendMouseDrag(VisualElement target, Vector2 position, Vector2 delta, int button = 0)
        {
            using var evt = MouseMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = position, delta = delta, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }

        private static void SendMouseUp(VisualElement target, Vector2 position, int button = 0)
        {
            using var evt = MouseUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = position, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }

        [UnityTest]
        public IEnumerator MouseDown_StartsDragAndCapturesMouse()
        {
            SetUpAttachedManipulator();
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;

            Assert.IsTrue(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDown_UnregisteredButton_DoesNotStartDrag()
        {
            // Only left (0) is a registered activator here
            SetUpAttachedManipulator();
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 1);
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_MovesNodeByDeltaDividedByZoom()
        {
            var (_, nodeElement) = SetUpAttachedManipulator(zoomProvider: () => 2f);
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            SendMouseDrag(window.rootVisualElement, new Vector2(70, 60), delta: new Vector2(20, 10));
            yield return null;

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
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            SendMouseDrag(window.rootVisualElement, new Vector2(70, 60), delta: new Vector2(20, 10));
            yield return null;

            Assert.AreEqual(1, liveCallCount);
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_ZeroOrNegativeZoom_ClampsToMinimumRatherThanDividingByZero()
        {
            var (_, _) = SetUpAttachedManipulator(zoomProvider: () => 0f);
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            SendMouseDrag(window.rootVisualElement, new Vector2(51, 50), delta: new Vector2(1, 0));
            yield return null;

            // zoom clamped to 0.01f minimum, so a delta of 1 becomes 1/0.01 = 100
            Assert.AreEqual(100f, node.GetPosition().x, 0.5f);
        }

        [UnityTest]
        public IEnumerator MouseUp_MovementBelowThreshold_SelectsNodeAssetInsteadOfCompleting()
        {
            int completeCallCount = 0;
            SetUpAttachedManipulator(onComplete: () => completeCallCount++);
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            SendMouseUp(window.rootVisualElement, new Vector2(50, 50)); // same position - a click, not a drag
            yield return null;

            Assert.AreSame(node, Selection.activeObject);
            Assert.AreEqual(0, completeCallCount);
        }

        [UnityTest]
        public IEnumerator MouseUp_MovementAboveThreshold_InvokesOnPositionChangedCompleteInsteadOfSelecting()
        {
            int completeCallCount = 0;
            var objectSelectedBeforeTest = Selection.activeObject;
            SetUpAttachedManipulator(onComplete: () => completeCallCount++);
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            SendMouseUp(window.rootVisualElement, new Vector2(90, 90)); // well past the 0.25 threshold
            yield return null;

            Assert.AreEqual(1, completeCallCount);
            Assert.AreSame(objectSelectedBeforeTest, Selection.activeObject, "Selection should be untouched on a real drag");
        }

        [UnityTest]
        public IEnumerator MouseUp_ReleasesMouseCapture()
        {
            SetUpAttachedManipulator();
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            SendMouseUp(window.rootVisualElement, new Vector2(90, 90));
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }
    }
}
