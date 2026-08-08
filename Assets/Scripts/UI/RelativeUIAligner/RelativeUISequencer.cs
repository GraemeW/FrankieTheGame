using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    [RequireComponent(typeof(LayoutElement))]
    [RequireComponent(typeof(RectTransform))]
    public class RelativeUISequencer : MonoBehaviour
    {
        // Use this component for aligning individual floating elements to fixed UI elements
        // LayoutElement should thus be ignoreLayout = true (enforced in Awake)
        
        // Tunables
        [SerializeField] private List<RelativeUIAligner> aligners = new();
        [SerializeField] private RectTransform xReference;
        [SerializeField] private RectTransform yReference;
        
        // State
        private RectTransform rectTransform;
        private RectTransform canvasRectTransform;
        
        private void Awake()
        {
            var layoutElement = GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            
            rectTransform = GetComponent<RectTransform>();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) { canvasRectTransform = canvas.GetComponent<RectTransform>(); }
        }
        
        public void AssertAlignment()
        {
            if (canvasRectTransform == null) { return; }
            foreach (RelativeUIAligner aligner in aligners.Where(aligner => aligner != null))
            {
                aligner.AssertAlignment(rectTransform, xReference, yReference, MapPointToCanvas);
            }
        }

        private Vector2 MapPointToCanvas(RectTransform rectTransformForMapping, Vector2 targetPoint, Vector2 pivotReference)
        {
            if (canvasRectTransform == null || rectTransformForMapping == null) { return Vector2.zero; }
            
            Vector3 worldPoint = rectTransformForMapping.TransformPoint(targetPoint);
            Vector2 canvasPoint = canvasRectTransform.InverseTransformPoint(worldPoint);
            
            var clampedReference = new Vector2(Mathf.Clamp01(pivotReference.x), Mathf.Clamp01(pivotReference.y));
            Vector2 offsetScaler = canvasRectTransform.pivot - clampedReference;
            Vector2 offset = offsetScaler * canvasRectTransform.rect.size;
            
            return canvasPoint + offset;
        }
    }
}
