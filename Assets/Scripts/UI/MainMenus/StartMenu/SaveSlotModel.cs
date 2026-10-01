using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class SaveSlotModel : BindableModel
    {
        // State
        private string internalIndexText = "";
        private string internalCharacterName = "";
        private string internalLevelLabel = "";
        private string internalLevelText = "";
        private bool internalIsHighlighted = false;

        [CreateProperty] public string indexText
        {
            get => internalIndexText;
            set => SetProperty(ref internalIndexText, value);
        }

        [CreateProperty] public string characterName
        {
            get => internalCharacterName;
            set => SetProperty(ref internalCharacterName, value);
        }

        [CreateProperty] public string levelLabel
        {
            get => internalLevelLabel;
            set => SetProperty(ref internalLevelLabel, value);
        }

        [CreateProperty] public string levelText
        {
            get => internalLevelText;
            set => SetProperty(ref internalLevelText, value);
        }

        [CreateProperty] public bool isHighlighted
        {
            get => internalIsHighlighted;
            set => SetProperty(ref internalIsHighlighted, value);
        }
    }
}
