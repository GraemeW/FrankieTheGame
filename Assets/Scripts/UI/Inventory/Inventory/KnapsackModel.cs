using System;
using System.Collections.Generic;
using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class KnapsackModel : BindableModel, IDisposable
    {
        // Live readout of a knapsack, shared by every screen that shows a character's items
        // Note:  Values are raw (items, not display text); screens and rows decide how to present them

        // State
        private IReadOnlyList<KnapsackSlot> internalSlots;
        private bool isDisposed = false;

        // Constructor
        public KnapsackModel(Knapsack knapsack)
        {
            this.knapsack = knapsack;
            internalSlots = ReadSlots();
            knapsack.knapsackUpdated += HandleKnapsackUpdated;
        }

        #region BoundProperties
        // Replaced wholesale on every knapsack update (equipped flags included)
        [CreateProperty] public IReadOnlyList<KnapsackSlot> slots
        {
            get => internalSlots;
            private set => SetProperty(ref internalSlots, value);
        }
        #endregion

        #region PublicMethods
        public Knapsack knapsack { get; }

        public int firstFreeSlot
        {
            get
            {
                foreach (KnapsackSlot slot in internalSlots) { if (!slot.hasItem) { return slot.index; } }
                return -1;
            }
        }

        public void Dispose()
        {
            if (isDisposed) { return; }
            isDisposed = true;
            if (knapsack != null) { knapsack.knapsackUpdated -= HandleKnapsackUpdated; }
        }
        #endregion

        #region PrivateMethods
        private void HandleKnapsackUpdated() => slots = ReadSlots();

        private KnapsackSlot[] ReadSlots()
        {
            var knapsackSlots = new KnapsackSlot[knapsack.GetSize()];
            for (int slot = 0; slot < knapsackSlots.Length; slot++)
            {
                knapsackSlots[slot] = new KnapsackSlot(slot, knapsack.GetItemInSlot(slot), knapsack.IsItemInSlotEquipped(slot));
            }
            return knapsackSlots;
        }
        #endregion
    }
}
