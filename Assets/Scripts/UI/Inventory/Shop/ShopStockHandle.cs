using System;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class ShopStockHandle : ChoiceHandle
    {
        // State
        private readonly ShopStockModel model;

        // Constructor
        public ShopStockHandle(UIToolkitBoxView view, ShopStockModel model, Action onChoose) : base(view, onChoose)
        {
            this.model = model;
        }

        // Overrides
        protected override VisualElement CreateChoiceElement() => new ShopStockElement { dataSource = model };
        protected override void SetHighlighted(bool enable) => model.isHighlighted = enable;
        public override void SetText(string text) => model.itemName = text;
    }
}
