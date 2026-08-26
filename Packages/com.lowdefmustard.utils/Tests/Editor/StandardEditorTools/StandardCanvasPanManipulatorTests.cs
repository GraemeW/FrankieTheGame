using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class StandardCanvasPanManipulatorTests
    {
        private EditorWindow window;

        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
        }

        [Test]
        public void AddManipulator_SetsTarget_NoPanelRequired()
        {
            var element = new VisualElement();
            var panTarget = new VisualElement();
            var manipulator = new StandardCanvasPanManipulator(panTarget);

            element.AddManipulator(manipulator);

            Assert.AreSame(element, manipulator.target);
        }

        private (StandardCanvasPanManipulator manipulator, VisualElement panTarget) SetUpAttachedManipulator()
        {
            window = HeadlessEditorWindowTestHelper.CreateOffscreenWindow();
            var panTarget = new VisualElement();
            window.rootVisualElement.Add(panTarget);
            var manipulator = new StandardCanvasPanManipulator(panTarget);
            window.rootVisualElement.AddManipulator(manipulator);
            return (manipulator, panTarget);
        }

        private static void SendMouseDown(VisualElement target, Vector2 position, int button)
        {
            using var evt = MouseDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = position, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }

        private static void SendMouseDrag(VisualElement target, Vector2 position, int button)
        {
            using var evt = MouseMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = position, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }

        private static void SendMouseUp(VisualElement target, Vector2 position, int button)
        {
            using var evt = MouseUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = position, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }

        [UnityTest]
        public IEnumerator MouseDown_LeftButton_StartsPanAndCapturesMouse()
        {
            SetUpAttachedManipulator();
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 0);
            yield return null;

            Assert.IsTrue(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_UpdatesPanTargetOffsetByDelta()
        {
            var (_, panTarget) = SetUpAttachedManipulator();
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 0);
            yield return null;
            SendMouseDrag(window.rootVisualElement, new Vector2(80, 65), button: 0);
            yield return null;

            // Started at offset (0,0) via default style, mouse moved by (30, 15)
            Assert.AreEqual(30f, panTarget.style.left.value.value, 0.5f);
            Assert.AreEqual(15f, panTarget.style.top.value.value, 0.5f);
        }

        [UnityTest]
        public IEnumerator MouseUp_ReleasesMouseCaptureAndStopsPanning()
        {
            SetUpAttachedManipulator();
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 0);
            yield return null;
            SendMouseUp(window.rootVisualElement, new Vector2(60, 60), button: 0);
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDrag_WithoutPriorMouseDown_DoesNotMoveTarget()
        {
            // No active pan (isPanning is false) - a stray drag should be a no-op
            var (_, panTarget) = SetUpAttachedManipulator();
            yield return null;

            SendMouseDrag(window.rootVisualElement, new Vector2(999, 999), button: 0);
            yield return null;

            Assert.AreEqual(StyleKeyword.Null, panTarget.style.left.keyword);
        }

        [UnityTest]
        public IEnumerator MouseDown_UnregisteredButton_DoesNotStartPan()
        {
            // Only left(0)/middle(2) are registered activators - right (1) shouldn't engage
            SetUpAttachedManipulator();
            yield return null;

            SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 1);
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }
    }
}
