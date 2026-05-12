using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Combat;
using Frankie.Control;
using Frankie.Stats;
using Frankie.Utils;
using Frankie.Combat.UI;
using Frankie.Speech.UI;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    public class EquipmentBox : UIBox, IUIItemHandler, ILocalizable
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] private TextMeshProUGUI selectedCharacterNameField;
        [Tooltip("Hook these up to confirm/reject in ConfirmationOptions")] [SerializeField] private UIChoiceButton[] equipmentChangeConfirmOptions;
        [Header("Parents")]
        [SerializeField] private Transform leftEquipment;
        [SerializeField] private Transform rightEquipment;
        [SerializeField] private GameObject equipmentChangeMenu;
        [SerializeField] private Transform statSheetParent;
        [Header("Hookups")] 
        [SerializeField] private UIChoiceButton confirmEquipmentChange;
        [SerializeField] private UIChoiceButton rejectEquipmentChange;
        [Header("Prefabs")]
        [SerializeField] private DialogueBox dialogueBoxPrefab;
        [SerializeField] private DialogueOptionBox dialogueOptionBoxPrefab;
        [SerializeField] private InventoryItemField inventoryItemFieldPrefab;
        [SerializeField] private EquipmentInventoryBox equipmentInventoryBoxPrefab;
        [SerializeField] private StatChangeField statChangeFieldPrefab;
        [Header("Info/Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedEmptyEquipmentItem;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageNoValidItems;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageUnequip;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionEquip;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionRemove;

        // State
        private EquipmentBoxState equipmentBoxState = EquipmentBoxState.InCharacterSelection;
        private readonly List<UIChoiceButton> playerSelectChoiceOptions = new();
        private readonly List<InventoryItemField> equipableItemChoiceOptions = new();
        private CombatParticipant selectedCharacter;
        private Equipment selectedEquipment;
        private EquipLocation selectedEquipLocation = EquipLocation.None;
        private EquipableItem selectedItem;

        // Cached References
        private readonly List<CharacterSlide> characterSlides = new();

        // Events
        public event Action<Enum> uiBoxStateChanged;

        #region UnityMethods

        private void Awake()
        {
            if (confirmEquipmentChange != null) { confirmEquipmentChange.AddOnClickListener(() => ConfirmEquipmentChange(true));}
            if (rejectEquipmentChange != null) { rejectEquipmentChange.AddOnClickListener(() => ConfirmEquipmentChange(false)); }
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
        #endregion
        
        #region LocalizationMethods

        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedEmptyEquipmentItem.TableEntryReference,
                localizedMessageNoValidItems.TableEntryReference,
                localizedMessageUnequip.TableEntryReference,
                localizedOptionText.TableEntryReference,
                localizedOptionEquip.TableEntryReference,
                localizedOptionRemove.TableEntryReference,
            };
        }
        #endregion

        #region Setup
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit partyCombatConduit, List<CharacterSlide> setCharacterSlides)
        {
            controller = standardPlayerInputCaller;
            characterSlides.Clear();
            foreach (CharacterSlide characterSlide in setCharacterSlides) { characterSlides.Add(characterSlide); }

            int choiceIndex = 0;
            foreach (CombatParticipant character in partyCombatConduit.GetPartyCombatParticipants())
            {
                GameObject uiChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
                var uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceButton>();
                uiChoiceOption.SetChoiceOrder(choiceIndex);
                uiChoiceOption.SetText(character.GetCombatName());
                uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(character, true); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(character); });

                playerSelectChoiceOptions.Add(uiChoiceOption);
                choiceIndex++;
            }
            SetEquipmentBoxState(EquipmentBoxState.InCharacterSelection);
            ShowCursorOnAnyInteraction(PlayerInputType.NavigateRight);
        }

        private void SetSelectedEquipment(Equipment equipment)
        {
            ListenToSelectedEquipment(false); // Remove subscription to current equipment
            selectedEquipment = equipment;
            ListenToSelectedEquipment(true); // Attach subscription to new equipment
        }

        private void ListenToSelectedEquipment(bool enable)
        {
            if (selectedEquipment == null) { return; }
            
            if (enable) { selectedEquipment.equipmentUpdated += HandleEquipmentUpdated; }
            else { selectedEquipment.equipmentUpdated -= HandleEquipmentUpdated; }
        }

        public void SetSelectedItem(EquipableItem equipableItem)
        {
            if (equipableItem == null) { return; }

            selectedItem = equipableItem;
            GenerateStatConfirmationMenu();
            SetEquipmentBoxState(EquipmentBoxState.InStatConfirmation);
            MoveCursor(PlayerInputType.NavigateRight);
        }

        private void HandleEquipmentUpdated(EquipableItem equipableItem)
        {
            ResetEquipmentBox(false);
        }

        protected override void SetUpChoiceOptions()
        {
            choiceOptions.Clear();
            switch (equipmentBoxState)
            {
                case EquipmentBoxState.InEquipmentSelection:
                    choiceOptions.AddRange(equipableItemChoiceOptions.Cast<UIChoice>().OrderBy(x => x.choiceOrder).ToList());
                    break;
                case EquipmentBoxState.InCharacterSelection:
                    choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList());
                    break;
                case EquipmentBoxState.InStatConfirmation:
                    choiceOptions.AddRange(equipmentChangeConfirmOptions);
                    break;
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

        private void SetEquipmentBoxState(EquipmentBoxState setEquipmentBoxState)
        {
            equipmentBoxState = setEquipmentBoxState;
            equipmentChangeMenu.SetActive(setEquipmentBoxState == EquipmentBoxState.InStatConfirmation);
            SetUpChoiceOptions();

            uiBoxStateChanged?.Invoke(setEquipmentBoxState);
        }
        #endregion

        #region Interaction
        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            switch (equipmentBoxState)
            {
                case EquipmentBoxState.InCharacterSelection:
                case EquipmentBoxState.InStatConfirmation:
                    return base.MoveCursor(playerInputType);
                case EquipmentBoxState.InEquipmentSelection:
                    MoveCursor2D(playerInputType);
                    break;
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
                SetEquipmentBoxState(EquipmentBoxState.InCharacterSelection);
                return;
            }

            if (character != selectedCharacter || forceChoose)
            {
                OnUIBoxModified(UIBoxModifiedType.ItemSelected, true);

                selectedCharacter = character;
                selectedCharacterNameField.text = selectedCharacter.GetCombatName();
                RefreshEquipment();
            }
            SetEquipmentBoxState(EquipmentBoxState.InEquipmentSelection);

            if (initializeCursor) { MoveCursor(PlayerInputType.NavigateRight); }
        }

        private void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false, false);
            SetEquipmentBoxState(EquipmentBoxState.InCharacterSelection);
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
                SetupItem(inventoryItemFieldPrefab, i % 2 == 0 ? leftEquipment : rightEquipment, (int)equipLocation);
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
            
            foreach (KeyValuePair<Stat, float> statEntry in activeStatSheetWithModifiers)
            {
                Stat stat = statEntry.Key;
                if (BaseStats.GetNonModifyingStats().Contains(stat)) { continue; }

                float oldValue = baseStats.GetStat(stat); // Pull from actual stat, since active stat sheet does not contain modifiers
                float newValue = oldValue;
                if (statDeltas.TryGetValue(stat, out var delta))
                {
                    newValue += delta;
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
                var choiceActionPairs = new List<ChoiceActionPair>();
                var equipActionPair = new ChoiceActionPair(localizedOptionEquip.GetSafeLocalizedString(), () => ExecuteChooseEquipLocation(selector));
                choiceActionPairs.Add(equipActionPair);
                var removeActionPair = new ChoiceActionPair(localizedOptionRemove.GetSafeLocalizedString(), () => ExecuteRemoveEquipment(selector));
                choiceActionPairs.Add(removeActionPair);

                DialogueOptionBox equipmentOptionMenu = Instantiate(dialogueOptionBoxPrefab, transform.parent);
                equipmentOptionMenu.Setup(localizedOptionText.GetSafeLocalizedString());
                equipmentOptionMenu.OverrideChoiceOptions(choiceActionPairs);

                PassControl(this, new Action[] { () => ResetEquipmentBox(false), () => EnableInput(true) }, equipmentOptionMenu, controller);
                equipmentOptionMenu.ClearDisableCallbacksOnChoose(true);
                SetEquipmentBoxState(EquipmentBoxState.InEquipmentOptionMenu);
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
                SpawnMessage(localizedMessageNoValidItems.GetSafeLocalizedString());
            }
        }

        private void ExecuteRemoveEquipment(int selector)
        {
            if (selectedEquipment == null) { return; }

            var equipLocation = (EquipLocation)selector;
            selectedEquipment.RemoveEquipment(equipLocation, true);

            SpawnMessage(localizedMessageUnequip.GetSafeLocalizedString());
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

        private void ConfirmEquipmentChange(bool confirm)
        {
            if (confirm)
            {
                selectedEquipment.AddEquipment(selectedItem, true);
            }
            else
            {
                // Reset chosen item & slot -> pulls to equipment selection
                ChooseCharacter(selectedCharacter, true);
            }
        }
        #endregion

        #region Interfaces
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType is PlayerInputType.Option or PlayerInputType.Cancel)
            {
                switch (equipmentBoxState)
                {
                    case EquipmentBoxState.InStatConfirmation:
                        ResetEquipmentBox(false);
                        return true;
                    case EquipmentBoxState.InEquipmentSelection:
                        ResetEquipmentBox(true);
                        return true;
                }
                // inKnapsack handled by the EquipmentInventoryBox
            }
            
            return base.HandleGlobalInput(playerInputType);
        }

        public InventoryItemField SetupItem(InventoryItemField setInventoryItemFieldPrefab, Transform container, int selector)
        {
            var equipLocation = (EquipLocation)selector;
            string itemName = localizedEmptyEquipmentItem.GetSafeLocalizedString();
            if (selectedEquipment.HasItemInSlot(equipLocation))
            {
                itemName = selectedEquipment.GetItemInSlot(equipLocation).GetDisplayName();
            }
            string fieldName = $"{LocalizationNames.GetLocalizedName(equipLocation)}:  {itemName}";

            InventoryItemField inventoryItemField = Instantiate(setInventoryItemFieldPrefab, container);
            inventoryItemField.SetText(fieldName);
            inventoryItemField.SetupButtonAction(this, ChooseEquipLocation, selector);
            equipableItemChoiceOptions.Add(inventoryItemField);

            return inventoryItemField;
        }
        #endregion
    }
}
