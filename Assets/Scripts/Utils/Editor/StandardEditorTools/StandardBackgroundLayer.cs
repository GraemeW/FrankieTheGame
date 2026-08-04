using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Utils.Editor
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

        private static void DrawDotBackground(MeshGenerationContext context)
        {
            const float size = _backgroundExtents * 2f;
            
            Painter2D painter = context.painter2D;
            painter.fillColor = _backgroundDotColour;
            
            for (float x = 0; x <= size; x += _cellSize)
            {
                for (float y = 0; y <= size; y += _cellSize)
                {
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x - _backgroundDotRadius, y - _backgroundDotRadius));
                    painter.LineTo(new Vector2(x + _backgroundDotRadius, y - _backgroundDotRadius));
                    painter.LineTo(new Vector2(x + _backgroundDotRadius, y + _backgroundDotRadius));
                    painter.LineTo(new Vector2(x - _backgroundDotRadius, y + _backgroundDotRadius));
                    painter.ClosePath();
                    painter.Fill();
                }
            }
            painter.Stroke();
        }
    }
}