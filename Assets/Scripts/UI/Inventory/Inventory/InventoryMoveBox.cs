using Frankie.Combat;
using Frankie.Combat.UI;
using Frankie.Control;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public class InventoryMoveBox : InventoryBox
    {
        // Cached References
        Knapsack sourceKnapsack = null;
        int sourceSlot = 0;

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit partyCombatConduit, Knapsack sourceKnapsack, int sourceSlot, List<CharacterSlide> characterSlides = null)
        {
            this.sourceKnapsack = sourceKnapsack;
            this.sourceSlot = sourceSlot;
            Setup(standardPlayerInputCaller, partyCombatConduit, characterSlides);
        }

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

        public override InventoryItemField SetupItem(InventoryItemField inventoryItemFieldPrefab, Transform container, int selector)
        {
            InventoryItemField inventoryItemField = base.SetupItem(inventoryItemFieldPrefab, container, selector);
            if (!inventoryItemField.HasAction())
            {
                // Force setup actions -- even allow for choice if item does not exist (move to blank space)
                inventoryItemField.SetupButtonAction(this, ChooseItem, selector);
                inventoryItemChoiceOptions.Add(inventoryItemField);
            }

            return inventoryItemField;
        }
    }
}
