using Frankie.Control;
using Frankie.Stats;
using Frankie.Speech.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using System;

namespace Frankie.Inventory.UI
{
    public class InventorySwapBox : InventoryBox
    {
        // Tunables
        [Header("Swap Specific Fields")]
        [Tooltip("Include {0} for item name to swap in & {1} for item name to chuck")] [SerializeField] string messageConfirmThrowOut = "Are you sure you want to pick up {0} and abandon {1}?";
        [SerializeField] string optionSwapItemAffirmative = "Yeah";
        [SerializeField] string optionSwapItemNegative = "Nah";

        // Cached References
        InventoryItem swapItem = null;
        Action swapSuccessAction = null;

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit partyCombatConduit, InventoryItem swapItem, Action swapSuccessAction)
        {
            this.swapItem = swapItem;
            this.swapSuccessAction = swapSuccessAction;
            Setup(standardPlayerInputCaller, partyCombatConduit);
        }

        protected override void ChooseItem(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }

            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            InventoryItem selectedItem = selectedKnapsack.GetItemInSlot(inventorySlot);
            string selectedItemName = selectedItem != null ? selectedItem.GetDisplayName() : ""; // Edge case (should not happen)

            dialogueOptionBox.Setup(string.Format(messageConfirmThrowOut, swapItem.GetDisplayName(), selectedItemName));
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            if (swapSuccessAction != null)
            {
                choiceActionPairs.Add(new ChoiceActionPair(optionSwapItemAffirmative, () => { 
                    SwapItem(dialogueOptionBox, true, inventorySlot); 
                    swapSuccessAction.Invoke(); 
                    Destroy(gameObject); }));
            }
            else
            {
                choiceActionPairs.Add(new ChoiceActionPair(optionSwapItemAffirmative, () => { 
                    SwapItem(dialogueOptionBox, true, inventorySlot); 
                    Destroy(gameObject); }));
            }
            choiceActionPairs.Add(new ChoiceActionPair(optionSwapItemNegative, () => { SwapItem(dialogueOptionBox, false, inventorySlot); }));
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);

            PassControl(dialogueOptionBox);
        }

        private void SwapItem(DialogueOptionBox confirmationBox, bool execute, int inventorySlot)
        {
            if (execute)
            {
                selectedKnapsack.RemoveFromSlot(inventorySlot, false);
                selectedKnapsack.AddToFirstEmptySlot(swapItem, true);
            }

            Destroy(confirmationBox.gameObject);
        }

        protected override void ListenToKnapsack(bool enable)
        {
            // Skip listening to knapsack -- window only exists momentarily and then killed
            return;
        }
    }
}