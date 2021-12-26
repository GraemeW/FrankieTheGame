using Frankie.Combat;
using Frankie.Combat.UI;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public class EquipmentInventoryBox : InventoryBox
    {
        [Header("Info/Messages")]
        [SerializeField] string optionEquip = "Equip";
        [SerializeField] string messageCannotEquip = "This item cannot be equipped in this spot.";

        // Cached References
        EquipmentBox equipmentBox = null;
        EquipLocation equipLocation = EquipLocation.None;

        public void Setup(EquipmentBox equipmentBox, EquipLocation equipLocation, CombatParticipant selectedCharacter, List<CharacterSlide> characterSlides = null)
        {
            this.equipmentBox = equipmentBox;
            this.equipLocation = equipLocation;
            Setup(selectedCharacter, characterSlides);
        }

        protected override List<ChoiceActionPair> GetChoiceActionPairs(int inventorySlot)
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            // Equip
            if (selectedKnapsack.GetItemInSlot(inventorySlot).GetType() == typeof(EquipableItem))
            {
                ChoiceActionPair equipActionPair = new ChoiceActionPair(optionEquip, () => Equip(inventorySlot));
                choiceActionPairs.Add(equipActionPair);
            }
            else
            {
                ChoiceActionPair inspectActionPair = new ChoiceActionPair(optionInspect, () => CannotEquip(inventorySlot));
                choiceActionPairs.Add(inspectActionPair);
            }

            return choiceActionPairs;
        }

        private void CannotEquip(int inventorySlot)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(messageCannotEquip);
            PassControl(dialogueBox);
        }

        private void Equip(int inventorySlot)
        {
            EquipableItem equipableItem = selectedKnapsack.GetItemInSlot(inventorySlot) as EquipableItem;
            if (equipableItem == null) { return; }

            equipmentBox.SetSelectedItem(equipableItem);
            Destroy(gameObject);
        }

        public override InventoryItemField SetupItem(InventoryItemField inventoryItemFieldPrefab, Transform container, int selector)
        {
            InventoryItemField inventoryItemField =  base.SetupItem(inventoryItemFieldPrefab, container, selector);
            if (selectedKnapsack.HasEquipableItemInSlot(selector, equipLocation))
            {
                inventoryItemField.SetValidColor(true);
            }
            else
            {
                inventoryItemField.SetValidColor(false);
            }
            return inventoryItemField;
        }

        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                equipmentBox.ResetEquipmentBox(false);
            }
            return StandardHandleGlobalInput(playerInputType);
        }
    }
}
