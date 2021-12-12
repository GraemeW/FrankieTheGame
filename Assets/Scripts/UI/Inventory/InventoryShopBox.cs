using Frankie.Combat;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public class InventoryShopBox : InventoryBox
    {
        // State
        InventoryItem inventoryItem = null;
        string messageNoSpace = "";

        // Cached References
        ShopBox shopBox = null;
        Shopper shopper = null;

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party, ShopBox shopBox, Shopper shopper, InventoryItem inventoryItem, string messageNoSpace)
        {
            // Standard behavior for purchasing a single item
            base.Setup(standardPlayerInputCaller, party);
            this.shopBox = shopBox;
            this.shopper = shopper;
            this.inventoryItem = inventoryItem;
            this.messageNoSpace = messageNoSpace;
        }

        protected override void ChooseCharacter(CombatParticipant character, bool initializeCursor = true)
        {
            UpdateKnapsackView(character);
            SetInventoryBoxState(InventoryBoxState.inCharacterSelection);

            Knapsack characterKnapsack = selectedCharacter.GetKnapsack();
            if (characterKnapsack == null) { return; }

            if (selectedCharacter.GetKnapsack().HasFreeSpace())
            {
                shopper.CompleteTransaction(ShopType.Buy, inventoryItem, characterKnapsack);
                shopBox.UpdateShopMessageToSuccess();
                Destroy(gameObject);
            }
            else
            {
                SpawnMessage(messageNoSpace);
            }
        }

        protected override void SoftChooseCharacter(CombatParticipant character)
        {
            UpdateKnapsackView(character);
        }

        private void SpawnMessage(string message)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(message);
            PassControl(dialogueBox);
        }
    }
}