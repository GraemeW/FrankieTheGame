using System;
using UnityEngine;

namespace Frankie.Utils.UI
{
    [CreateAssetMenu(fileName = "New Align MidPoint to Edge", menuName = "RelativeUIAligner/AlignMidPointToEdge", order = 30)]
    public class RelativeUIAlignMidPointToEdge : RelativeUIAligner
    {
        [SerializeField] private bool xAlignEnabled = false;
        [SerializeField] private bool isLeftEdge = true;
        [SerializeField] private float xPadding = 200f;
        [SerializeField] private bool yAlignEnabled = false;
        [SerializeField] private bool isTopEdge = true;
        [SerializeField] private float yPadding = 200f;
        
        public override void AssertAlignment(RectTransform rectTransform, RectTransform xReference, RectTransform yReference, Func<RectTransform, Vector2, Vector2, Vector2> canvasMapper)
        {
            if (rectTransform == null || canvasMapper == null) { return; }
            
            float xPosition = rectTransform.anchoredPosition.x;
            float yPosition = rectTransform.anchoredPosition.y;

            if (xAlignEnabled && xReference != null)
            {
                float xMin = canvasMapper.Invoke(xReference, new Vector2(xReference.rect.xMin, xReference.rect.center.y), new Vector2(0.5f, 0.5f)).x;
                float xMax = canvasMapper.Invoke(xReference, new Vector2(xReference.rect.xMax, xReference.rect.center.y), new Vector2(0.5f, 0.5f)).x;
                xPosition = isLeftEdge ? xMin - xPadding : xMax + xPadding;
            }

            if (yAlignEnabled && yReference != null)
            {
                float yMin = canvasMapper.Invoke(yReference, new Vector2(yReference.rect.center.x, yReference.rect.yMin), new Vector2(0.5f, 0.5f)).y;
                float yMax = canvasMapper.Invoke(yReference, new Vector2(yReference.rect.center.x, yReference.rect.yMax), new Vector2(0.5f, 0.5f)).y;
                yPosition = isTopEdge ? yMax + yPadding : yMin - yPadding;
            }
            rectTransform.anchoredPosition = new Vector2(xPosition, yPosition);
        }
    }
}
