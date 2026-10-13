using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class InventorySlotModel : BindableModel
    {
        // State
        private string internalText = "";
        private bool internalIsHighlighted = false;
        private bool internalIsEquipped = false;
        private bool internalIsDimmed = false;
        private bool internalIsShown = false;

        [CreateProperty] public string text
        {
            get => internalText;
            set => SetProperty(ref internalText, value);
        }

        [CreateProperty] public bool isHighlighted
        {
            get => internalIsHighlighted;
            set => SetProperty(ref internalIsHighlighted, value);
        }

        [CreateProperty] public bool isEquipped
        {
            get => internalIsEquipped;
            set => SetProperty(ref internalIsEquipped, value);
        }

        [CreateProperty] public bool isDimmed // e.g. an item that cannot be used for the box's purpose
        {
            get => internalIsDimmed;
            set => SetProperty(ref internalIsDimmed, value);
        }

        [CreateProperty] public bool isShown // Hidden rows collapse (e.g. empty knapsack slots)
        {
            get => internalIsShown;
            set => SetProperty(ref internalIsShown, value);
        }
    }
}
