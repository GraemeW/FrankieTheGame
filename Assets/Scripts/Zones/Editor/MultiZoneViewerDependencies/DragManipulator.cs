using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.UIEditor
{
    public class DragManipulator : MouseManipulator
    {
        private readonly ZoneView zoneView;
        private readonly VisualElement activeVisualElement;
        private bool dragging;
        private Vector2 startMouse;
        private Vector2 startPos;

        public DragManipulator(ZoneView zoneView, VisualElement activeVisualElement)
        {
            this.zoneView = zoneView;
            this.activeVisualElement = activeVisualElement;
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

            BringNodeToFront();
            target.CaptureMouse();
            mouseDownEvent.StopPropagation();
        }

        private void BringNodeToFront()
        {
            // Re-inserting at end of parent hack
            VisualElement parent = activeVisualElement.parent;
            if (parent == null) { return; }
            parent.Remove(activeVisualElement);
            parent.Add(activeVisualElement);
        }

        private void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!dragging) { return; }
            zoneView.data.topLeftPosition = startPos + (mouseMoveEvent.mousePosition - startMouse);
            activeVisualElement.style.left = zoneView.data.topLeftPosition.x;
            activeVisualElement.style.top = zoneView.data.topLeftPosition.y;
            mouseMoveEvent.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent mouseUpEvent)
        {
            if (!dragging || !CanStopManipulation(mouseUpEvent)) { return; }
            dragging = false;
            target.ReleaseMouse();
            
            EditorUtility.SetDirty(zoneView.data);
            AssetDatabase.SaveAssetIfDirty(zoneView.data);
            
            mouseUpEvent.StopPropagation();
        }
    }
}
