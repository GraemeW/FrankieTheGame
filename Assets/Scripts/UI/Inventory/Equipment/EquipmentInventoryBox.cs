using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;
using Frankie.Combat.UI;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Utils;

namespace Frankie.Inventory.UI
{
    public class EquipmentInventoryBox : InventoryBox
    {
        [Header("Info/Messages")]
        [SerializeField] private string optionEquip = "Equip";
        [SerializeField] private string messageCannotEquip = "This item cannot be equipped in this spot.";

        // Cached References
        private EquipmentBox equipmentBox;
        private EquipLocation equipLocation = EquipLocation.None;
        private Equipment equipment;

        public void Setup(EquipmentBox setEquipmentBox, EquipLocation setEquipLocation, CombatParticipant setSelectedCharacter, List<CharacterSlide> setCharacterSlides)
        {
            equipmentBox = setEquipmentBox;
            equipLocation = setEquipLocation;
            equipment = setSelectedCharacter.GetComponent<Equipment>();
            Setup(setSelectedCharacter, setCharacterSlides);
        }

        protected override List<ChoiceActionPair> GetChoiceActionPairs(int inventorySlot)
        {
            var choiceActionPairs = new List<ChoiceActionPair>();
            var equipableItem = selectedKnapsack.GetItemInSlot(inventorySlot) as EquipableItem;
            
            if (equipableItem != null
                && equipment != null && equipableItem.CanUseItem(equipment)
                && equipLocation != EquipLocation.None && equipableItem.GetEquipLocation() == equipLocation)
            {
                var equipActionPair = new ChoiceActionPair(optionEquip, () => Equip(inventorySlot));
                choiceActionPairs.Add(equipActionPair);
            }
            else
            {
                var inspectActionPair = new ChoiceActionPair(optionInspect, CannotEquip);
                choiceActionPairs.Add(inspectActionPair);
            }
            return choiceActionPairs;
        }

        private void CannotEquip()
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(messageCannotEquip);
            PassControl(dialogueBox);
        }

        private void Equip(int inventorySlot)
        {
            var equipableItem = selectedKnapsack.GetItemInSlot(inventorySlot) as EquipableItem;
            if (equipableItem == null) { return; }

            equipmentBox.SetSelectedItem(equipableItem);
            Destroy(gameObject);
        }

        public override InventoryItemField SetupItem(InventoryItemField setInventoryItemFieldPrefab, Transform container, int selector)
        {
            InventoryItemField inventoryItemField =  base.SetupItem(setInventoryItemFieldPrefab, container, selector);
            inventoryItemField.SetValidColor(selectedKnapsack.HasEquipableItemInSlot(selector, equipLocation));
            return inventoryItemField;
        }

        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            // Spoof:  Cannot accept input, so treat as if global input already handled
            if (!handleGlobalInput) { return true; }

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                equipmentBox.ResetEquipmentBox(false);
            }
            return StandardHandleGlobalInput(playerInputType);
        }
    }
}
