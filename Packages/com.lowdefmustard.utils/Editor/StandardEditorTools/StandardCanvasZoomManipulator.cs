using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    public class StandardCanvasZoomManipulator : Manipulator
    {
        // Const Tunables
        private const float _minZoom = 0.25f;
        private const float _maxZoom = 2f;
        private const float _zoomSensitivity = 0.15f; // fraction of current zoom applied per wheel notch

        // State
        private readonly VisualElement zoomTarget;
        public float zoomFactor { get; private set; } = 1f;
        public event Action<float> zoomChanged;

        public StandardCanvasZoomManipulator(VisualElement zoomTarget)
        {
            this.zoomTarget = zoomTarget;
            zoomTarget.style.transformOrigin = new TransformOrigin(0, 0);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<WheelEvent>(OnWheel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<WheelEvent>(OnWheel);
        }

        private void OnWheel(WheelEvent wheelEvent)
        {
            float previousZoom = zoomFactor;
            float zoomMultiplier = 1f - wheelEvent.delta.y * _zoomSensitivity;
            float newZoom = Mathf.Clamp(previousZoom * zoomMultiplier, _minZoom, _maxZoom);
            if (Mathf.Approximately(newZoom, previousZoom)) { return; }

            var previousOffset = new Vector2(zoomTarget.resolvedStyle.left, zoomTarget.resolvedStyle.top);
            Vector2 mousePosition = wheelEvent.mousePosition;
            Vector2 newOffset = mousePosition - (mousePosition - previousOffset) * (newZoom / previousZoom);

            zoomFactor = newZoom;
            zoomTarget.style.left = newOffset.x;
            zoomTarget.style.top = newOffset.y;
            zoomTarget.style.scale = new Scale(new Vector3(newZoom, newZoom, 1f));

            zoomChanged?.Invoke(newZoom);
            wheelEvent.StopPropagation();
        }
    }
}
