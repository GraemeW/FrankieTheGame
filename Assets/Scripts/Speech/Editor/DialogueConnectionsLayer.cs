using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Speech.Editor
{
    public class DialogueConnectionsLayer : VisualElement
    {
        // Const Tunables
        private const float _bezierOffsetMultiplier = 0.7f;
        private const float _bezierWidth = 2f;

        // State
        private Dialogue dialogue;
        private float zoomFactor = 1f;

        public DialogueConnectionsLayer()
        {
            pickingMode = PickingMode.Ignore;
            InitializeLayerStyle();

            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetDialogue(Dialogue setDialogue)
        {
            dialogue = setDialogue;
            MarkDirtyRepaint();
        }
        
        public void SetZoomFactor(float setZoomFactor)
        {
            zoomFactor = setZoomFactor;
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (dialogue == null) { return; }

            Painter2D painter = context.painter2D;
            painter.strokeColor = Color.white;
            painter.lineWidth = _bezierWidth / Mathf.Max(zoomFactor, 0.01f);;

            foreach (DialogueNode parentNode in dialogue.GetAllNodes())
            {
                if (parentNode == null) { continue; }

                Rect parentRect = parentNode.GetRect();
                var startPoint = new Vector2(parentRect.xMax, parentRect.center.y);

                foreach (DialogueNode childNode in dialogue.GetAllChildren(parentNode))
                {
                    if (childNode == null) { continue; }

                    Rect childRect = childNode.GetRect();
                    var endPoint = new Vector2(childRect.xMin, childRect.center.y);
                    float bezierOffset = (endPoint.x - startPoint.x) * _bezierOffsetMultiplier;

                    painter.BeginPath();
                    painter.MoveTo(startPoint);
                    painter.BezierCurveTo(startPoint + Vector2.right * bezierOffset, endPoint + Vector2.left * bezierOffset, endPoint);
                    painter.Stroke();
                }
            }
        }
        
        private void InitializeLayerStyle()
        {
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
        }
    }
}
