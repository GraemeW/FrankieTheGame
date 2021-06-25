using Frankie.Combat;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Inventory.UI
{
    public class EquipmentBox : DialogueOptionBox, IUIItemHandler
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] TextMeshProUGUI selectedCharacterNameField;
        [Header("Parents")]
        [SerializeField] Transform leftEquipment = null;
        [SerializeField] Transform rightEquipment = null;
        [SerializeField] GameObject equipmentChangeMenu = null;
        [SerializeField] Transform oldStatColumn = null;
        [SerializeField] Transform newStatColumn = null;
        [Header("Prefabs")]
        [SerializeField] GameObject dialogueBoxPrefab = null;
        [SerializeField] GameObject dialogueOptionBoxPrefab = null;
        [SerializeField] GameObject inventoryItemFieldPrefab = null;

        // State
        EquipmentBoxState equipmentBoxState = EquipmentBoxState.inCharacterSelection;
        List<DialogueChoiceOption> playerSelectChoiceOptions = new List<DialogueChoiceOption>();
        List<InventoryItemField> equipableItemChoiceOptions = new List<InventoryItemField>();
        CombatParticipant selectedCharacter = null;
        Equipment selectedEquipment = null;
        EquipableItem selectedItem = null;

        // Cached References
        IStandardPlayerInputCaller standardPlayerInputCaller = null;
        Party party = null;

        public event Action<Enum> uiBoxStateChanged;

        protected override void Start()
        {
            // Do Nothing (skip base implementation)
        }

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party)
        {
            this.standardPlayerInputCaller = standardPlayerInputCaller;
            this.party = party;

            SetGlobalCallbacks(standardPlayerInputCaller);
            int choiceIndex = 0;
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject characterFieldObject = Instantiate(optionPrefab, optionParent);
                DialogueChoiceOption dialogueChoiceOption = characterFieldObject.GetComponent<DialogueChoiceOption>();
                dialogueChoiceOption.SetChoiceOrder(choiceIndex);
                dialogueChoiceOption.SetText(character.GetCombatName());
                characterFieldObject.GetComponent<Button>().onClick.AddListener(delegate { ChooseCharacter(character); });

                if (choiceIndex == 0) { ChooseCharacter(character); SetEquipmentBoxState(EquipmentBoxState.inCharacterSelection); }
                choiceIndex++;

                playerSelectChoiceOptions.Add(dialogueChoiceOption);
            }
            SetUpChoiceOptions();
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
        }

        private void SetEquipmentBoxState(EquipmentBoxState equipmentBoxState)
        {
            this.equipmentBoxState = equipmentBoxState;
            if (uiBoxStateChanged != null)
            {
                uiBoxStateChanged.Invoke(equipmentBoxState);
            }
        }


        private void ChooseCharacter(CombatParticipant character)
        {
            if (character != selectedCharacter)
            {
                OnDialogueBoxModified(DialogueBoxModifiedType.itemSelected, true);

                selectedCharacter = character;
                selectedCharacterNameField.text = selectedCharacter.GetCombatName();
                GenerateEquipment();
            }
            SetEquipmentBoxState(EquipmentBoxState.inEquipmentSelection);
            SetUpChoiceOptions();
        }

        private void GenerateEquipment()
        {
            CleanUpOldEquipment();
            selectedEquipment = selectedCharacter.GetEquipment();

            int i = 0;
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                if (i % 2 == 0)
                {
                    SetupItem(inventoryItemFieldPrefab, leftEquipment, (int)equipLocation);
                }
                else
                {
                    SetupItem(inventoryItemFieldPrefab, rightEquipment, (int)equipLocation);
                }
                i++;
            }
        }

        private void CleanUpOldEquipment()
        {
            equipableItemChoiceOptions.Clear();
            foreach (Transform child in leftEquipment) { Destroy(child.gameObject); }
            foreach (Transform child in rightEquipment) { Destroy(child.gameObject); }
            selectedEquipment = null;
        }

        protected override void SetUpChoiceOptions()
        {
            choiceOptions.Clear();
            if (equipmentBoxState == EquipmentBoxState.inEquipmentSelection)
            {
                choiceOptions.AddRange(equipableItemChoiceOptions.Cast<DialogueChoiceOption>().OrderBy(x => x.choiceOrder).ToList());
            }
            else if (equipmentBoxState == EquipmentBoxState.inCharacterSelection)
            {
                choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList());
            }

            if (choiceOptions.Count > 0) { isChoiceAvailable = true; }
            else { isChoiceAvailable = false; }
            MoveCursor(PlayerInputType.NavigateRight); // Initialize Highlight
        }

        private void ChooseItem(int selector)
        {

        }

        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            if (equipmentBoxState == EquipmentBoxState.inCharacterSelection)
            {
                return base.MoveCursor(playerInputType);
            }
            else if (equipmentBoxState == EquipmentBoxState.inEquipmentSelection)
            {
                // Support for 2-D movement across the inventory items
                if (highlightedChoiceOption == null) { return false; }
                int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);

                bool validInput = MoveCursor2D(playerInputType, ref choiceIndex);

                if (validInput)
                {
                    ClearChoiceSelections();
                    highlightedChoiceOption = choiceOptions[choiceIndex];
                    choiceOptions[choiceIndex].Highlight(true);
                    return true;
                }
            }
            // TODO:  Implement other state selections

            return false;
        }

        #region Interfaces
        public void SetupItem(GameObject inventoryItemFieldPrefab, Transform container, int selector)
        {
            EquipLocation equipLocation = (EquipLocation)selector;
            string itemName = "Empty";
            if (selectedEquipment.HasItemInSlot(equipLocation))
            {
                itemName = selectedEquipment.GetItemInSlot(equipLocation).GetDisplayName();
            }
            string fieldName = string.Format("{0}:  {1}", equipLocation.ToString(), itemName);

            GameObject inventoryItemFieldObject = Instantiate(inventoryItemFieldPrefab, container);
            InventoryItemField inventoryItemField = inventoryItemFieldObject.GetComponent<InventoryItemField>();
            inventoryItemField.SetText(fieldName);
            inventoryItemField.SetupButtonAction(this, ChooseItem, selector);
            equipableItemChoiceOptions.Add(inventoryItemField);
        }
        #endregion
    }
}
