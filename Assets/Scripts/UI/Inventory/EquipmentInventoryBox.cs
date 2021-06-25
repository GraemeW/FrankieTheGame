using Frankie.Combat.UI;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public class EquipmentInventoryBox : InventoryBox
    {
        [Header("EquipmentInventory Messages")]
        [SerializeField] string messageCannotEquip = "This item cannot be equipped.";

        // Cached References
        EquipmentBox equipmentBox = null;
        EquipLocation equipLocation = EquipLocation.None;

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party, EquipmentBox equipmentBox, EquipLocation equipLocation, List<CharacterSlide> characterSlides = null)
        {
            this.equipmentBox = equipmentBox;
            this.equipLocation = equipLocation;
            Setup(standardPlayerInputCaller, party, characterSlides);
        }

        protected override List<ChoiceActionPair> GetChoiceActionPairs(int inventorySlot)
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            // Equip
            if (selectedKnapsack.GetItemInSlot(inventorySlot).GetType() == typeof(EquipableItem))
            {
                ChoiceActionPair equipActionPair = new ChoiceActionPair(optionEquip, Equip, inventorySlot);
                choiceActionPairs.Add(equipActionPair);
            }
            else
            {
                ChoiceActionPair inspectActionPair = new ChoiceActionPair(optionInspect, CannotEquip, inventorySlot);
                choiceActionPairs.Add(inspectActionPair);
            }

            return choiceActionPairs;
        }

        private void CannotEquip(int inventorySlot)
        {
            handleGlobalInput = false;
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText(messageCannotEquip);
            dialogueBox.SetGlobalCallbacks(standardPlayerInputCaller);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        protected override void Equip(int inventorySlot)
        {
            // TODO:  Finish implementation -- call to equipmentBox to give it the item
            handleGlobalInput = false;
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText("This is where we would go to the equipment screen, if we had implemented it");
            dialogueBox.SetGlobalCallbacks(standardPlayerInputCaller);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        public override void SetupItem(GameObject inventoryItemFieldPrefab, Transform container, int selector)
        {
            GameObject inventoryItemFieldObject = Instantiate(inventoryItemFieldPrefab, container);
            InventoryItemField inventoryItemField = inventoryItemFieldObject.GetComponent<InventoryItemField>();
            inventoryItemField.SetChoiceOrder(selector);
            inventoryItemField.SetText(selectedKnapsack.GetItemInSlot(selector).GetDisplayName());
            if (selectedKnapsack.HasEquipableItemInSlot(selector))
            {
                inventoryItemField.SetValidColor(true);
            }
            else
            {
                inventoryItemField.SetValidColor(false);
            }

            inventoryItemField.SetupButtonAction(this, ChooseItem, selector);

            inventoryItemChoiceOptions.Add(inventoryItemField);
        }
    }
}
