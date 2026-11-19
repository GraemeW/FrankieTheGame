using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Editor
{
    public sealed class UndoChangeSnapshot
    {
        //  Notes:
        //  - Undo.undoRedoPerformed fires for every undo / redo in the editor ~ thus we can compare against the last capture to skip unrelated events
        //  - Undo.RecordObject edits only bump the count once flushed (end of frame / next undo) ~ Capture flushes first so pending edits land in the baseline

        // State
        private readonly List<(Object watched, int dirtyCount)> capturedCounts = new();

        // Public Methods
        public void Capture(IEnumerable<Object> watchedObjects)
        {
            capturedCounts.Clear();
            if (watchedObjects == null) { return; }
            Undo.FlushUndoRecordObjects();
            foreach (Object watched in watchedObjects)
            {
                if (watched == null) { continue; }
                capturedCounts.Add((watched, EditorUtility.GetDirtyCount(watched)));
            }
        }

        public void Capture(Object watched) => Capture(watched != null ? new[] { watched } : null);

        public void Clear() => capturedCounts.Clear();
        
        public bool HasChanged()
        {
            foreach ((Object watched, int dirtyCount) in capturedCounts)
            {
                // Destroyed/null counts as changed (e.g. the asset was deleted or its creation undone)
                if (watched == null || EditorUtility.GetDirtyCount(watched) != dirtyCount) { return true; }
            }
            return false;
        }
    }
}
