using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    public class StandardBackgroundLayer : VisualElement
    {
        // Tunables
        private const float _backgroundExtents = 6000f;
        private const float _cellSize = 50f;
        private const float _lineWidth = 1f;
        private static readonly Color _lineColour = new(1f, 1f, 1f, 0.08f);
        private const float _backgroundDotRadius = 1.5f;
        private static readonly Color _backgroundDotColour = new(1f, 1f, 1f, 0.15f);

        // State
        private readonly StandardBackgroundType backgroundType;

        public StandardBackgroundLayer(StandardBackgroundType standardBackgroundType)
        {
            name = "background-layer";
            backgroundType = standardBackgroundType;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = -_backgroundExtents;
            style.top = -_backgroundExtents;
            style.width = _backgroundExtents * 2f;
            style.height = _backgroundExtents * 2f;
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            switch (backgroundType)
            {
                case StandardBackgroundType.Dots:
                    DrawDotBackground(context);
                    break;
                case StandardBackgroundType.Lines:
                default:
                    DrawLineBackground(context);
                    break;
            }
        }

        private static void DrawLineBackground(MeshGenerationContext context)
        {
            const float size = _backgroundExtents * 2f;

            Painter2D painter = context.painter2D;
            painter.strokeColor = _lineColour;
            painter.lineWidth = _lineWidth;

            painter.BeginPath();
            foreach (var (start, end) in GenerateGridLines(size, _cellSize))
            {
                painter.MoveTo(start);
                painter.LineTo(end);
            }
            painter.Stroke();
        }

        private static void DrawDotBackground(MeshGenerationContext context)
        {
            const float size = _backgroundExtents * 2f;

            Painter2D painter = context.painter2D;
            painter.fillColor = _backgroundDotColour;

            foreach (var quad in GenerateDotQuads(size, _cellSize, _backgroundDotRadius))
            {
                painter.BeginPath();
                painter.MoveTo(quad.bottomLeft);
                painter.LineTo(quad.bottomRight);
                painter.LineTo(quad.topRight);
                painter.LineTo(quad.topLeft);
                painter.ClosePath();
                painter.Fill();
            }
            painter.Stroke();
        }

        #region InternalHelpers
        // Note:  marked internal so the geometry math is directly testable
        internal static List<(Vector2 start, Vector2 end)> GenerateGridLines(float size, float cellSize)
        {
            var lines = new List<(Vector2, Vector2)>();
            for (float x = 0f; x <= size; x += cellSize)
            {
                lines.Add((new Vector2(x, 0f), new Vector2(x, size)));
            }
            for (float y = 0f; y <= size; y += cellSize)
            {
                lines.Add((new Vector2(0f, y), new Vector2(size, y)));
            }
            return lines;
        }

        internal static List<(Vector2 bottomLeft, Vector2 bottomRight, Vector2 topRight, Vector2 topLeft)> GenerateDotQuads(float size, float cellSize, float radius)
        {
            var quads = new List<(Vector2, Vector2, Vector2, Vector2)>();
            for (float x = 0f; x <= size; x += cellSize)
            {
                for (float y = 0f; y <= size; y += cellSize)
                {
                    var center = new Vector2(x, y);
                    quads.Add((
                        center + new Vector2(-radius, -radius),
                        center + new Vector2(radius, -radius),
                        center + new Vector2(radius, radius),
                        center + new Vector2(-radius, radius)
                    ));
                }
            }
            return quads;
        }
        #endregion
    }
}
