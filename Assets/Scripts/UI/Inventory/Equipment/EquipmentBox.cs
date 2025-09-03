using Frankie.Combat;
using Frankie.Control;
using Frankie.Stats;
using Frankie.Utils;
using Frankie.Utils.UI;
using Frankie.Speech.UI;
using Frankie.Combat.UI;
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
        [Tooltip("Hook these up to confirm/reject in ConfirmationOptions")] [SerializeField] UIChoiceButton[] equipmentChangeConfirmOptions;
        [Header("Parents")]
        [SerializeField] Transform leftEquipment = null;
        [SerializeField] Transform rightEquipment = null;
        [SerializeField] GameObject equipmentChangeMenu = null;
        [SerializeField] Transform statSheetParent = null;
        [Header("Prefabs")]
        [SerializeField] DialogueBox dialogueBoxPrefab = null;
        [SerializeField] DialogueOptionBox dialogueOptionBoxPrefab = null;
        [SerializeField] InventoryItemField inventoryItemFieldPrefab = null;
        [SerializeField] EquipmentInventoryBox equipmentInventoryBoxPrefab = null;
        [SerializeField] StatChangeField statChangeFieldPrefab = null;
        [Header("Info/Messages")]
        [SerializeField] string messageNoValidItems = "There's nothing in the knapsack to equip.";
        [SerializeField] string messageUnequip = "Guess we're goin' nude";
        [SerializeField] string optionText = "What do you want to do?";
        [SerializeField] string optionEquip = "Put on";
        [SerializeField] string optionRemove = "Take off";

        // State
        EquipmentBoxState equipmentBoxState = EquipmentBoxState.inCharacterSelection;
        List<UIChoiceButton> playerSelectChoiceOptions = new List<UIChoiceButton>();
        List<InventoryItemField> equipableItemChoiceOptions = new List<InventoryItemField>();
        CombatParticipant selectedCharacter = null;
        Equipment selectedEquipment = null;
        EquipLocation selectedEquipLocation = EquipLocation.None;
        EquipableItem selectedItem = null;

        // Cached References
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
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit partyCombatConduit, List<CharacterSlide> characterSlides = null)
        {
            controller = standardPlayerInputCaller;
            this.characterSlides = characterSlides;

            int choiceIndex = 0;
            CombatParticipant firstCharacter = null;
            foreach (CombatParticipant character in partyCombatConduit.GetPartyCombatParticipants())
            {
                if (firstCharacter != null) { firstCharacter = character; }

                GameObject uiChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
                UIChoiceButton uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceButton>();
                uiChoiceOption.SetChoiceOrder(choiceIndex);
                uiChoiceOption.SetText(character.GetCombatName());
                uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(character, true); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(character); });

                playerSelectChoiceOptions.Add(uiChoiceOption);
                choiceIndex++;
            }
            SetEquipmentBoxState(EquipmentBoxState.inCharacterSelection);
            ShowCursorOnAnyInteraction(PlayerInputType.NavigateRight);
        }

        private void SetSelectedEquipment(Equipment equipment)
        {
            ListenToSelectedEquipment(false); // Remove subscription to current equipment
            selectedEquipment = equipment;
            ListenToSelectedEquipment(true); // Attach subcscription to new equipment
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
                choiceOptions.AddRange(equipableItemChoiceOptions.Cast<UIChoice>().OrderBy(x => x.choiceOrder).ToList());
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
            foreach (UIChoiceButton dialogueChoiceOption in playerSelectChoiceOptions)
            {
                dialogueChoiceOption.Highlight(false);
            }
            foreach (InventoryItemField inventoryItemField in equipableItemChoiceOptions)
            {
                inventoryItemField.Highlight(false);
            }
            foreach (UIChoiceButton dialogueChoiceOption in equipmentChangeConfirmOptions)
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

            uiBoxStateChanged?.Invoke(equipmentBoxState);
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
                MoveCursor2D(playerInputType);
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

            if (!selectedCharacter.TryGetComponent(out Equipment selectedCharacterEquipment)) { return; }
            SetSelectedEquipment(selectedCharacterEquipment);

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

            if (!selectedCharacter.TryGetComponent(out BaseStats baseStats)) { return; }

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

                StatChangeField statChangeField = Instantiate(statChangeFieldPrefab, statSheetParent);
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

                DialogueOptionBox equipmentOptionMenu = Instantiate(dialogueOptionBoxPrefab, transform.parent);
                equipmentOptionMenu.Setup(optionText);
                equipmentOptionMenu.OverrideChoiceOptions(choiceActionPairs);

                PassControl(this, new Action[] { () => ResetEquipmentBox(false), () => EnableInput(true) }, equipmentOptionMenu, controller);
                equipmentOptionMenu.ClearDisableCallbacksOnChoose(true);
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

            if (!selectedCharacter.TryGetComponent(out Knapsack knapsack)) { return; }

            if (knapsack.HasAnyEquipableItem(equipLocation))
            {
                selectedEquipLocation = equipLocation;
                SpawnInventoryBox();
            }
            else
            {
                selectedEquipLocation = EquipLocation.None;
                SpawnMessage(messageNoValidItems);
            }
        }

        private void ExecuteRemoveEquipment(int selector)
        {
            if (selectedEquipment == null) { return; }

            EquipLocation equipLocation = (EquipLocation)selector;
            selectedEquipment.RemoveEquipment(equipLocation, true);

            SpawnMessage(messageUnequip);
        }

        private void SpawnMessage(string message)
        {
            handleGlobalInput = false;
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(message);
            PassControl(dialogueBox);
        }

        private void SpawnInventoryBox()
        {
            if (selectedEquipLocation == EquipLocation.None) { return; }

            handleGlobalInput = false;
            EquipmentInventoryBox inventoryBox = Instantiate(equipmentInventoryBoxPrefab, transform.parent.transform);
            inventoryBox.Setup(this, selectedEquipLocation, selectedCharacter, characterSlides);
            canvasGroup.alpha = 0.0f;
            PassControl(this, new Action[] { () => EnableInput(true), () => SetVisible(true) }, inventoryBox, controller);
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

        public InventoryItemField SetupItem(InventoryItemField inventoryItemFieldPrefab, Transform container, int selector)
        {
            EquipLocation equipLocation = (EquipLocation)selector;
            string itemName = "Empty";
            if (selectedEquipment.HasItemInSlot(equipLocation))
            {
                itemName = selectedEquipment.GetItemInSlot(equipLocation).GetDisplayName();
            }
            string fieldName = string.Format("{0}:  {1}", equipLocation.ToString(), itemName);

            InventoryItemField inventoryItemField = Instantiate(inventoryItemFieldPrefab, container);
            inventoryItemField.SetText(fieldName);
            inventoryItemField.SetupButtonAction(this, ChooseEquipLocation, selector);
            equipableItemChoiceOptions.Add(inventoryItemField);

            return inventoryItemField;
        }
        #endregion
    }
}
