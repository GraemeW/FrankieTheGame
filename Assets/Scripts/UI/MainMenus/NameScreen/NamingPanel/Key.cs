using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public class Key
    {
        public UIChoiceButton keyboardButton { get; }
        public char character { get; }

        public Key(UIChoiceButton keyboardButton, char character)
        {
            this.keyboardButton = keyboardButton;
            this.character = character;
        }
    }
}
