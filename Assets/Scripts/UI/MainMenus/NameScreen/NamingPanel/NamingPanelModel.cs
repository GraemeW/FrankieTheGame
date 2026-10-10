using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class NamingPanelModel : BindableModel
    {
        // State
        private string internalInputText = "";
        private bool internalIsUpper = false;
        private int internalLetterColumns = NamingKeyboardLayout.defaultLetterColumns;
        private int internalSpecialColumns = NamingKeyboardLayout.defaultSpecialColumns;

        [CreateProperty] public string inputText
        {
            get => internalInputText;
            set => SetProperty(ref internalInputText, value);
        }

        [CreateProperty] public bool isUpper
        {
            get => internalIsUpper;
            set => SetProperty(ref internalIsUpper, value);
        }

        // Keys per row in each block of the keyboard
        [CreateProperty] public int letterColumns
        {
            get => internalLetterColumns;
            set => SetProperty(ref internalLetterColumns, value);
        }

        [CreateProperty] public int specialColumns
        {
            get => internalSpecialColumns;
            set => SetProperty(ref internalSpecialColumns, value);
        }
    }
}
