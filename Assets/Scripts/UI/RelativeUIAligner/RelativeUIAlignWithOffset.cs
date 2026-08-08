using System;
using UnityEngine;

namespace Frankie.Utils.UI
{
    [CreateAssetMenu(fileName = "New Align To Target with Offset", menuName = "UI/RelativeUIAligner/AlignTargetWithOffset", order = 30)]
    public class RelativeUIAlignWithOffset : RelativeUIAligner
    {
        [SerializeField] private bool xAlignEnabled = false;
        [SerializeField] private bool yAlignEnabled = false;
        [SerializeField][Tooltip("0-to-1 each x/y to define point of interest on rect - 0.5/0.5 is mid-pt")] private Vector2 relativeRectPosition = new(0.5f, 0.5f);
        [SerializeField] private Vector2 offset = Vector2.zero;
        
        public override void AssertAlignment(RectTransform rectTransform, RectTransform xReference, RectTransform yReference, Func<RectTransform, Vector2, Vector2, Vector2> canvasMapper)
        {
            if (rectTransform == null || canvasMapper == null) { return; }
            if (xReference == null && yReference == null) { return; }

            float xTarget = rectTransform.anchoredPosition.x;
            float yTarget = rectTransform.anchoredPosition.y;
            
            if (xAlignEnabled && xReference != null) { xTarget = canvasMapper.Invoke(xReference, relativeRectPosition, new Vector2(0.5f, 0.5f)).x + offset.x; }
            if (yAlignEnabled && yReference != null) { yTarget = canvasMapper.Invoke(yReference, relativeRectPosition, new Vector2(0.5f, 0.5f)).y + offset.y; }
            rectTransform.anchoredPosition = new Vector2(xTarget, yTarget);
        }
    }
}
