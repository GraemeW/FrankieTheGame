using Unity.Properties;

namespace LowDefMustard.UIBox
{
    public sealed class ToggleChoiceModel : BindableModel
    {
        // State
        private string internalText = "";
        private bool internalIsOn = false;
        private bool internalIsHighlighted = false;

        [CreateProperty] public string text
        {
            get => internalText;
            set => SetProperty(ref internalText, value);
        }

        [CreateProperty] public bool isOn
        {
            get => internalIsOn;
            set => SetProperty(ref internalIsOn, value);
        }

        [CreateProperty] public bool isHighlighted
        {
            get => internalIsHighlighted;
            set => SetProperty(ref internalIsHighlighted, value);
        }
    }
}
