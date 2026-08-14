using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    public class StandardCanvasPanManipulator : MouseManipulator
    {
        // State
        private readonly VisualElement panTarget;
        private Vector2 panStartMousePosition;
        private Vector2 panStartOffset;
        private bool isPanning;

        public StandardCanvasPanManipulator(VisualElement panTarget)
        {
            this.panTarget = panTarget;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
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

        private void OnMouseDown(MouseDownEvent mouseDownEvent)
        {
            if (isPanning || !CanStartManipulation(mouseDownEvent)) { return; }

            isPanning = true;
            panStartMousePosition = mouseDownEvent.mousePosition;
            panStartOffset = new Vector2(panTarget.resolvedStyle.left, panTarget.resolvedStyle.top);

            target.CaptureMouse();
            mouseDownEvent.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!isPanning || !target.HasMouseCapture()) { return; }

            Vector2 delta = mouseMoveEvent.mousePosition - panStartMousePosition;
            panTarget.style.left = panStartOffset.x + delta.x;
            panTarget.style.top = panStartOffset.y + delta.y;
            mouseMoveEvent.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent mouseUpEvent)
        {
            if (!isPanning || !CanStopManipulation(mouseUpEvent)) { return; }

            isPanning = false;
            target.ReleaseMouse();
            mouseUpEvent.StopPropagation();
        }
    }
}
