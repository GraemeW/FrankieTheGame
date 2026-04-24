using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.UIEditor
{
    public class MultiZonePanManipulator : MouseManipulator
    {
        private readonly System.Action<Vector2> onDelta;
        private bool active;
        private Vector2 lastPosition;

        public MultiZonePanManipulator(System.Action<Vector2> onDelta)
        {
            this.onDelta = onDelta;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
            activators.Add(new ManipulatorActivationFilter
            {
                button = MouseButton.LeftMouse,
                modifiers = EventModifiers.Alt
            });
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
            if (!CanStartManipulation(mouseDownEvent)) { return; }
            active = true;
            lastPosition = mouseDownEvent.mousePosition;
            target.CaptureMouse();
            mouseDownEvent.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!active) { return; }
            onDelta(mouseMoveEvent.mousePosition - lastPosition);
            lastPosition = mouseMoveEvent.mousePosition;
            mouseMoveEvent.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent mouseUpEvent)
        {
            if (!active || !CanStopManipulation(mouseUpEvent)) { return; }
            active = false;
            target.ReleaseMouse();
            mouseUpEvent.StopPropagation();
        }
    }
}
