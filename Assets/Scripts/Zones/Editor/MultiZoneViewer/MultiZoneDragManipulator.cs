using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.Editor
{
    public class MultiZoneDragManipulator : MouseManipulator
    {
        private readonly ZoneView zoneView;
        private readonly VisualElement activeVisualElement;
        private readonly System.Action onClicked;
        private readonly System.Action onDragged;
        private readonly System.Func<float> getZoomScale;
        private readonly System.Action onDragComplete;
        private bool dragging;
        private Vector2 startMouse;
        private Vector2 startPos;

        private const float _clickMoveThreshold = 4f;

        public MultiZoneDragManipulator(ZoneView zoneView, VisualElement activeVisualElement, System.Action onClicked, System.Action onDragged, System.Func<float> getZoomScale = null, System.Action onDragComplete = null)
        {
            this.zoneView = zoneView;
            this.activeVisualElement = activeVisualElement;
            this.onClicked = onClicked;
            this.onDragged = onDragged;
            this.getZoomScale = getZoomScale ?? (() => 1f);
            this.onDragComplete = onDragComplete;
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
            if (mouseDownEvent.altKey || !CanStartManipulation(mouseDownEvent)) { return; }
                // Alt+left reserved for panning

            dragging = true;
            startMouse = mouseDownEvent.mousePosition;
            startPos = zoneView.data.topLeftPosition;
            Undo.RecordObject(zoneView.data, "Move Zone View");

            BringNodeToFront();
            target.CaptureMouse();
            mouseDownEvent.StopPropagation();
        }

        private void BringNodeToFront()
        {
            VisualElement parent = activeVisualElement.parent;
            if (parent == null) { return; }
            parent.Remove(activeVisualElement);
            parent.Add(activeVisualElement);
        }

        private void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!dragging) { return; }
            
            float zoomScale = Mathf.Max(getZoomScale(), 0.0001f);
            Vector2 screenDelta = mouseMoveEvent.mousePosition - startMouse;
            zoneView.data.topLeftPosition = startPos + screenDelta / zoomScale;
            activeVisualElement.style.left = zoneView.data.topLeftPosition.x;
            activeVisualElement.style.top = zoneView.data.topLeftPosition.y;
            onDragged?.Invoke();
            mouseMoveEvent.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent mouseUpEvent)
        {
            if (!dragging || !CanStopManipulation(mouseUpEvent)) { return; }
            dragging = false;
            target.ReleaseMouse();
            
            float travel = Vector2.Distance(mouseUpEvent.mousePosition, startMouse);
            if (travel <= _clickMoveThreshold)
            {
                onClicked?.Invoke();
            }
            else
            {
                EditorUtility.SetDirty(zoneView.data);
                onDragComplete?.Invoke(); // SaveAssetIfDirty must target the parent asset, so the actual disk save should delegate to the caller via onDragComplete
            }
            mouseUpEvent.StopPropagation();
        }
    }
}
