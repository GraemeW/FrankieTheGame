using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.Editor
{
    public class ZoneNodeGroupDragManipulator : MouseManipulator
    {
        private readonly Func<Vector2> getPosition;
        private readonly Action<Vector2> setPosition;
        private readonly Func<float> zoomProvider;
        private bool isDragging;

        public ZoneNodeGroupDragManipulator(Func<Vector2> getPosition, Action<Vector2> setPosition, Func<float> zoomProvider)
        {
            this.getPosition = getPosition;
            this.setPosition = setPosition;
            this.zoomProvider = zoomProvider;
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

        private void OnMouseDown(MouseDownEvent mouseDownEvent)
        {
            if (isDragging || !CanStartManipulation(mouseDownEvent)) { return; }

            isDragging = true;
            target.CaptureMouse();
            mouseDownEvent.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!isDragging || !target.HasMouseCapture()) { return; }

            float zoom = Mathf.Max(zoomProvider?.Invoke() ?? 1f, 0.01f);
            Vector2 newPosition = getPosition() + mouseMoveEvent.mouseDelta / zoom;
            setPosition(newPosition);
            mouseMoveEvent.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent mouseUpEvent)
        {
            if (!isDragging || !CanStopManipulation(mouseUpEvent)) { return; }

            isDragging = false;
            target.ReleaseMouse();
            mouseUpEvent.StopPropagation();
        }
    }
}
