using System;
using UnityEngine;

namespace Frankie.Utils.UI
{
    [CreateAssetMenu(fileName = "New Copy from Reference", menuName = "RelativeUIAligner/CopyFromReference", order = 30)]
    public class RelativeUICopyReference : RelativeUIAligner
    {
        [SerializeField] private bool xAlignEnabled = false;
        [SerializeField] private bool yAlignEnabled = false;
        [SerializeField] private Vector2 offset = Vector2.zero;
        
        public override void AssertAlignment(RectTransform rectTransform, RectTransform xReference, RectTransform yReference, Func<RectTransform, Vector2, Vector2, Vector2> canvasMapper)
        {
            if (rectTransform == null || canvasMapper == null) { return; }
            if (xReference == null && yReference == null) { return; }
            
            float xTarget = rectTransform.anchoredPosition.x;
            float xWidth = rectTransform.sizeDelta.x;
            float yTarget = rectTransform.anchoredPosition.y;
            float yWidth = rectTransform.sizeDelta.y;

            Vector2 midPoint = new Vector2(0.5f, 0.5f);
            if (xAlignEnabled && xReference != null)
            {
                xTarget = canvasMapper.Invoke(xReference, midPoint, midPoint).x + offset.x;
                xWidth = xReference.sizeDelta.x;
            }

            if (yAlignEnabled && yReference != null)
            {
                yTarget = canvasMapper.Invoke(yReference, midPoint, midPoint).y + offset.y;
                yWidth = yReference.sizeDelta.y;
            }
            
            rectTransform.anchoredPosition = new Vector2(xTarget, yTarget);
            rectTransform.sizeDelta = new Vector2(xWidth, yWidth);
        }
    }
}
