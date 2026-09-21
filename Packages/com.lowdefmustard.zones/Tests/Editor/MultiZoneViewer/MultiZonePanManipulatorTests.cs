using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class MultiZonePanManipulatorTests
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
        private MultiZonePanManipulator SetUpAttachedManipulator(System.Action<Vector2> onDelta)
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            var manipulator = new MultiZonePanManipulator(onDelta);
            window.rootVisualElement.AddManipulator(manipulator);
            return manipulator;
        }
        #endregion

        #region Tests
        [Test]
        public void AddManipulator_SetsTarget_NoPanelRequired()
        {
            var element = new VisualElement();
            var manipulator = new MultiZonePanManipulator(_ => { });

            element.AddManipulator(manipulator);

            Assert.AreSame(element, manipulator.target);
        }
        
        [UnityTest]
        public IEnumerator MouseDown_MiddleButton_StartsPanAndCapturesMouse()
        {
            SetUpAttachedManipulator(_ => { });
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 2);
            yield return null;

            Assert.IsTrue(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDown_LeftButtonWithAlt_StartsPanAndCapturesMouse()
        {
            SetUpAttachedManipulator(_ => { });
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 0, altKey: true);
            yield return null;

            Assert.IsTrue(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDown_LeftButtonWithoutAlt_DoesNotStartPan()
        {
            SetUpAttachedManipulator(_ => { });
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 0);
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_InvokesOnDeltaWithMouseMovement()
        {
            Vector2? receivedDelta = null;
            SetUpAttachedManipulator(delta => receivedDelta = delta);
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 2);
            yield return null;
            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(80, 65), button: 2);
            yield return null;

            Assert.AreEqual(new Vector2(30, 15), receivedDelta);
        }

        [UnityTest]
        public IEnumerator MouseDrag_WithoutPriorMouseDown_DoesNotInvokeOnDelta()
        {
            bool invoked = false;
            SetUpAttachedManipulator(_ => invoked = true);
            yield return null;

            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(999, 999), button: 2);
            yield return null;

            Assert.IsFalse(invoked);
        }

        [UnityTest]
        public IEnumerator MouseUp_ReleasesMouseCapture()
        {
            SetUpAttachedManipulator(_ => { });
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 2);
            yield return null;
            HeadlessEditorWindow.SendMouseUp(window.rootVisualElement, new Vector2(60, 60), button: 2);
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }
        #endregion
    }
}
