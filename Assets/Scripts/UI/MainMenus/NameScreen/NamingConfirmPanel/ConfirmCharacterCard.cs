using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public readonly struct ConfirmCharacterCard
    {
        public readonly string characterName;
        public readonly SpriteModel spriteModel; // Null when the character has nothing to show

        public ConfirmCharacterCard(string characterName, SpriteModel spriteModel)
        {
            this.characterName = characterName;
            this.spriteModel = spriteModel;
        }
    }
}
