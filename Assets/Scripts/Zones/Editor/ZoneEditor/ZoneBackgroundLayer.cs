using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.ZoneManagement.Editor
{
    public class ZoneBackgroundLayer : VisualElement
    {
        // Tunables
        private const float _backgroundExtents = 3000f;
        private const float _cellSize = 50f;
        private const float _lineWidth = 1f;
        private static readonly Color _lineColor = new(1f, 1f, 1f, 0.08f);

        public ZoneBackgroundLayer()
        {
            name = "zone-background-layer";
            style.position = Position.Absolute;
            style.left = -_backgroundExtents;
            style.top = -_backgroundExtents;
            style.width = _backgroundExtents * 2f;
            style.height = _backgroundExtents * 2f;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
        }

        private static void OnGenerateVisualContent(MeshGenerationContext context)
        {
            const float size = _backgroundExtents * 2f;

            Painter2D painter = context.painter2D;
            painter.strokeColor = _lineColor;
            painter.lineWidth = _lineWidth;

            painter.BeginPath();
            for (float x = 0f; x <= size; x += _cellSize)
            {
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, size));
            }
            for (float y = 0f; y <= size; y += _cellSize)
            {
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(size, y));
            }
            painter.Stroke();
        }
    }
}
