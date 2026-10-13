using Unity.Properties;
using UnityEngine;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class FrameFlavourPanelModel : BindableModel
    {
        // State
        private string internalQuestionText = "";
        private bool internalHasPreview = false;
        private Color internalPreviewColour = Color.white;

        [CreateProperty] public string questionText
        {
            get => internalQuestionText;
            set => SetProperty(ref internalQuestionText, value);
        }
        
        [CreateProperty] public bool hasPreview
        {
            get => internalHasPreview;
            set => SetProperty(ref internalHasPreview, value);
        }

        [CreateProperty] public Color previewColour
        {
            get => internalPreviewColour;
            set => SetProperty(ref internalPreviewColour, value);
        }
    }
}
