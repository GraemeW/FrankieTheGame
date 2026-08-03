using System;
using UnityEngine;

namespace Frankie.Utils.UI
{
    [CreateAssetMenu(fileName = "New Align Width to Edge", menuName = "RelativeUIAligner/AlignDimensionToEdge", order = 30)]
    public class RelativeUIStretchToEdge : RelativeUIAligner
    {
        [SerializeField] private bool xAlignEnabled = false;
        [SerializeField] private bool isLeftEdge = true;
        [SerializeField] private float xPadding = 50f;
        [SerializeField] private bool yAlignEnabled = false;
        [SerializeField] private bool isTopEdge = true;
        [SerializeField] private float yPadding = 50f;
        
        public override void AssertAlignment(RectTransform rectTransform, RectTransform xReference, RectTransform yReference, Func<RectTransform, Vector2, Vector2, Vector2> canvasMapper)
        {
            if (rectTransform == null || canvasMapper == null) { return; }

            float xTarget = 0f;
            var xAlign = XAlignment.None;
            if (xAlignEnabled && xReference != null)
            {
                if (isLeftEdge)
                {
                    xAlign = XAlignment.Left;
                    var pivotReference = new Vector2(0f, 0.5f);
                    xTarget = canvasMapper.Invoke(xReference, new Vector2(xReference.rect.xMin, xReference.rect.center.y), pivotReference).x - xPadding;
                }
                else
                {
                    xAlign = XAlignment.Right;
                    var pivotReference = new Vector2(1f, 0.5f);
                    xTarget = canvasMapper.Invoke(xReference, new Vector2(xReference.rect.xMax, xReference.rect.center.y), pivotReference).x + xPadding; 
                }
            }
            
            float yTarget = 0f;
            var yAlign = YAlignment.None;
            if (yAlignEnabled && yReference != null)
            {
                if (isTopEdge)
                {
                    yAlign = YAlignment.Top;
                    var pivotReference = new Vector2(0.5f, 1.0f);
                    yTarget = canvasMapper.Invoke(yReference, new Vector2(yReference.rect.center.x, yReference.rect.yMax), pivotReference).y + yPadding;
                }
                else
                {
                    yAlign = YAlignment.Bottom;
                    var pivotReference = new Vector2(0.5f, 0f);
                    yTarget = canvasMapper.Invoke(yReference, new Vector2(yReference.rect.center.x, yReference.rect.yMin), pivotReference).y - yPadding; 
                }
            }
            
            StretchToTarget(ref rectTransform, xAlign, xTarget, yAlign, yTarget);
        }
        
        private enum XAlignment { Left, Right, None }
        private enum YAlignment { Bottom, Top, None }
        
        private static void StretchToTarget(ref RectTransform rectTransform, XAlignment xAlign, float xTarget, YAlignment yAlign, float yTarget)
        {
            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;
            Vector2 sizeDelta = rectTransform.sizeDelta;
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            
            if (xAlign == XAlignment.Left)
            {
                anchorMin.x = 0f;
                anchorMax.x = 0f;
                sizeDelta.x = xTarget;
                anchoredPosition.x = xTarget * 0.5f;
            }
            else if (xAlign == XAlignment.Right)
            {
                anchorMin.x = 1f;
                anchorMax.x = 1f;
                sizeDelta.x = xTarget;
                anchoredPosition.x = -xTarget * 0.5f;
            }
            
            if (yAlign == YAlignment.Bottom)
            {
                anchorMin.y = 0f;
                anchorMax.y = 0f;
                sizeDelta.y = yTarget;
                anchoredPosition.y = yTarget * 0.5f;
            }
            else if (yAlign == YAlignment.Top)
            {
                anchorMin.y = 1f;
                anchorMax.y = 1f;
                sizeDelta.y = yTarget;
                anchoredPosition.y = -yTarget * 0.5f;
            }
            
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}
