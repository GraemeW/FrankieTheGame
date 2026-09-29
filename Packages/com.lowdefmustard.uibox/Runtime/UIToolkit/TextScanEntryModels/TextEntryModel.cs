using Unity.Properties;

namespace LowDefMustard.UIBox
{
    public class TextEntryModel : BindableModel
    {
        // State
        private string internalText = "";
        private bool internalIsRevealed = false;

        [CreateProperty] public string text
        {
            get => internalText;
            set => SetProperty(ref internalText, value);
        }

        [CreateProperty] public bool isRevealed
        {
            get => internalIsRevealed;
            set => SetProperty(ref internalIsRevealed, value);
        }
    }
}
