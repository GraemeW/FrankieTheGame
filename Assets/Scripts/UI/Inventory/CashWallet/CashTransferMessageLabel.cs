using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    [UxmlElement]
    public sealed partial class CashTransferMessageLabel : ModelBoundLabel
    {
        public CashTransferMessageLabel() : base(nameof(CashTransferModel.messageText)) { }
    }
}
