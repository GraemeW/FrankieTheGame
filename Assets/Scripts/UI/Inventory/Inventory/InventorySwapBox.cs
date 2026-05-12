using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Utils;
using Frankie.Control;
using Frankie.Stats;
using Frankie.Speech.UI;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    public class InventorySwapBox : InventoryBox
    {
        // Tunables
        [Header("Inventory-Swap Messages")]
        [Header("Include {0} for item name to swap in & {1} for item name to chuck")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageConfirmThrowOut;

        // Cached References
        private InventoryItem swapItem;
        private Action swapSuccessAction;

        #region LocalizationMethods
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            // Note:  Standard configuration re-uses localization keys from InventoryBox 
            // Here we only return unique to this child script to prevent deletion of InventoryBox keys
            // Overridden standard Inventory entries would need to be manually deleted
            return new List<TableEntryReference>
            {
                localizedMessageConfirmThrowOut.TableEntryReference,
            };
        }
        #endregion
        
        #region PublicMethods
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit partyCombatConduit, InventoryItem setSwapItem, Action setSwapSuccessAction)
        {
            swapItem = setSwapItem;
            swapSuccessAction = setSwapSuccessAction;
            Setup(standardPlayerInputCaller, partyCombatConduit);
        }
        #endregion

        #region ProtectedPrivateMethods
        protected override void ChooseItem(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }

            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            InventoryItem selectedItem = selectedKnapsack.GetItemInSlot(inventorySlot);
            string selectedItemName = selectedItem != null ? selectedItem.GetDisplayName() : ""; // Edge case (should not happen)

            dialogueOptionBox.Setup(string.Format(localizedMessageConfirmThrowOut.GetSafeLocalizedString(), swapItem.GetDisplayName(), selectedItemName));
            var choiceActionPairs = new List<ChoiceActionPair>();
            if (swapSuccessAction != null)
            {
                choiceActionPairs.Add(new ChoiceActionPair(localizedConfirmChoiceAffirmative.GetSafeLocalizedString(), () =>
                {
                    SwapItem(dialogueOptionBox, true, inventorySlot);
                    swapSuccessAction.Invoke();
                    Destroy(gameObject);
                }));
            }
            else
            {
                choiceActionPairs.Add(new ChoiceActionPair(localizedConfirmChoiceAffirmative.GetSafeLocalizedString(), () =>
                {
                    SwapItem(dialogueOptionBox, true, inventorySlot);
                    Destroy(gameObject);
                }));
            }
            choiceActionPairs.Add(new ChoiceActionPair(localizedConfirmChoiceNegative.GetSafeLocalizedString(), () => { SwapItem(dialogueOptionBox, false, inventorySlot); }));
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);

            PassControl(dialogueOptionBox);
        }
        
        protected override void ListenToKnapsack(bool enable)
        {
            // Skip listening to knapsack -- window only exists momentarily and then killed
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
        #endregion
    }
}
