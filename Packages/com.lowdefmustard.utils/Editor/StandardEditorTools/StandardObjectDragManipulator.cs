using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Editor
{
    public class StandardObjectDragManipulator : PointerManipulator
    {
        // Const Tunables
        internal const float dragThreshold = 4f;

        // State
        private readonly Func<Object> objectProvider;
        private Object pendingObject;
        private Vector2 pendingStart;

        public StandardObjectDragManipulator(Func<Object> objectProvider)
        {
            this.objectProvider = objectProvider;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        private void OnPointerDown(PointerDownEvent pointerDownEvent)
        {
            if (!CanStartManipulation(pointerDownEvent)) { return; }
            pendingObject = objectProvider?.Invoke();
            pendingStart = pointerDownEvent.position;
        }

        private void OnPointerMove(PointerMoveEvent pointerMoveEvent)
        {
            if (pendingObject == null || (pointerMoveEvent.pressedButtons & 1) == 0) { return; }
            if (Vector2.Distance(pendingStart, pointerMoveEvent.position) < dragThreshold) { return; }

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { pendingObject };
            DragAndDrop.StartDrag(pendingObject.name);
            pendingObject = null;
        }

        private void OnPointerUp(PointerUpEvent pointerUpEvent) => pendingObject = null;
    }
}
