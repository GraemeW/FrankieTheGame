using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    [UxmlElement]
    public sealed partial class ShopMessageLabel : ModelBoundLabel
    {
        public ShopMessageLabel() : base(nameof(ShopMessageModel.messageText)) { }
    }
}
