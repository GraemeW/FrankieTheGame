using Frankie.Combat;
using Frankie.Combat.UI;
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
        [SerializeField] TextMeshProUGUI selectedCharacterNameField = null;
        [SerializeField] CanvasGroup canvasGroup = null;
        [Tooltip("Hook these up to confirm/reject in ConfirmationOptions")][SerializeField] List<DialogueChoiceOption> equipmentChangeConfirmOptions = new List<DialogueChoiceOption>();
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
        [SerializeField] string optionEquip = "Put on";
        [SerializeField] string optionRemove = "Take off";

        // State
        EquipmentBoxState equipmentBoxState = EquipmentBoxState.inCharacterSelection;
        List<DialogueChoiceOption> playerSelectChoiceOptions = new List<DialogueChoiceOption>();
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

        // Static
        protected static string DIALOGUE_CALLBACK_RESTORE_ALPHA = "RESTORE_ALPHA";

        protected override void Start()
        {
            // Do Nothing (skip base implementation)
        }

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

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party, List<CharacterSlide> characterSlides = null)
        {
            this.standardPlayerInputCaller = standardPlayerInputCaller;
            this.party = party;
            this.characterSlides = characterSlides;
            SetGlobalCallbacks(standardPlayerInputCaller);

            int choiceIndex = 0;
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject characterFieldObject = Instantiate(optionPrefab, optionParent);
                DialogueChoiceOption dialogueChoiceOption = characterFieldObject.GetComponent<DialogueChoiceOption>();
                dialogueChoiceOption.SetChoiceOrder(choiceIndex);
                dialogueChoiceOption.SetText(character.GetCombatName());
                characterFieldObject.GetComponent<Button>().onClick.AddListener(delegate { ChooseCharacter(character); });

                if (choiceIndex == 0) { ChooseCharacter(character); } // Initialize box with first character stats
                choiceIndex++;

                playerSelectChoiceOptions.Add(dialogueChoiceOption);
            }
            ResetEquipmentBox(true);
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
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
            SetUpChoiceOptions();
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

            if (uiBoxStateChanged != null)
            {
                uiBoxStateChanged.Invoke(equipmentBoxState);
            }
        }


        private void ChooseCharacter(CombatParticipant character, bool forceChoose = false)
        {
            selectedEquipLocation = EquipLocation.None;
            selectedItem = null;

            if (character != selectedCharacter || forceChoose)
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
            else if (equipmentBoxState == EquipmentBoxState.inStatConfirmation)
            {
                choiceOptions.AddRange(equipmentChangeConfirmOptions);
            }

            if (choiceOptions.Count > 0) { isChoiceAvailable = true; }
            else { isChoiceAvailable = false; }
            MoveCursor(PlayerInputType.NavigateRight); // Initialize Highlight
        }

        private void ChooseEquipLocation(int selector)
        {
            EquipLocation equipLocation = (EquipLocation)selector;
            if (equipLocation == EquipLocation.None || selectedEquipment == null) { return; }

            if (selectedEquipment.HasItemInSlot(equipLocation))
            {
                List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
                ChoiceActionPair equipActionPair = new ChoiceActionPair(optionEquip, ExecuteChooseEquipLocation, selector);
                choiceActionPairs.Add(equipActionPair);
                ChoiceActionPair removeActionPair = new ChoiceActionPair(optionRemove, ExecuteRemoveEquipment, selector);
                choiceActionPairs.Add(removeActionPair);

                handleGlobalInput = false;
                GameObject dialogueOptionBoxObject = Instantiate(dialogueOptionBoxPrefab, transform.parent);
                DialogueOptionBox equipmentOptionMenu = dialogueOptionBoxObject.GetComponent<DialogueOptionBox>();
                equipmentOptionMenu.SetupSimpleChoices(choiceActionPairs);
                equipmentOptionMenu.SetGlobalCallbacks(standardPlayerInputCaller);
                equipmentOptionMenu.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
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

            if (HasAnyEquipableItems(equipLocation))
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
            selectedEquipment.AddSwapOrRemoveItem(equipLocation, null);
        }

        private bool HasAnyEquipableItems(EquipLocation equipLocation)
        {
            foreach (CombatParticipant character in party.GetParty())
            {
                Knapsack knapsack = character.GetKnapsack();
                if (knapsack.HasAnyEquipableItem(equipLocation))
                {
                    return true;
                }
            }
            return false;
        }

        private void SpawnNoValidItemsMessage()
        {
            handleGlobalInput = false;
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText(messageNoValidItems);
            dialogueBox.SetGlobalCallbacks(standardPlayerInputCaller);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        private void SpawnInventoryBox()
        {
            if (selectedEquipLocation == EquipLocation.None) { return; }

            handleGlobalInput = false;
            GameObject inventoryBoxObject = Instantiate(equipmentInventoryBoxPrefab, transform.parent.transform);
            EquipmentInventoryBox inventoryBox = inventoryBoxObject.GetComponent<EquipmentInventoryBox>();
            inventoryBox.Setup(standardPlayerInputCaller, party, this, selectedEquipLocation, characterSlides);
            inventoryBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);

            canvasGroup.alpha = 0.0f;
            inventoryBox.SetDisableCallback(this, DIALOGUE_CALLBACK_RESTORE_ALPHA);
        }

        public void ConfirmEquipmentChange(bool confirm) // Called via equipmentChangeConfirmOptions buttons, hooked up in Unity
        {
            if (confirm)
            {
                selectedEquipment.AddSwapOrRemoveItem(selectedEquipLocation, selectedItem);
            }
            else
            {
                ChooseCharacter(selectedCharacter, true); // Resets chosen item & slot -> pulls to equipment selection
            }
        }

        private void HandleEquipmentUpdated()
        {
            ResetEquipmentBox(false);
        }

        public void ResetEquipmentBox(bool clearSelectedCharacter)
        {
            if (selectedCharacter != null && !clearSelectedCharacter)
            {
                ChooseCharacter(selectedCharacter, true); // Resets chosen item & slot -> pulls to equipment selection
            }
            else
            {
                selectedItem = null;
                selectedEquipLocation = EquipLocation.None;
                SetSelectedEquipment(null);
                ChooseCharacter(party.GetPartyLeader());
                SetEquipmentBoxState(EquipmentBoxState.inCharacterSelection);
                selectedCharacter = null;
            }
            SetUpChoiceOptions();
        }

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

        public override void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return; }

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (equipmentBoxState == EquipmentBoxState.inStatConfirmation)
                {
                    ClearChoiceSelections();
                    ResetEquipmentBox(false);
                }
                else if (equipmentBoxState == EquipmentBoxState.inEquipmentSelection)
                {
                    ClearChoiceSelections();
                    ResetEquipmentBox(true);
                }
                else if (equipmentBoxState == EquipmentBoxState.inCharacterSelection)
                {
                    Destroy(gameObject);
                }
                // inKnapsack handled by the EquipmentInventoryBox
            }
            base.HandleGlobalInput(playerInputType);
        }

        public override void HandleDialogueCallback(DialogueBox dialogueBox, string callbackMessage)
        {
            base.HandleDialogueCallback(dialogueBox, callbackMessage);
            if (callbackMessage == DIALOGUE_CALLBACK_RESTORE_ALPHA)
            {
                canvasGroup.alpha = 1.0f;
            }
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
            inventoryItemField.SetupButtonAction(this, ChooseEquipLocation, selector);
            equipableItemChoiceOptions.Add(inventoryItemField);
        }
        #endregion
    }
}
