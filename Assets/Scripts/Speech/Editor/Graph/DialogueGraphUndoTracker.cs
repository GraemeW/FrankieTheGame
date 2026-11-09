using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Frankie.Speech.Editor
{
    internal static partial class DialogueGraphUndoTracker
    {
        // Small utility class to lets the next graph change know it came from undo/redo
        //  - allows us to skip structural refreshes (i.e. avoids clearing redo)
        //  - undo/redo fires for every object, but only graph undos reach OnGraphChanged ~ so the mark expires once the undo group moves on (i.e. on the next edit)

        // Const Tunables
        private const int _unsetGroup = -1;

        // State
        [AutoStaticsCleanup] private static bool _undoRedoPending;
        [AutoStaticsCleanup] private static int _undoRedoGroup = _unsetGroup;

        // InitializeOnLoad instead of static constructor (latter cannot live alongside auto statics cleanup properties)
        [InitializeOnLoadMethod]
        private static void SubscribeToUndoRedo()
        {
            Undo.undoRedoEvent += HandleUndoRedo;
        }

        public static bool ConsumeUndoRedo()
        {
            bool fromUndoRedo = _undoRedoPending && (_undoRedoGroup == _unsetGroup || _undoRedoGroup == Undo.GetCurrentGroup()); // unset -> group not yet captured from next editor tick (must be this editor)
            _undoRedoPending = false;
            _undoRedoGroup = _unsetGroup;
            EditorApplication.update -= CaptureUndoRedoGroup;
            return fromUndoRedo;
        }

        private static void HandleUndoRedo(in UndoRedoInfo undoRedoInfo)
        {
            _undoRedoPending = true;
            _undoRedoGroup = _unsetGroup;
            EditorApplication.update -= CaptureUndoRedoGroup;
            EditorApplication.update += CaptureUndoRedoGroup;
        }
        
        private static void CaptureUndoRedoGroup()
        {
            // Unity is still mid-undo/redo during the event (and advances the group after it)
            //  - so we grab the settled group on the next editor tick
            
            EditorApplication.update -= CaptureUndoRedoGroup;
            if (_undoRedoPending) { _undoRedoGroup = Undo.GetCurrentGroup(); }
        }
    }
}
