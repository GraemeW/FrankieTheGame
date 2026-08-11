using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Zones.Editor
{
    public class ZoneNodeLinkManipulator : MouseManipulator
    {
        private readonly VisualElement coordinateSpace;
        private readonly System.Action onDragStarted;
        private readonly System.Action<Vector2> onDragUpdated;
        private readonly System.Action<Vector2> onDragEnded;
        private bool dragging;

        public ZoneNodeLinkManipulator(VisualElement coordinateSpace, System.Action onDragStarted, System.Action<Vector2> onDragUpdated, System.Action<Vector2> onDragEnded)
        {
            this.coordinateSpace = coordinateSpace;
            this.onDragStarted = onDragStarted;
            this.onDragUpdated = onDragUpdated;
            this.onDragEnded = onDragEnded;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private Vector2 ToCoordinateSpace(Vector2 mousePosition) => coordinateSpace.WorldToLocal(mousePosition);

        private void OnMouseDown(MouseDownEvent mouseDownEvent)
        {
            if (!CanStartManipulation(mouseDownEvent)) { return; }

            dragging = true;
            target.CaptureMouse();
            onDragStarted?.Invoke();
            mouseDownEvent.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!dragging) { return; }
            onDragUpdated?.Invoke(ToCoordinateSpace(mouseMoveEvent.mousePosition));
            mouseMoveEvent.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent mouseUpEvent)
        {
            if (!dragging || !CanStopManipulation(mouseUpEvent)) { return; }

            dragging = false;
            target.ReleaseMouse();
            onDragEnded?.Invoke(ToCoordinateSpace(mouseUpEvent.mousePosition));
            mouseUpEvent.StopPropagation();
        }
    }
}
