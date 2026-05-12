using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;
using Frankie.Combat.UI;
using Frankie.Control;
using Frankie.Stats;

namespace Frankie.Inventory.UI
{
    public class InventoryMoveBox : InventoryBox
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
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit partyCombatConduit, Knapsack setSourceKnapsack, int setSourceSlot, List<CharacterSlide> characterSlides)
        {
            sourceKnapsack = setSourceKnapsack;
            sourceSlot = setSourceSlot;
            Setup(standardPlayerInputCaller, partyCombatConduit, characterSlides);
        }
        
        public override InventoryItemField SetupItem(InventoryItemField setInventoryItemFieldPrefab, Transform container, int selector)
        {
            InventoryItemField inventoryItemField = base.SetupItem(setInventoryItemFieldPrefab, container, selector);
            if (!inventoryItemField.HasAction())
            {
                // Force setup actions -- even allow for choice if item does not exist (move to blank space)
                inventoryItemField.SetupButtonAction(this, ChooseItem, selector);
                inventoryItemChoiceOptions.Add(inventoryItemField);
            }

            return inventoryItemField;
        }
        #endregion

        #region ProtectedPrivateMethods
        protected override void ChooseItem(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }

            sourceKnapsack.MoveItem(sourceSlot, selectedKnapsack, inventorySlot);
            Destroy(gameObject);
        }

        protected override void ListenToKnapsack(bool enable)
        {
            // Skip listening to knapsack -- window only exists momentarily and then killed
            return;
        }
        #endregion


    }
}
