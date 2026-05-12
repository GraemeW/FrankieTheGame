using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Combat;
using Frankie.Combat.UI;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Utils;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    public class EquipmentInventoryBox : InventoryBox
    {
        [Header("Equipment-Inventory Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionEquip;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageCannotEquip;

        // Cached References
        private EquipmentBox equipmentBox;
        private EquipLocation equipLocation = EquipLocation.None;
        private Equipment equipment;

        #region LocalizationMethods
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            // Note:  Standard configuration re-uses localization keys from InventoryBox 
            // Here we only return unique to this child script to prevent deletion of InventoryBox keys
            // Overridden standard Inventory entries would need to be manually deleted
            return new List<TableEntryReference>
            {
                localizedOptionEquip.TableEntryReference,
                localizedMessageCannotEquip.TableEntryReference,
            };
        }
        #endregion
        
        #region PublicMethods
        public void Setup(EquipmentBox setEquipmentBox, EquipLocation setEquipLocation, CombatParticipant setSelectedCharacter, List<CharacterSlide> setCharacterSlides)
        {
            equipmentBox = setEquipmentBox;
            equipLocation = setEquipLocation;
            equipment = setSelectedCharacter.GetComponent<Equipment>();
            Setup(setSelectedCharacter, setCharacterSlides);
        }
        
        public override InventoryItemField SetupItem(InventoryItemField setInventoryItemFieldPrefab, Transform container, int selector)
        {
            InventoryItemField inventoryItemField =  base.SetupItem(setInventoryItemFieldPrefab, container, selector);
            inventoryItemField.SetValidColor(selectedKnapsack.HasEquipableItemInSlot(selector, equipLocation));
            return inventoryItemField;
        }
        #endregion

        #region ProtectedPrivateMethods
        protected override List<ChoiceActionPair> GetChoiceActionPairs(int inventorySlot)
        {
            var choiceActionPairs = new List<ChoiceActionPair>();
            var equipableItem = selectedKnapsack.GetItemInSlot(inventorySlot) as EquipableItem;
            
            if (equipableItem != null
                && equipment != null && equipableItem.CanUseItem(equipment)
                && equipLocation != EquipLocation.None && equipableItem.GetEquipLocation() == equipLocation)
            {
                var equipActionPair = new ChoiceActionPair(localizedOptionEquip.GetSafeLocalizedString(), () => Equip(inventorySlot));
                choiceActionPairs.Add(equipActionPair);
            }
            else
            {
                var inspectActionPair = new ChoiceActionPair(localizedOptionInspect.GetSafeLocalizedString(), CannotEquip);
                choiceActionPairs.Add(inspectActionPair);
            }
            return choiceActionPairs;
        }
        
        private void CannotEquip()
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(localizedMessageCannotEquip.GetSafeLocalizedString());
            PassControl(dialogueBox);
        }

        private void Equip(int inventorySlot)
        {
            var equipableItem = selectedKnapsack.GetItemInSlot(inventorySlot) as EquipableItem;
            if (equipableItem == null) { return; }

            equipmentBox.SetSelectedItem(equipableItem);
            Destroy(gameObject);
        }
        #endregion

        #region InputInterface
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
        #endregion
    }
}
