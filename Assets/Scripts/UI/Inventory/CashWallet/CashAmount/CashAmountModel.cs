using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class CashAmountModel : BindableModel
    {
        // Const Tunables
        public const int digitCount = 9;
        public const int noCaret = -1;

        // State
        private int internalAmount = 0;
        private int internalCaretDigit = noCaret;

        [CreateProperty] public int amount
        {
            get => internalAmount;
            set => SetProperty(ref internalAmount, value);
        }

        [CreateProperty] public int caretDigit // Digit under the cursor, as a power of ten (0 = ones); noCaret while the amount is not highlighted
        {
            get => internalCaretDigit;
            set => SetProperty(ref internalCaretDigit, value);
        }
    }
}
