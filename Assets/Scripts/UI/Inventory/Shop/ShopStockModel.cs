using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class ShopStockModel : BindableModel
    {
        // State
        private string internalItemName = "";
        private string internalPriceText = "";
        private bool internalIsHighlighted = false;

        [CreateProperty] public string itemName
        {
            get => internalItemName;
            set => SetProperty(ref internalItemName, value);
        }

        [CreateProperty] public string priceText
        {
            get => internalPriceText;
            set => SetProperty(ref internalPriceText, value);
        }

        [CreateProperty] public bool isHighlighted
        {
            get => internalIsHighlighted;
            set => SetProperty(ref internalIsHighlighted, value);
        }
    }
}
