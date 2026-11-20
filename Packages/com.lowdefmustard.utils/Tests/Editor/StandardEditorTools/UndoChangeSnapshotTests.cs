using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Utils.Editor;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class UndoChangeSnapshotTests
    {
        // Tested:
        //  - HasChanged - false w/ nothing changed or nothing captured;  true after an edit, an undo / redo of the watched object, or it being destroyed
        //  - Unrelated undo - not a change;  Capture resets the baseline (incl. pending Undo.RecordObject edits);  Clear forgets everything
        // Not Tested:
        //  - Asset deletion - same path as destroying the object (Unity-null while still referenced)

        // State
        private readonly TestObjectTracker tracker = new();
        private Watched watched;
        private Watched unrelated;
        private UndoChangeSnapshot snapshot;

        // Data Structures
        private class Watched : ScriptableObject { public int value; }
        
        #region Setup
        [SetUp]
        public void SetUp()
        {
            watched = tracker.Create<Watched>("UndoChangeSnapshotTests_Watched");
            unrelated = tracker.Create<Watched>("UndoChangeSnapshotTests_Unrelated");
            snapshot = new UndoChangeSnapshot();
        }

        [TearDown]
        public void TearDown()
        {
            if (watched != null) { Undo.ClearUndo(watched); }
            Undo.ClearUndo(unrelated);
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private static void RecordEdit(Watched target, int newValue)
        {
            Undo.IncrementCurrentGroup();
            Undo.RecordObject(target, "UndoChangeSnapshotTests Edit");
            target.value = newValue;
            EditorUtility.SetDirty(target);
            Undo.IncrementCurrentGroup();
        }
        #endregion

        #region Tests
        [Test]
        public void HasChanged_NothingChanged_False()
        {
            snapshot.Capture(watched);

            Assert.IsFalse(snapshot.HasChanged());
        }

        [Test]
        public void HasChanged_NothingCaptured_False()
        {
            snapshot.Capture((Object)null);

            Assert.IsFalse(snapshot.HasChanged());
        }

        [Test]
        public void HasChanged_WatchedEdited_True()
        {
            snapshot.Capture(watched);

            RecordEdit(watched, 1);

            Assert.IsTrue(snapshot.HasChanged());
        }

        [Test]
        public void HasChanged_WatchedUndoneAndRedone_TrueEachTime()
        {
            RecordEdit(watched, 1);
            snapshot.Capture(watched);

            Undo.PerformUndo();
            Assert.AreEqual(0, watched.value);
            Assert.IsTrue(snapshot.HasChanged(), "undo");

            snapshot.Capture(watched);
            Undo.PerformRedo();
            Assert.IsTrue(snapshot.HasChanged(), "redo");
        }

        [Test]
        public void HasChanged_UnrelatedUndone_False()
        {
            RecordEdit(unrelated, 1);
            snapshot.Capture(watched);

            Undo.PerformUndo();

            Assert.AreEqual(0, unrelated.value);
            Assert.IsFalse(snapshot.HasChanged());
        }

        [Test]
        public void HasChanged_WatchedDestroyed_True()
        {
            snapshot.Capture(new Object[] { watched, unrelated });

            Object.DestroyImmediate(watched);

            Assert.IsTrue(snapshot.HasChanged());
        }

        [Test]
        public void Capture_AfterChange_ResetsBaseline()
        {
            snapshot.Capture(watched);
            RecordEdit(watched, 1);

            snapshot.Capture(watched);

            Assert.IsFalse(snapshot.HasChanged());
        }

        [Test]
        public void Capture_PendingRecordedEdit_InBaseline()
        {
            Undo.IncrementCurrentGroup();
            Undo.RecordObject(watched, "UndoChangeSnapshotTests Pending Edit");
            watched.value = 1; // No SetDirty ~ only registered once the record is flushed
            snapshot.Capture(watched);
            Undo.IncrementCurrentGroup();
            RecordEdit(unrelated, 1);

            Undo.PerformUndo();

            Assert.IsFalse(snapshot.HasChanged(), "the flush happened at capture, not during the unrelated undo");
        }

        [Test]
        public void Clear_ForgetsCapture()
        {
            snapshot.Capture(watched);
            snapshot.Clear();

            Object.DestroyImmediate(watched);

            Assert.IsFalse(snapshot.HasChanged());
        }
        #endregion
    }
}
