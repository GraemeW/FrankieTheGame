using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class StandardObjectDragManipulatorTests
    {
        // Tested:
        //  - Below the drag threshold - no drag started, clicks still reach the target
        //  - No object to drag (provider returns null) - no drag started past the threshold
        // Not Tested:
        //  - Starting the drag past the threshold - DragAndDrop.StartDrag begins a real editor drag (not safe headless), so this is checked manually

        // State
        private readonly TestObjectTracker tracker = new();
        private EditorWindow window;
        private VisualElement dragSource;
        private DragAsset dragAsset;

        // Data Structures
        private class DragAsset : ScriptableObject { }
        
        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            HeadlessEditorWindow.EndObjectDrag();
            dragAsset = tracker.Create<DragAsset>();

            window = HeadlessEditorWindow.CreateOffscreenWindow();
            dragSource = new VisualElement { style = { width = 100f, height = 40f } };
            window.rootVisualElement.Add(dragSource);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);
        }

        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.EndObjectDrag();
            HeadlessEditorWindow.Close(window);
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private void SendPointerDown(Vector2 position)
        {
            using PointerDownEvent pointerDownEvent = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = position, button = 0 });
            pointerDownEvent.target = dragSource;
            dragSource.SendEvent(pointerDownEvent);
        }

        private void SendPointerMove(Vector2 position)
        {
            using PointerMoveEvent pointerMoveEvent = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = position, button = 0 });
            pointerMoveEvent.target = dragSource;
            dragSource.SendEvent(pointerMoveEvent);
        }

        private static bool IsDragStarted() => DragAndDrop.objectReferences is { Length: > 0 };
        #endregion

        #region Tests
        [Test]
        public void PointerMove_BelowThreshold_StartsNoDrag()
        {
            dragSource.AddManipulator(new StandardObjectDragManipulator(() => dragAsset));
            Vector2 start = dragSource.worldBound.center;

            SendPointerDown(start);
            SendPointerMove(start + Vector2.right * (StandardObjectDragManipulator.dragThreshold * 0.5f));

            Assert.IsFalse(IsDragStarted());
        }

        [Test]
        public void PointerMove_PastThresholdWithNoObject_StartsNoDrag()
        {
            dragSource.AddManipulator(new StandardObjectDragManipulator(() => null));
            Vector2 start = dragSource.worldBound.center;

            SendPointerDown(start);
            SendPointerMove(start + Vector2.right * (StandardObjectDragManipulator.dragThreshold * 2f));

            Assert.IsFalse(IsDragStarted());
        }

        [Test]
        public void Click_BelowThreshold_StillReachesTarget()
        {
            int clickCount = 0;
            dragSource.AddManipulator(new StandardObjectDragManipulator(() => dragAsset));
            dragSource.RegisterCallback<ClickEvent>(_ => clickCount++);

            SendPointerDown(dragSource.worldBound.center);
            HeadlessEditorWindow.SendClick(dragSource);

            Assert.AreEqual(1, clickCount);
        }
        #endregion
    }
}
