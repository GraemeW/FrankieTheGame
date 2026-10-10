using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class FrameFlavourPreviewFrame : VisualElement
    {
        // Use this frame to preview the flavour under the cursor ~ standard frames keep the saved flavour
        
        // Const Tunables
        private const string _ussClassName = "frame-flavour-preview";
        private const string _frameUssClassName = _ussClassName + "__frame";

        // State
        private bool internalHasPreview = false;
        private Color internalPreviewColour = Color.white;

        // Cached References
        private readonly FrameElement frame;

        [CreateProperty] public bool hasPreview
        {
            get => internalHasPreview;
            set
            {
                internalHasPreview = value;
                RefreshPreview();
            }
        }

        [CreateProperty] public Color previewColour
        {
            get => internalPreviewColour;
            set
            {
                internalPreviewColour = value;
                RefreshPreview();
            }
        }

        public FrameFlavourPreviewFrame()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;

            frame = new FrameElement();
            frame.AddToClassList(_frameUssClassName);
            Add(frame);

            SetBinding(nameof(hasPreview), UIToolkitBindings.ToTarget(nameof(FrameFlavourPanelModel.hasPreview)));
            SetBinding(nameof(previewColour), UIToolkitBindings.ToTarget(nameof(FrameFlavourPanelModel.previewColour)));
        }

        private void RefreshPreview()
        {
            if (internalHasPreview) { frame.SetLocalFlavour(internalPreviewColour); }
            else { frame.ClearLocalFlavour(); }
        }
    }
}
