using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Utils.Editor
{
    public class StandardNodeDragManipulator : MouseManipulator
    {
        // Const Tunables
        private const float _moveThreshold = 0.25f;
        
        // State
        private readonly VisualElement nodeElement;
        private readonly IStandardGraphNode activeNode;
        private readonly Action onPositionChangedLive;
        private readonly Action onPositionChangedComplete;
        private readonly Func<float> zoomProvider;
        private bool isDragging;
        private Vector2 initialPosition;

        public StandardNodeDragManipulator(VisualElement nodeElement, IStandardGraphNode activeNode, Action onPositionChangedLive, Action onPositionChangedComplete, Func<float> zoomProvider)
        {
            this.nodeElement = nodeElement;
            this.activeNode = activeNode;
            this.onPositionChangedLive = onPositionChangedLive;
            this.onPositionChangedComplete = onPositionChangedComplete;
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
            initialPosition = mouseDownEvent.mousePosition;
            target.CaptureMouse();
            mouseDownEvent.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!isDragging || !target.HasMouseCapture()) { return; }

            float zoom = Mathf.Max(zoomProvider?.Invoke() ?? 1f, 0.01f);
            Vector2 newPosition = activeNode.GetPosition() + mouseMoveEvent.mouseDelta / zoom;
            activeNode.SetPosition(newPosition);
            nodeElement.style.left = newPosition.x;
            nodeElement.style.top = newPosition.y;

            onPositionChangedLive?.Invoke();
            mouseMoveEvent.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent mouseUpEvent)
        {
            if (!isDragging || !CanStopManipulation(mouseUpEvent)) { return; }

            isDragging = false;
            target.ReleaseMouse();
            mouseUpEvent.StopPropagation();

            if (Vector2.Distance(initialPosition, mouseUpEvent.mousePosition) < _moveThreshold)
            {
                Selection.activeObject = activeNode.scriptableObject;
            }
            else
            {
                onPositionChangedComplete?.Invoke();
            }
        }
    }
}
