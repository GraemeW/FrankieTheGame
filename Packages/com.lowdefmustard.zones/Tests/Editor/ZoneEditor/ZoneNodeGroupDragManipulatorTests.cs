using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneNodeGroupDragManipulatorTests
    {
        // State
        private EditorWindow window;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
        }
        #endregion

        #region PrivateMethods
        private ZoneNodeGroupDragManipulator SetUpAttachedManipulator(System.Action<Vector2> shiftPosition, System.Func<float> zoomProvider = null)
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            var manipulator = new ZoneNodeGroupDragManipulator(shiftPosition, zoomProvider ?? (() => 1f));
            window.rootVisualElement.AddManipulator(manipulator);
            return manipulator;
        }

        private static void SendMouseDown(VisualElement target, Vector2 position, int button = 0)
        {
            using MouseDownEvent evt = MouseDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = position, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }

        private static void SendMouseDrag(VisualElement target, Vector2 position, Vector2 delta, int button = 0)
        {
            using MouseMoveEvent evt = MouseMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = position, delta = delta, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }

        private static void SendMouseUp(VisualElement target, Vector2 position, int button = 0)
        {
            using MouseUpEvent evt = MouseUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = position, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }
        #endregion

        #region Tests
        [Test]
        public void AddManipulator_SetsTarget_NoPanelRequired()
        {
            var element = new VisualElement();
            var manipulator = new ZoneNodeGroupDragManipulator(_ => { }, () => 1f);

            element.AddManipulator(manipulator);

            Assert.AreSame(element, manipulator.target);
        }
        
        [UnityTest]
        public IEnumerator MouseDown_StartsDragAndCapturesMouse()
        {
            SetUpAttachedManipulator(_ => { });
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;

            Assert.IsTrue(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDown_UnregisteredButton_DoesNotStartDrag()
        {
            SetUpAttachedManipulator(_ => { });
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 1);
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_InvokesShiftPositionWithDeltaDividedByZoom()
        {
            Vector2? receivedDelta = null;
            SetUpAttachedManipulator(delta => receivedDelta = delta, zoomProvider: () => 2f);
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            SendMouseDrag(window.rootVisualElement, new Vector2(70, 60), delta: new Vector2(20, 10));
            yield return null;

            Assert.AreEqual(new Vector2(10, 5), receivedDelta);
        }

        [UnityTest]
        public IEnumerator MouseDrag_WithoutPriorMouseDown_DoesNotInvokeShiftPosition()
        {
            bool invoked = false;
            SetUpAttachedManipulator(_ => invoked = true);
            yield return null;

            SendMouseDrag(window.rootVisualElement, new Vector2(999, 999), delta: new Vector2(1, 1));
            yield return null;

            Assert.IsFalse(invoked);
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_ZeroOrNegativeZoom_ClampsToMinimumRatherThanDividingByZero()
        {
            Vector2? receivedDelta = null;
            SetUpAttachedManipulator(delta => receivedDelta = delta, zoomProvider: () => 0f);
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            SendMouseDrag(window.rootVisualElement, new Vector2(51, 50), delta: new Vector2(1, 0));
            yield return null;

            // zoom clamped to 0.01f minimum, so a delta of 1 becomes 1/0.01 = 100
            receivedDelta ??= Vector2.zero;
            Assert.AreEqual(100f, receivedDelta.Value.x, 0.5f);
        }

        [UnityTest]
        public IEnumerator MouseUp_ReleasesMouseCapture()
        {
            SetUpAttachedManipulator(_ => { });
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            SendMouseUp(window.rootVisualElement, new Vector2(60, 60));
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }
        #endregion
    }
}
