using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Rendering.UI
{
    [ExecuteAlways]
    public sealed class CinematicOverlayView : UIToolkitBoxView
    {
        // Note:  Binds in edit mode too (Timeline scrubbing) ~ so state is applied straight to the elements rather than through runtime data binding

        // State
        private float overlayOpacity = 1f;
        private string overlayText;
        private bool hasOverlayText;

        // Cached References
        private VisualElement overlayRoot;
        private CinematicOverlayLabel overlayLabel;

        #region PublicMethods
        public void SetOverlayOpacity(float opacity)
        {
            overlayOpacity = Mathf.Clamp01(opacity);
            ApplyOverlayOpacity();
        }

        public void SetOverlayText(string text) // Until set, the label keeps its authored (preview) text
        {
            overlayText = text;
            hasOverlayText = true;
            ApplyOverlayText();
        }
        #endregion

        #region ViewMethods
        protected override bool TryBindContent(VisualElement rootElement)
        {
            overlayLabel = rootElement.Q<CinematicOverlayLabel>();
            if (overlayLabel == null)
            {
                Debug.LogWarning($"{nameof(CinematicOverlayView)}[{name}]:  UXML has no {nameof(CinematicOverlayLabel)}.");
                return false;
            }

            overlayRoot = rootElement;
            ApplyOverlayOpacity();
            ApplyOverlayText();
            return true;
        }

        protected override void UnbindContent()
        {
            overlayRoot = null;
            overlayLabel = null;
        }
        #endregion

        #region PrivateMethods
        private void ApplyOverlayOpacity()
        {
            if (overlayRoot == null) { return; }
            overlayRoot.style.opacity = overlayOpacity;
        }

        private void ApplyOverlayText()
        {
            if (overlayLabel == null || !hasOverlayText) { return; }
            overlayLabel.text = overlayText;
        }
        #endregion
    }
}
