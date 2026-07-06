using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Speech.Editor
{
    public class DialogueNodeDragManipulator : MouseManipulator
    {
        private readonly VisualElement nodeElement;
        private readonly DialogueNode dialogueNode;
        private readonly Action onPositionChanged;
        private readonly Func<float> zoomProvider;
        private bool isDragging;

        public DialogueNodeDragManipulator(VisualElement nodeElement, DialogueNode dialogueNode, Action onPositionChanged, Func<float> zoomProvider)
        {
            this.nodeElement = nodeElement;
            this.dialogueNode = dialogueNode;
            this.onPositionChanged = onPositionChanged;
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
            Selection.activeObject = dialogueNode;

            target.CaptureMouse();
            mouseDownEvent.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!isDragging || !target.HasMouseCapture()) { return; }

            float zoom = Mathf.Max(zoomProvider?.Invoke() ?? 1f, 0.01f);
            Vector2 newPosition = dialogueNode.GetPosition() + mouseMoveEvent.mouseDelta / zoom;
            dialogueNode.SetPosition(newPosition);
            nodeElement.style.left = newPosition.x;
            nodeElement.style.top = newPosition.y;

            onPositionChanged?.Invoke();
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
