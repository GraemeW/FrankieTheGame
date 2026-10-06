using System;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class InventorySlotHandle : ChoiceHandle
    {
        // Persistent item row:  the owning box re-texts the model as the knapsack/equipment changes (rows are not rebuilt)
        
        // Constructor
        public InventorySlotHandle(UIToolkitBoxView view, bool isRightColumn, Action onChoose) : base(view, onChoose, null, isRightColumn ? typeof(InventorySlotRightColumn) : typeof(InventorySlotLeftColumn)) { }

        #region PublicMethods
        public InventorySlotModel model { get; } = new();
        #endregion

        #region ChoiceHandle
        protected override VisualElement CreateChoiceElement() => new InventorySlotElement { dataSource = model };
        protected override void SetHighlighted(bool enable) => model.isHighlighted = enable;
        public override void SetText(string text) => model.text = text;
        #endregion
    }
}
