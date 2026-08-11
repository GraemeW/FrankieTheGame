using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Zones.Editor
{
    public class ZoneEdgesLayer : VisualElement
    {
        // Tunables
        private const float _bezierOffset = 100f;
        private const float _bezierWidth = 2f;
        private static readonly Color _originColor = Color.green;
        private static readonly Color _arrivingColor = Color.blue;

        // State
        private List<(Rect from, Rect to)> edges = new();
        private static readonly Gradient _edgeGradient = new()
        {
            colorKeys = new[]
            {
                new GradientColorKey(_originColor, 0f),
                new GradientColorKey(_arrivingColor, 1f)
            }
        };
        
        public ZoneEdgesLayer()
        {
            name = "zone-edges-layer";
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            pickingMode = PickingMode.Ignore; // Never intercept clicks meant for nodes/canvas
            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetEdges(List<(Rect from, Rect to)> newEdges)
        {
            edges = newEdges ?? new List<(Rect, Rect)>();
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (edges.Count == 0) { return; }

            Painter2D painter = context.painter2D;
            painter.lineWidth = _bezierWidth;
            painter.strokeGradient = _edgeGradient;

            foreach ((Rect fromRect, Rect toRect) in edges)
            {
                var startPoint = new Vector2(fromRect.xMax, fromRect.center.y);
                var endPoint = new Vector2(toRect.xMin, toRect.center.y);
                Vector2 startTangent = startPoint + Vector2.right * _bezierOffset;
                Vector2 endTangent = endPoint + Vector2.left * _bezierOffset;

                painter.BeginPath();
                painter.MoveTo(startPoint);
                painter.BezierCurveTo(startTangent, endTangent, endPoint);
                painter.Stroke();
            }
        }
    }
}
