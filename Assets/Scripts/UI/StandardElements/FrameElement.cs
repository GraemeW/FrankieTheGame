using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Saving;

namespace Frankie.Utils.UI
{
    // Note: Frame image & slicing are defined in USS (.frame)
    // This element only drives the tint colour
    [UxmlElement]
    public sealed partial class FrameElement : VisualElement
    {
        private const string _ussClassName = "frame";

        // Tunables
        [UxmlAttribute] public float colourModifyFactor { get; set; } = 1.0f; // Range 0.5 - 1.5, as per UIFrame

        public FrameElement()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }

        #region EventHandlers
        private void HandleAttachToPanel(AttachToPanelEvent attachToPanelEvent)
        {
            if (!Application.isPlaying) { return; } // Edit-time (UI Builder) previews use the USS default tint

            SetFrameFlavour(UIFrame.GetFrameFlavourColour());
            PlayerPrefsController.frameFlavourUpdated -= SetFrameFlavour;
            PlayerPrefsController.frameFlavourUpdated += SetFrameFlavour;
        }

        private void HandleDetachFromPanel(DetachFromPanelEvent detachFromPanelEvent)
        {
            PlayerPrefsController.frameFlavourUpdated -= SetFrameFlavour;
        }

        private void SetFrameFlavour(Color frameFlavourColour)
        {
            style.unityBackgroundImageTintColor = UIFrame.GetScaledColour(frameFlavourColour, colourModifyFactor);
        }
        #endregion
    }
}
