using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class MultiZoneDragManipulatorTests
    {
        // State
        private EditorWindow window;
        private ZoneViewData zoneViewData;
        private Texture2D texture;
        private ZoneView zoneView;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
            if (zoneViewData != null) { Object.DestroyImmediate(zoneViewData); }
            if (texture != null) { Object.DestroyImmediate(texture); }
        }
        #endregion

        #region PrivateMethods
        private (MultiZoneDragManipulator manipulator, VisualElement nodeElement) SetUpAttachedManipulator(System.Action onClicked = null, System.Action onDragged = null, System.Func<float> zoomProvider = null, System.Action onDragComplete = null)
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow();

            zoneViewData = ScriptableObject.CreateInstance<ZoneViewData>();
            texture = new Texture2D(1, 1);
            zoneView = new ZoneView(zoneViewData, texture, Vector2.zero, Vector2.zero);

            var nodeElement = new VisualElement();
            window.rootVisualElement.Add(nodeElement);
            var manipulator = new MultiZoneDragManipulator(zoneView, nodeElement, onClicked, onDragged, zoomProvider ?? (() => 1f), onDragComplete);
            window.rootVisualElement.AddManipulator(manipulator);
            return (manipulator, nodeElement);
        }
        #endregion

        #region Tests
        [Test]
        public void AddManipulator_SetsTarget_NoPanelRequired()
        {
            var element = new VisualElement();
            zoneViewData = ScriptableObject.CreateInstance<ZoneViewData>();
            zoneView = new ZoneView(zoneViewData, null, Vector2.zero, Vector2.zero);
            var manipulator = new MultiZoneDragManipulator(zoneView, new VisualElement(), null, null);

            element.AddManipulator(manipulator);

            Assert.AreSame(element, manipulator.target);
        }
        
        [UnityTest]
        public IEnumerator MouseDown_StartsDragAndCapturesMouse()
        {
            SetUpAttachedManipulator();
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;

            Assert.IsTrue(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDown_UnregisteredButton_DoesNotStartDrag()
        {
            SetUpAttachedManipulator();
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), button: 1);
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDown_AltKey_DoesNotStartDrag()
        {
            // Alt+left is reserved for panning by MultiZonePanManipulator
            SetUpAttachedManipulator();
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50), altKey: true);
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_MovesZoneViewByDeltaDividedByZoom()
        {
            SetUpAttachedManipulator(zoomProvider: () => 2f);
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(70, 60));
            yield return null;

            // delta (20,10) / zoom 2 = (10,5), starting from topLeftPosition (0,0)
            Assert.AreEqual(new Vector2(10, 5), zoneView.data.topLeftPosition);
        }

        [UnityTest]
        public IEnumerator MouseDownThenDrag_InvokesOnDragged()
        {
            int dragCallCount = 0;
            SetUpAttachedManipulator(onDragged: () => dragCallCount++);
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            HeadlessEditorWindow.SendMouseDrag(window.rootVisualElement, new Vector2(70, 60));
            yield return null;

            Assert.AreEqual(1, dragCallCount);
        }

        [UnityTest]
        public IEnumerator MouseUp_MovementBelowThreshold_InvokesOnClickedInsteadOfComplete()
        {
            int clickCount = 0;
            int completeCount = 0;
            SetUpAttachedManipulator(onClicked: () => clickCount++, onDragComplete: () => completeCount++);
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            HeadlessEditorWindow.SendMouseUp(window.rootVisualElement, new Vector2(51, 50)); // 1px, below the 4px threshold
            yield return null;

            Assert.AreEqual(1, clickCount);
            Assert.AreEqual(0, completeCount);
        }

        [UnityTest]
        public IEnumerator MouseUp_MovementAboveThreshold_InvokesOnDragCompleteInsteadOfClicked()
        {
            int clickCount = 0;
            int completeCount = 0;
            SetUpAttachedManipulator(onClicked: () => clickCount++, onDragComplete: () => completeCount++);
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            HeadlessEditorWindow.SendMouseUp(window.rootVisualElement, new Vector2(90, 90)); // well past the 4px threshold
            yield return null;

            Assert.AreEqual(0, clickCount);
            Assert.AreEqual(1, completeCount);
        }

        [UnityTest]
        public IEnumerator MouseUp_ReleasesMouseCapture()
        {
            SetUpAttachedManipulator();
            yield return null;

            HeadlessEditorWindow.SendMouseDown(window.rootVisualElement, new Vector2(50, 50));
            yield return null;
            HeadlessEditorWindow.SendMouseUp(window.rootVisualElement, new Vector2(90, 90));
            yield return null;

            Assert.IsFalse(window.rootVisualElement.HasMouseCapture());
        }
        #endregion
    }
}
