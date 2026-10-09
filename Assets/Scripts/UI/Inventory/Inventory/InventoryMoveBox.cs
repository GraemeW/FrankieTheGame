using System.Collections.Generic;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using Frankie.Combat.UI;
using Frankie.Stats;

namespace Frankie.Inventory.UI
{
    public sealed class InventoryMoveBox : InventoryBox
    {
        // State
        private int sourceSlot = 0;
        // Cached References
        private Knapsack sourceKnapsack;

        #region LocalizationMethods
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            // Note:  Standard configuration re-uses localization keys from InventoryBox
            // Here we only return unique to this child script to prevent deletion of InventoryBox keys
            // Overridden standard Inventory entries would need to be manually deleted
            return new List<TableEntryReference>();
        }
        #endregion

        #region PublicMethods
        public void Setup(BaseController baseController, PartyCombatConduit partyCombatConduit, Knapsack setSourceKnapsack, int setSourceSlot, List<CharacterSlideHandle> characterSlides)
        {
            sourceKnapsack = setSourceKnapsack;
            sourceSlot = setSourceSlot;
            Setup(baseController, partyCombatConduit, characterSlides);
        }
        #endregion

        #region ProtectedPrivateMethods
        protected override bool ConfigureSlot(InventorySlotModel slotModel, KnapsackSlot knapsackSlot)
        {
            if (base.ConfigureSlot(slotModel, knapsackSlot)) { return true; }

            // Blank space is a valid destination - items squish up after a move, so only the first blank slot is offered
            bool isFirstFreeSlot = knapsackSlot.index == selectedKnapsackModel.firstFreeSlot;
            slotModel.isShown = isFirstFreeSlot;
            return isFirstFreeSlot;
        }

        protected override void ChooseItem(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }

            sourceKnapsack.MoveItem(sourceSlot, selectedKnapsack, inventorySlot);
            Destroy(gameObject);
        }
        #endregion
    }
}
