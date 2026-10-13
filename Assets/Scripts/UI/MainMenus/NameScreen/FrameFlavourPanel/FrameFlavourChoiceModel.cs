using Unity.Properties;
using UnityEngine;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class FrameFlavourChoiceModel : BindableModel
    {
        // State
        private string internalText = "";
        private Color internalColour = Color.white;
        private bool internalIsHighlighted = false;

        [CreateProperty] public string text
        {
            get => internalText;
            set => SetProperty(ref internalText, value);
        }

        [CreateProperty] public Color colour
        {
            get => internalColour;
            set => SetProperty(ref internalColour, value);
        }

        [CreateProperty] public bool isHighlighted
        {
            get => internalIsHighlighted;
            set => SetProperty(ref internalIsHighlighted, value);
        }
    }
}
