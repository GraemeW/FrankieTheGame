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
            HeadlessEditorWindow.Close(window);
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
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            var panTarget = new VisualElement();
            window.rootVisualElement.Add(panTarget);
            var manipulator = new StandardCanvasPanManipulator(panTarget);
            window.rootVisualElement.AddManipulator(manipulator);
            return (manipulator, panTarget);
        }

        [UnityTest]
        public IEnumerator MouseDown_LeftButton_StartsPanAndCapturesMouse()
        {
            SetUpAttachedManipulator();
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 0);

            Assert.IsTrue(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_UpdatesPanTargetOffsetByDelta()
        {
            var (_, panTarget) = SetUpAttachedManipulator();
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 0);
            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(80, 65), button: 0);

            // Started at offset (0,0) via default style, mouse moved by (30, 15)
            Assert.AreEqual(30f, panTarget.style.left.value.value, 0.5f);
            Assert.AreEqual(15f, panTarget.style.top.value.value, 0.5f);
        }

        [UnityTest]
        public IEnumerator MouseUp_ReleasesMouseCaptureAndStopsPanning()
        {
            SetUpAttachedManipulator();
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 0);
            HeadlessEditorWindow.SendMouseUp(window.rootVisualElement, new Vector2(60, 60), button: 0);

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDrag_WithoutPriorMouseDown_DoesNotMoveTarget()
        {
            // No active pan (isPanning is false) - a stray drag should be a no-op
            var (_, panTarget) = SetUpAttachedManipulator();
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(999, 999), button: 0);

            Assert.AreEqual(StyleKeyword.Null, panTarget.style.left.keyword);
        }

        [UnityTest]
        public IEnumerator MouseDown_UnregisteredButton_DoesNotStartPan()
        {
            // Only left(0)/middle(2) are registered activators - right (1) shouldn't engage
            SetUpAttachedManipulator();
            yield return HeadlessEditorWindow.WaitUntilSettled(window);

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 1);

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }
    }
}
