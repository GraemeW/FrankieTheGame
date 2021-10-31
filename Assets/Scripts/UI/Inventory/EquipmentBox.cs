using Frankie.Combat;
using Frankie.Combat.UI;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public class EquipmentBox : UIBox, IUIItemHandler
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] TextMeshProUGUI selectedCharacterNameField = null;
        [Tooltip("Hook these up to confirm/reject in ConfirmationOptions")] [SerializeField] UIChoiceOption[] equipmentChangeConfirmOptions;
        [Header("Parents")]
        [SerializeField] Transform leftEquipment = null;
        [SerializeField] Transform rightEquipment = null;
        [SerializeField] GameObject equipmentChangeMenu = null;
        [SerializeField] Transform statSheetParent = null;
        [Header("Prefabs")]
        [SerializeField] GameObject dialogueBoxPrefab = null;
        [SerializeField] GameObject dialogueOptionBoxPrefab = null;
        [SerializeField] GameObject inventoryItemFieldPrefab = null;
        [SerializeField] GameObject equipmentInventoryBoxPrefab = null;
        [SerializeField] GameObject statChangeFieldPrefab = null;
        [Header("Info/Messages")]
        [Tooltip("Include {0} for character name")] [SerializeField] string messageNoValidItems = "There's nothing in the knapsack to equip.";
        [SerializeField] string optionText = "What do you want to do?";
        [SerializeField] string optionEquip = "Put on";
        [SerializeField] string optionRemove = "Take off";

        // State
        EquipmentBoxState equipmentBoxState = EquipmentBoxState.inCharacterSelection;
        List<UIChoiceOption> playerSelectChoiceOptions = new List<UIChoiceOption>();
        List<InventoryItemField> equipableItemChoiceOptions = new List<InventoryItemField>();
        CombatParticipant selectedCharacter = null;
        Equipment selectedEquipment = null;
        EquipLocation selectedEquipLocation = EquipLocation.None;
        EquipableItem selectedItem = null;

        // Cached References
        IStandardPlayerInputCaller standardPlayerInputCaller = null;
        Party party = null;
        List<CharacterSlide> characterSlides = null;

        // Events
        public event Action<Enum> uiBoxStateChanged;

        protected override void OnEnable()
        {
            base.OnEnable();
            ListenToSelectedEquipment(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ListenToSelectedEquipment(false);
        }

        #region Setup
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party, List<CharacterSlide> characterSlides = null)
        {
            this.standardPlayerInputCaller = standardPlayerInputCaller;
            this.party = party;
            this.characterSlides = characterSlides;
            SetGlobalInputHandler(standardPlayerInputCaller);

            int choiceIndex = 0;
            CombatParticipant firstCharacter = null;
            foreach (CombatParticipant character in party.GetParty())
            {
                if (firstCharacter != null) { firstCharacter = character; }

                GameObject uiChoiceOptionObject = Instantiate(optionPrefab, optionParent);
                UIChoiceOption uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceOption>();
                uiChoiceOption.SetChoiceOrder(choiceIndex);
                uiChoiceOption.SetText(character.GetCombatName());
                uiChoiceOption.GetButton().onClick.AddListener(delegate { ChooseCharacter(character, true); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(character); });

                playerSelectChoiceOptions.Add(uiChoiceOption);
                choiceIndex++;
            }
            SetEquipmentBoxState(EquipmentBoxState.inCharacterSelection);
            ShowCursorOnAnyInteraction(PlayerInputType.NavigateRight);
        }

        private void SetSelectedEquipment(Equipment equipment)
        {
            if (selectedEquipment != null)
            {
                ListenToSelectedEquipment(false);
            }

            selectedEquipment = equipment;
            ListenToSelectedEquipment(true);
        }

        private void ListenToSelectedEquipment(bool enable)
        {
            if (selectedEquipment == null) { return; }

            if (enable)
            {
                selectedEquipment.equipmentUpdated += HandleEquipmentUpdated;
            }
            else
            {
                selectedEquipment.equipmentUpdated -= HandleEquipmentUpdated;
            }
        }

        public void SetSelectedItem(EquipableItem equipableItem)
        {
            if (equipableItem == null) { return; }

            selectedItem = equipableItem;
            GenerateStatConfirmationMenu();
            SetEquipmentBoxState(EquipmentBoxState.inStatConfirmation);
            MoveCursor(PlayerInputType.NavigateRight);
        }

        private void HandleEquipmentUpdated(EquipableItem equipableItem)
        {
            ResetEquipmentBox(false);
        }

        protected override void SetUpChoiceOptions()
        {
            choiceOptions.Clear();
            if (equipmentBoxState == EquipmentBoxState.inEquipmentSelection)
            {
                choiceOptions.AddRange(equipableItemChoiceOptions.Cast<UIChoiceOption>().OrderBy(x => x.choiceOrder).ToList());
            }
            else if (equipmentBoxState == EquipmentBoxState.inCharacterSelection)
            {
                choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList());
            }
            else if (equipmentBoxState == EquipmentBoxState.inStatConfirmation)
            {
                choiceOptions.AddRange(equipmentChangeConfirmOptions);
            }

            SetChoiceAvailable(choiceOptions.Count > 0);
        }

        public void ResetEquipmentBox(bool clearSelectedCharacter)
        {
            if (selectedCharacter != null && !clearSelectedCharacter)
            {
                ChooseCharacter(selectedCharacter, true); // Resets chosen item & slot -> pulls to equipment selection
            }
            else
            {
                ClearChoiceSelections();
                ChooseCharacter(null);
            }
        }

        protected override void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (UIChoiceOption dialogueChoiceOption in playerSelectChoiceOptions)
            {
                dialogueChoiceOption.Highlight(false);
            }
            foreach (InventoryItemField inventoryItemField in equipableItemChoiceOptions)
            {
                inventoryItemField.Highlight(false);
            }
            foreach (UIChoiceOption dialogueChoiceOption in equipmentChangeConfirmOptions)
            {
                dialogueChoiceOption.Highlight(false);
            }
        }

        private void SetEquipmentBoxState(EquipmentBoxState equipmentBoxState)
        {
            this.equipmentBoxState = equipmentBoxState;
            if (equipmentBoxState == EquipmentBoxState.inStatConfirmation)
            {
                equipmentChangeMenu.SetActive(true);
            }
            else
            {
                equipmentChangeMenu.SetActive(false);
            }
            SetUpChoiceOptions();

            if (uiBoxStateChanged != null)
            {
                uiBoxStateChanged.Invoke(equipmentBoxState);
            }
        }
        #endregion

        #region Interaction
        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            if (equipmentBoxState == EquipmentBoxState.inCharacterSelection || equipmentBoxState == EquipmentBoxState.inStatConfirmation)
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

            return false;
        }

        private void ChooseCharacter(CombatParticipant character, bool forceChoose = false, bool initializeCursor = true)
        {
            selectedEquipLocation = EquipLocation.None;
            selectedItem = null;
            if (character == null)
            {
                SetSelectedEquipment(null);
                SetEquipmentBoxState(EquipmentBoxState.inCharacterSelection);
                return;
            }

            if (character != selectedCharacter || forceChoose)
            {
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);

                selectedCharacter = character;
                selectedCharacterNameField.text = selectedCharacter.GetCombatName();
                RefreshEquipment();
            }
            SetEquipmentBoxState(EquipmentBoxState.inEquipmentSelection);

            if (initializeCursor) { MoveCursor(PlayerInputType.NavigateRight); }
        }

        private void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false, false);
            SetEquipmentBoxState(EquipmentBoxState.inCharacterSelection);
        }
        #endregion

        #region EquipmentBehaviour

        private void RefreshEquipment()
        {
            CleanUpOldEquipment();
            SetSelectedEquipment(selectedCharacter.GetEquipment());

            int i = 0;
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                if (equipLocation == EquipLocation.None) { continue; }

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
            SetSelectedEquipment(null);
        }

        private void GenerateStatConfirmationMenu()
        {
            CleanOldStatSheet();

            BaseStats baseStats = selectedCharacter.GetBaseStats();
            Dictionary<Stat, float> activeStatSheetWithModifiers = baseStats.GetActiveStatSheet();
            Dictionary<Stat, float> statDeltas = selectedEquipment.CompareEquipableItem(selectedEquipLocation, selectedItem);

            Stat[] nonModifyingStats = BaseStats.GetNonModifyingStats();
            foreach (KeyValuePair<Stat, float> statEntry in activeStatSheetWithModifiers)
            {
                Stat stat = statEntry.Key;
                if (nonModifyingStats.Contains(stat)) { continue; }

                float oldValue = baseStats.GetStat(stat); // Pull from actual stat, since active stat sheet does not contain modifiers
                float newValue = oldValue;
                if (statDeltas.ContainsKey(stat))
                {
                    newValue += statDeltas[stat];
                }

                GameObject statChangeFieldObject = Instantiate(statChangeFieldPrefab, statSheetParent);
                StatChangeField statChangeField = statChangeFieldObject.GetComponent<StatChangeField>();
                statChangeField.Setup(stat, oldValue, newValue);
            }
        }

        private void CleanOldStatSheet()
        {
            foreach (Transform child in statSheetParent)
            {
                Destroy(child.gameObject);
            }
        }
        #endregion

        #region UserBehaviour
        private void ChooseEquipLocation(int selector)
        {
            EquipLocation equipLocation = (EquipLocation)selector;
            if (equipLocation == EquipLocation.None || selectedEquipment == null) { return; }

            if (selectedEquipment.HasItemInSlot(equipLocation))
            {
                List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
                ChoiceActionPair equipActionPair = new ChoiceActionPair(optionEquip, () => ExecuteChooseEquipLocation(selector));
                choiceActionPairs.Add(equipActionPair);
                ChoiceActionPair removeActionPair = new ChoiceActionPair(optionRemove, () => ExecuteRemoveEquipment(selector));
                choiceActionPairs.Add(removeActionPair);

                handleGlobalInput = false;
                GameObject dialogueOptionBoxObject = Instantiate(dialogueOptionBoxPrefab, transform.parent);
                DialogueOptionBox equipmentOptionMenu = dialogueOptionBoxObject.GetComponent<DialogueOptionBox>();
                equipmentOptionMenu.Setup(optionText);
                equipmentOptionMenu.OverrideChoiceOptions(choiceActionPairs);
                equipmentOptionMenu.SetGlobalInputHandler(standardPlayerInputCaller);
                equipmentOptionMenu.SetDisableCallback(this, () => EnableInput(true));
                SetEquipmentBoxState(EquipmentBoxState.inEquipmentOptionMenu);
            }
            else
            {
                ExecuteChooseEquipLocation(selector);
            }
        }

        private void ExecuteChooseEquipLocation(int selector)
        {
            EquipLocation equipLocation = (EquipLocation)selector;

            if (selectedCharacter.GetKnapsack().HasAnyEquipableItem(equipLocation))
            {
                selectedEquipLocation = equipLocation;
                SpawnInventoryBox();
            }
            else
            {
                selectedEquipLocation = EquipLocation.None;
                SpawnNoValidItemsMessage();
            }
        }

        private void ExecuteRemoveEquipment(int selector)
        {
            if (selectedEquipment == null) { return; }

            EquipLocation equipLocation = (EquipLocation)selector;
            selectedEquipment.RemoveEquipment(equipLocation, true);
        }

        private void SpawnNoValidItemsMessage()
        {
            handleGlobalInput = false;
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText(messageNoValidItems);
            dialogueBox.SetGlobalInputHandler(standardPlayerInputCaller);
            dialogueBox.SetDisableCallback(this, () => EnableInput(true));
        }

        private void SpawnInventoryBox()
        {
            if (selectedEquipLocation == EquipLocation.None) { return; }

            handleGlobalInput = false;
            GameObject inventoryBoxObject = Instantiate(equipmentInventoryBoxPrefab, transform.parent.transform);
            EquipmentInventoryBox inventoryBox = inventoryBoxObject.GetComponent<EquipmentInventoryBox>();
            inventoryBox.Setup(standardPlayerInputCaller, this, selectedEquipLocation, selectedCharacter, characterSlides);
            inventoryBox.SetDisableCallback(this, () => EnableInput(true));

            canvasGroup.alpha = 0.0f;
            inventoryBox.SetDisableCallback(this, () => SetVisible(true));
        }

        public void ConfirmEquipmentChange(bool confirm) // Called via equipmentChangeConfirmOptions buttons, hooked up in Unity
        {
            if (confirm)
            {
                selectedEquipment.AddEquipment(selectedItem, true);
            }
            else
            {
                ChooseCharacter(selectedCharacter, true); // Resets chosen item & slot -> pulls to equipment selection
            }
        }
        #endregion

        #region Interfaces
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (equipmentBoxState == EquipmentBoxState.inStatConfirmation)
                {
                    ResetEquipmentBox(false);
                    return true;
                }
                else if (equipmentBoxState == EquipmentBoxState.inEquipmentSelection)
                {
                    ResetEquipmentBox(true);
                    return true;
                }
                // inKnapsack handled by the EquipmentInventoryBox
            }
            
            return base.HandleGlobalInput(playerInputType);
        }

        public InventoryItemField SetupItem(GameObject inventoryItemFieldPrefab, Transform container, int selector)
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
            inventoryItemField.SetupButtonAction(this, ChooseEquipLocation, selector);
            equipableItemChoiceOptions.Add(inventoryItemField);

            return inventoryItemField;
        }
        #endregion
    }
}
