using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Editor
{
    public class StandardObjectDropManipulator<T> : Manipulator where T : Object
    {
        // State
        private readonly Func<T, bool> canAccept;
        private readonly Action<T> onDrop;
        private readonly string highlightClass;

        public StandardObjectDropManipulator(Func<T, bool> canAccept, Action<T> onDrop, string highlightClass = null)
        {
            this.canAccept = canAccept;
            this.onDrop = onDrop;
            this.highlightClass = highlightClass; // opt-in (e.g. StandardBrownTheme.dropTargetClass) ~ null leaves the element's styling alone
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
            target.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
            target.RegisterCallback<DragLeaveEvent>(OnDragEnded);
            target.RegisterCallback<DragExitedEvent>(OnDragEnded);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
            target.UnregisterCallback<DragLeaveEvent>(OnDragEnded);
            target.UnregisterCallback<DragExitedEvent>(OnDragEnded);
            SetHighlight(false);
        }

        private void OnDragUpdated(DragUpdatedEvent dragUpdatedEvent)
        {
            if (!IsDraggingAny(DragAndDrop.objectReferences)) { return; } // Not ours (e.g. text / other assets) ~ leave to the target's own handling

            bool isDroppable = TryGetDroppable(DragAndDrop.objectReferences, canAccept, out _);
            DragAndDrop.visualMode = isDroppable ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
            SetHighlight(isDroppable);
            dragUpdatedEvent.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent dragPerformEvent)
        {
            SetHighlight(false);
            if (!TryGetDroppable(DragAndDrop.objectReferences, canAccept, out T droppable)) { return; }

            DragAndDrop.AcceptDrag();
            dragPerformEvent.StopPropagation();
            onDrop?.Invoke(droppable);
        }

        private void OnDragEnded(EventBase _) => SetHighlight(false);
        
        internal static bool TryGetDroppable(IReadOnlyList<Object> objectReferences, Func<T, bool> canAccept, out T droppable)
        {
            droppable = null;
            if (objectReferences is not { Count: 1 } || objectReferences[0] is not T candidate) { return false; }
            if (canAccept != null && !canAccept(candidate)) { return false; }

            droppable = candidate;
            return true;
        }

        internal static bool IsDraggingAny(IReadOnlyList<Object> objectReferences)
        {
            if (objectReferences == null) { return false; }
            foreach (Object objectReference in objectReferences) { if (objectReference is T) { return true; } }
            return false;
        }
        
        private void SetHighlight(bool isHighlighted)
        {
            if (string.IsNullOrEmpty(highlightClass)) { return; }
            target.EnableInClassList(highlightClass, isHighlighted);
        }
    }
}
