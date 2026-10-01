using Unity.Properties;

namespace LowDefMustard.UIBox
{
    public sealed class SliderChoiceModel : BindableModel
    {
        // State
        private string internalText = "";
        private float internalValue = 0f;
        private bool internalIsHighlighted = false;

        [CreateProperty] public string text
        {
            get => internalText;
            set => SetProperty(ref internalText, value);
        }

        [CreateProperty] public float value
        {
            get => internalValue;
            set => SetProperty(ref internalValue, value);
        }

        [CreateProperty] public bool isHighlighted
        {
            get => internalIsHighlighted;
            set => SetProperty(ref internalIsHighlighted, value);
        }
    }
}
