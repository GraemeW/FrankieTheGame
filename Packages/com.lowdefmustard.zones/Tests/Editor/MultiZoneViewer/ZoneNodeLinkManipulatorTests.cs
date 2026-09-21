using System.Collections;
using LowDefMustard.Zones.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneNodeLinkManipulatorTests
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
        private ZoneNodeLinkManipulator SetUpAttachedManipulator(
            System.Action onDragStarted = null, System.Action<Vector2> onDragUpdated = null, System.Action<Vector2> onDragEnded = null)
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();
            var manipulator = new ZoneNodeLinkManipulator(window.rootVisualElement, onDragStarted, onDragUpdated, onDragEnded);
            window.rootVisualElement.AddManipulator(manipulator);
            return manipulator;
        }
        #endregion

        #region Tests
        [Test]
        public void AddManipulator_SetsTarget_NoPanelRequired()
        {
            var element = new VisualElement();
            var manipulator = new ZoneNodeLinkManipulator(new VisualElement(), null, null, null);

            element.AddManipulator(manipulator);

            Assert.AreSame(element, manipulator.target);
        }
        
        [UnityTest]
        public IEnumerator MouseDown_StartsDragAndInvokesOnDragStarted()
        {
            int startCount = 0;
            SetUpAttachedManipulator(onDragStarted: () => startCount++);
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;

            Assert.AreEqual(1, startCount);
            Assert.IsTrue(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDown_UnregisteredButton_DoesNotStartDrag()
        {
            int startCount = 0;
            SetUpAttachedManipulator(onDragStarted: () => startCount++);
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 1);
            yield return null;

            Assert.AreEqual(0, startCount);
            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_InvokesOnDragUpdatedWithLocalCoordinates()
        {
            Vector2? receivedPosition = null;
            SetUpAttachedManipulator(onDragUpdated: pos => receivedPosition = pos);
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(70, 60));
            yield return null;

            Assert.IsTrue(receivedPosition.HasValue);
        }

        [UnityTest]
        public IEnumerator MouseDrag_WithoutPriorMouseDown_DoesNotInvokeOnDragUpdated()
        {
            bool invoked = false;
            SetUpAttachedManipulator(onDragUpdated: _ => invoked = true);
            yield return null;

            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(999, 999));
            yield return null;

            Assert.IsFalse(invoked);
        }

        [UnityTest]
        public IEnumerator MouseUp_InvokesOnDragEndedAndReleasesMouseCapture()
        {
            int endCount = 0;
            SetUpAttachedManipulator(onDragEnded: _ => endCount++);
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            HeadlessEditorWindow.SendMouseUp(window.rootVisualElement, new Vector2(70, 60));
            yield return null;

            Assert.AreEqual(1, endCount);
            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }
        #endregion
    }
}
