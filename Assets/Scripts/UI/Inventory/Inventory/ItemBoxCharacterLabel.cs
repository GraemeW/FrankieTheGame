using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    [UxmlElement]
    public sealed partial class ItemBoxCharacterLabel : ModelBoundLabel
    {
        public ItemBoxCharacterLabel() : base(nameof(ItemBoxModel.characterName)) { }
    }
}
