using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class StandardObjectDropManipulatorTests
    {
        // Note:  Drags are synthesized ~ DragAndDrop.objectReferences set w/o a live drag, events sent straight to the field
        // Tested:
        //  - Drag perform onto a text field - accepted T fills the field (onDrop), no drop for a refused / wrong-type / multi-object drag
        //  - Drag updated - highlight + link cursor for an accepted T, rejected cursor for a refused / multi-object drag, wrong-type drags pass through untouched
        //  - Highlight cleared on drag leave, on drop, and when the manipulator is removed
        //  - No highlight class given - accepted drag still links, element classes untouched
        // Not Tested:
        //  - Live drag from another window - synthesized events stand in for it

        // State
        private readonly TestObjectTracker tracker = new();
        private EditorWindow window;
        private TextField textField;
        private DropAsset dropAsset;
        private DropAsset secondDropAsset;
        private OtherAsset otherAsset;
        private int dropCount;
        private int passedThroughCount;

        // Data Structures
        private class DropAsset : ScriptableObject { }
        private class OtherAsset : ScriptableObject { }
        
        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            dropAsset = tracker.Create<DropAsset>("DropAsset");
            secondDropAsset = tracker.Create<DropAsset>("SecondDropAsset");
            otherAsset = tracker.Create<OtherAsset>("OtherAsset");
            dropCount = 0;
            passedThroughCount = 0;

            window = HeadlessEditorWindow.CreateOffscreenWindow();
            textField = new TextField();
            window.rootVisualElement.Add(textField);
            window.rootVisualElement.RegisterCallback<DragUpdatedEvent>(_ => passedThroughCount++);
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
        private StandardObjectDropManipulator<DropAsset> AddDropManipulator(System.Func<DropAsset, bool> canAccept = null, string highlightClass = StandardBrownTheme.dropTargetClass)
        {
            var manipulator = new StandardObjectDropManipulator<DropAsset>(canAccept, droppedAsset =>
            {
                dropCount++;
                textField.value = droppedAsset.name;
            }, highlightClass);
            textField.AddManipulator(manipulator);
            return manipulator;
        }

        private bool IsHighlighted() => textField.ClassListContains(StandardBrownTheme.dropTargetClass);
        #endregion

        #region Tests
        [Test]
        public void DragPerform_AcceptedObject_FillsFieldWithItsName()
        {
            AddDropManipulator();

            HeadlessEditorWindow.DropObjects(textField, dropAsset);

            Assert.AreEqual(dropAsset.name, textField.value);
            Assert.AreEqual(1, dropCount);
            Assert.IsFalse(IsHighlighted(), "highlight cleared on drop");
        }

        [Test]
        public void DragPerform_RefusedObject_LeavesFieldUnchanged()
        {
            AddDropManipulator(_ => false);
            HeadlessEditorWindow.BeginObjectDrag(dropAsset);

            HeadlessEditorWindow.SendDragPerform(textField);

            Assert.AreEqual(string.Empty, textField.value);
            Assert.AreEqual(0, dropCount);
        }

        [Test]
        public void DragPerform_WrongType_LeavesFieldUnchanged()
        {
            AddDropManipulator();
            HeadlessEditorWindow.BeginObjectDrag(otherAsset);

            HeadlessEditorWindow.SendDragPerform(textField);

            Assert.AreEqual(0, dropCount);
        }

        [Test]
        public void DragPerform_SeveralObjects_LeavesFieldUnchanged()
        {
            AddDropManipulator();
            HeadlessEditorWindow.BeginObjectDrag(dropAsset, secondDropAsset);

            HeadlessEditorWindow.SendDragPerform(textField);

            Assert.AreEqual(0, dropCount);
        }

        [Test]
        public void DragUpdated_AcceptedObject_HighlightsWithLinkCursor()
        {
            AddDropManipulator();
            HeadlessEditorWindow.BeginObjectDrag(dropAsset);

            HeadlessEditorWindow.SendDragUpdated(textField);

            Assert.IsTrue(IsHighlighted());
            Assert.AreEqual(DragAndDropVisualMode.Link, DragAndDrop.visualMode);
            Assert.AreEqual(0, passedThroughCount, "handled drags stop at the field");
        }

        [Test]
        public void DragUpdated_RefusedObject_RejectedCursorWithoutHighlight()
        {
            AddDropManipulator(_ => false);
            HeadlessEditorWindow.BeginObjectDrag(dropAsset);

            HeadlessEditorWindow.SendDragUpdated(textField);

            Assert.IsFalse(IsHighlighted());
            Assert.AreEqual(DragAndDropVisualMode.Rejected, DragAndDrop.visualMode);
        }

        [Test]
        public void DragUpdated_SeveralObjects_RejectedCursor()
        {
            AddDropManipulator();
            HeadlessEditorWindow.BeginObjectDrag(dropAsset, secondDropAsset);

            HeadlessEditorWindow.SendDragUpdated(textField);

            Assert.IsFalse(IsHighlighted());
            Assert.AreEqual(DragAndDropVisualMode.Rejected, DragAndDrop.visualMode);
        }

        [Test]
        public void DragUpdated_WrongType_PassesThroughUntouched()
        {
            AddDropManipulator();
            HeadlessEditorWindow.BeginObjectDrag(otherAsset);
            DragAndDropVisualMode visualModeBefore = DragAndDrop.visualMode;

            HeadlessEditorWindow.SendDragUpdated(textField);

            Assert.IsFalse(IsHighlighted());
            Assert.AreEqual(visualModeBefore, DragAndDrop.visualMode);
            Assert.AreEqual(1, passedThroughCount);
        }

        [Test]
        public void DragUpdated_NoHighlightClass_LinksWithoutAddingClasses()
        {
            AddDropManipulator(highlightClass: null);
            string[] classesBefore = textField.GetClasses().ToArray();
            HeadlessEditorWindow.BeginObjectDrag(dropAsset);

            HeadlessEditorWindow.SendDragUpdated(textField);

            Assert.AreEqual(DragAndDropVisualMode.Link, DragAndDrop.visualMode);
            CollectionAssert.AreEqual(classesBefore, textField.GetClasses());
        }

        [Test]
        public void DragLeave_ClearsHighlight()
        {
            AddDropManipulator();
            HeadlessEditorWindow.BeginObjectDrag(dropAsset);
            HeadlessEditorWindow.SendDragUpdated(textField);

            HeadlessEditorWindow.SendDragLeave(textField);

            Assert.IsFalse(IsHighlighted());
        }

        [Test]
        public void RemoveManipulator_ClearsHighlight()
        {
            StandardObjectDropManipulator<DropAsset> manipulator = AddDropManipulator();
            HeadlessEditorWindow.BeginObjectDrag(dropAsset);
            HeadlessEditorWindow.SendDragUpdated(textField);

            textField.RemoveManipulator(manipulator);

            Assert.IsFalse(IsHighlighted());
        }
        #endregion
    }
}
