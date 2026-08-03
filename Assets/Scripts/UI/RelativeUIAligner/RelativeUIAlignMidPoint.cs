using System;
using UnityEngine;

namespace Frankie.Utils.UI
{
    [CreateAssetMenu(fileName = "New Align to MidPoint", menuName = "RelativeUIAligner/AlignToMidPoint", order = 30)]
    public class RelativeUIAlignMidPoint : RelativeUIAligner
    {
        [SerializeField] private bool xAlignEnabled = false;
        [SerializeField] private bool yAlignEnabled = false;
        
        public override void AssertAlignment(RectTransform rectTransform, RectTransform xReference, RectTransform yReference, Func<RectTransform, Vector2, Vector2, Vector2> canvasMapper)
        {
            if (rectTransform == null || canvasMapper == null) { return; }
            
            float xPosition = rectTransform.anchoredPosition.x;
            float yPosition = rectTransform.anchoredPosition.y;

            if (xAlignEnabled && xReference != null)
            {
                float xCenter = canvasMapper.Invoke(xReference, xReference.rect.center, new Vector2(0.5f, 0.5f)).x;
                xPosition = xCenter;
            }

            if (yAlignEnabled && yReference != null)
            {
                float yCenter = canvasMapper.Invoke(yReference, yReference.rect.center, new Vector2(0.5f, 0.5f)).y;
                yPosition = yCenter;
            }
            rectTransform.anchoredPosition = new Vector2(xPosition, yPosition);
        }
    }
}
