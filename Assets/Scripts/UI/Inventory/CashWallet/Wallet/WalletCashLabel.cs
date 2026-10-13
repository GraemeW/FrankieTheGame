using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    [UxmlElement]
    public sealed partial class WalletCashLabel : Label
    {
        // State
        private int internalCash = -1; // Differs from any real balance, so the first bound value always replaces the authored preview text

        [CreateProperty] public int cash
        {
            get => internalCash;
            set
            {
                internalCash = value;
                text = $"${value:N0}";
            }
        }

        public WalletCashLabel()
        {
            SetBinding(nameof(cash), UIToolkitBindings.ToTarget(nameof(WalletModel.cash)));
        }
    }
}
