using Unity.Properties;

namespace LowDefMustard.UIBox
{
    public class ChoiceEntryModel : TextEntryModel
    {
        // State
        private bool internalIsHighlighted = false;

        [CreateProperty] public bool isHighlighted
        {
            get => internalIsHighlighted;
            set => SetProperty(ref internalIsHighlighted, value);
        }
    }
}
