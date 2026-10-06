using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class CashTransferModel : BindableModel
    {
        // State
        private string internalMessageText = "";

        [CreateProperty] public string messageText
        {
            get => internalMessageText;
            set => SetProperty(ref internalMessageText, value);
        }
    }
}
