using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Localization;
using Frankie.Rendering.UI;
using Frankie.Utils.Localization;

namespace Frankie.Rendering
{
    [ExecuteAlways]
    [RequireComponent(typeof(CinematicOverlayView))]
    public sealed class CinematicOverlay : MonoBehaviour, ILocalizable
    {
        // Note:  Runs in edit mode so the overlay shows (and fades) while scrubbing a Timeline

        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Speech, true)] private LocalizedString localizedOverlayText;
        [SerializeField][Range(0f, 1f)][Tooltip("Overlay opacity - keyed by Timeline / Animator")] private float alpha = 1f;

        // State
        private float appliedAlpha = -1f;

        // Cached References
        private CinematicOverlayView overlayView;

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Speech;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedOverlayText.TableEntryReference,
            };
        }
        #endregion
        
        #region UnityMethods
        private void OnEnable()
        {
            ApplyAlpha();
        }

        private void Start()
        {
            if (TryGetOverlayView(out CinematicOverlayView view)) { view.SetOverlayText(localizedOverlayText.GetLocalizedString()); }
        }

        private void LateUpdate()
        {
            // Note:  Animated fields are written without a setter - OnDidApplyAnimationProperties covers Animator / Timeline;
            //        this compare is the catch-all for any source that skips that message
            if (!Mathf.Approximately(alpha, appliedAlpha)) { ApplyAlpha(); }
        }

        private void OnDidApplyAnimationProperties() => ApplyAlpha();

        private void OnValidate() => ApplyAlpha();

        private void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
        }
        #endregion

        #region PrivateMethods
        private void ApplyAlpha()
        {
            if (!TryGetOverlayView(out CinematicOverlayView view)) { return; }

            appliedAlpha = alpha;
            view.SetOverlayOpacity(alpha);
        }

        private bool TryGetOverlayView(out CinematicOverlayView view)
        {
            if (overlayView == null) { TryGetComponent(out overlayView); }
            view = overlayView;
            return view != null;
        }
        #endregion
    }
}
