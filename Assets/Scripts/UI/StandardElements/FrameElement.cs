using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Saving;

namespace Frankie.Utils.UI
{
    [UxmlElement]
    public sealed partial class FrameElement : VisualElement
    {
        // Note: Frame image & slicing are defined in USS (.frame) - this element only drives the tint colour
        // Tint is the global frame flavour, adjusted by USS custom properties set on the frame itself (e.g. `.my-state .frame { ... }`):
        //   --frame-tint-factor:  brightness scaling (range 0.5 - 1.5, as per UIFrame)
        //   --frame-tint-override:  local colour in place of the frame flavour (e.g. a targeted character slide)
        // A local flavour stands in for the global one on this frame only

        // Const
        private const string _ussClassName = "frame";
        private static readonly CustomStyleProperty<float> _tintFactorProperty = new("--frame-tint-factor");
        private static readonly CustomStyleProperty<Color> _tintOverrideProperty = new("--frame-tint-override");

        // State
        private Color frameFlavourColour = Color.white;
        private float tintFactor = 1f;
        private Color? tintOverride; // frame highlights in gameplay
        
        private bool hasLocalFlavour = false;
        private Color localFlavourColour = Color.white; // frame in flavour selection 

        public FrameElement()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            RegisterCallback<CustomStyleResolvedEvent>(HandleCustomStyleResolved);
        }

        #region PublicMethods
        public void SetLocalFlavour(Color setLocalFlavourColour)
        {
            hasLocalFlavour = true;
            localFlavourColour = setLocalFlavourColour;
            ApplyTint();
        }

        public void ClearLocalFlavour()
        {
            hasLocalFlavour = false;
            ApplyTint();
        }
        #endregion

        #region EventHandlers
        private void HandleAttachToPanel(AttachToPanelEvent attachToPanelEvent)
        {
            if (!Application.isPlaying) { return; }

            frameFlavourColour = UIFrame.GetFrameFlavourColour();
            ApplyTint();
            PlayerPrefsController.frameFlavourUpdated -= SetFrameFlavour;
            PlayerPrefsController.frameFlavourUpdated += SetFrameFlavour;
        }

        private void HandleDetachFromPanel(DetachFromPanelEvent detachFromPanelEvent)
        {
            PlayerPrefsController.frameFlavourUpdated -= SetFrameFlavour;
        }

        private void SetFrameFlavour(Color setFrameFlavourColour)
        {
            frameFlavourColour = setFrameFlavourColour;
            ApplyTint();
        }

        private void HandleCustomStyleResolved(CustomStyleResolvedEvent customStyleResolvedEvent)
        {
            ICustomStyle newStyle = customStyleResolvedEvent.customStyle;
            tintFactor = newStyle.TryGetValue(_tintFactorProperty, out float resolvedTintFactor) ? resolvedTintFactor : 1f;
            tintOverride = newStyle.TryGetValue(_tintOverrideProperty, out Color resolvedTintOverride) ? resolvedTintOverride : null;
            ApplyTint();
        }
        #endregion

        #region PrivateMethods
        private void ApplyTint()
        {
            if (!Application.isPlaying || panel == null) { return; } // Edit-time (UI Builder) previews use the USS default tint
            style.unityBackgroundImageTintColor = UIFrame.GetScaledColour(tintOverride ?? (hasLocalFlavour ? localFlavourColour : frameFlavourColour), tintFactor);
        }
        #endregion
    }
}
