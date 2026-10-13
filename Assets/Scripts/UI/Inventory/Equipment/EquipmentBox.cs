using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Combat;
using Frankie.Stats;
using Frankie.Stats.UI;
using Frankie.Combat.UI;
using Frankie.Speech.UI;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class EquipmentBox : UIBox<EquipmentBoxState>, ILocalizable
    {
        // Tunables
        [Header("Prefabs")]
        [SerializeField] private DialogueBox dialogueBoxPrefab;
        [SerializeField] private DialogueOptionBox dialogueOptionBoxPrefab;
        [SerializeField] private EquipmentInventoryBox equipmentInventoryBoxPrefab;
        [Header("Info/Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedEmptyEquipmentItem;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageNoValidItems;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageUnequip;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionEquip;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionRemove;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmChoiceAffirmative;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmChoiceNegative;

        // State -- UI
        private readonly ItemBoxModel itemBoxModel = new();
        private readonly List<IUIChoice> characterChoices = new();
        private readonly List<InventorySlotHandle> equipmentSlotHandles = new(); // One per equip location, re-texted as the equipment changes
        private readonly List<IUIChoice> confirmChoices = new();
        private readonly Dictionary<Equipment, EquipmentModel> equipmentModels = new();

        // State
        private bool isPartySolo = false;
        private CombatParticipant selectedCharacter;
        private EquipmentModel selectedEquipmentModel;
        private EquipLocation selectedEquipLocation = EquipLocation.None;
        private EquipableItemBase selectedItem;

        // Cached References
        private UIToolkitMenuView menuView;
        private readonly List<CharacterSlideHandle> characterSlides = new();

        // UIBox Configuration
        protected override EnumLookup<EquipmentBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var equipmentConfiguration = new EnumLookup<EquipmentBoxState, UIBoxStateBehaviour>();
            equipmentConfiguration.TrySet(EquipmentBoxState.InCharacterSelection,
                new UIBoxStateBehaviour(
                    setupChoiceOptions: ImplementSetUpChoiceOptions,
                    moveCursor: (input, _) => StandardMoveCursor(input, CursorMovementStyle.Horizontal))
            );
            equipmentConfiguration.TrySet(EquipmentBoxState.InEquipmentSelection,
                new UIBoxStateBehaviour(
                    setupChoiceOptions: ImplementSetUpChoiceOptions,
                    moveCursor: (input, _) => MoveCursor2D(input),
                    tryHandleBackNavigation: TryBackFromEquipmentSelection)
            );
            equipmentConfiguration.TrySet(EquipmentBoxState.InStatConfirmation,
                new UIBoxStateBehaviour(
                    setupChoiceOptions: ImplementSetUpChoiceOptions,
                    moveCursor: StandardMoveCursor,
                    tryHandleBackNavigation: TryBackFromStatConfirmation)
            );
            return equipmentConfiguration;
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            uiState = EquipmentBoxState.InCharacterSelection;
            menuView = GetComponent<UIToolkitMenuView>();
            menuView.SetDataSource(itemBoxModel);
        }

        protected override void DestroyTriggered()
        {
            SetSelectedEquipment(null);
            foreach (EquipmentModel equipmentModel in equipmentModels.Values) { equipmentModel.Dispose(); }
            equipmentModels.Clear();
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            // Note:  Confirm choices re-use localization keys from StandardConfirmationMenu (not returned, to prevent deletion of its keys)
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
        public void Setup(BaseController baseController, PartyCombatConduit partyCombatConduit, List<CharacterSlideHandle> setCharacterSlides)
        {
            if (baseController == null || partyCombatConduit == null) { destroyQueued = true;  return; }

            controller = baseController;
            isPartySolo = partyCombatConduit.IsPartySolo();

            // Note:  Choice execution raises ItemSelected (sound) - no further raise on choosing a character
            var partySelector = new PartySelector(partyCombatConduit, AddNonDestroyChoiceOption, character => ChooseCharacter(character), SoftChooseCharacter);
            characterChoices.AddRange(partySelector.choices);
            confirmChoices.Add(AddConfirmChoice(localizedConfirmChoiceAffirmative.GetSafeLocalizedString(), () => ConfirmEquipmentChange(true)));
            confirmChoices.Add(AddConfirmChoice(localizedConfirmChoiceNegative.GetSafeLocalizedString(), () => ConfirmEquipmentChange(false)));

            characterSlides.Clear();
            if (setCharacterSlides != null) { characterSlides.AddRange(setCharacterSlides); }

            SetEquipmentBoxState(EquipmentBoxState.InCharacterSelection, true);
            ShowCursorOnAnyInteraction(ControllerInputType.Execute);
            if (isPartySolo) { Choose(null); }
        }

        private IUIChoice AddConfirmChoice(string choiceText, Action action)
        {
            var confirmChoice = new ChoiceEntryHandle(menuView, choiceText, true, () => StandardChoiceExecution(action, false), null, typeof(EquipmentConfirmContainer));
            menuView.AddEntry(confirmChoice);
            return confirmChoice;
        }

        public void SetSelectedItem(EquipableItemBase equipableItem)
        {
            if (equipableItem == null || selectedEquipmentModel == null) { return; }
            if (!selectedCharacter.TryGetComponent(out BaseStats baseStats)) { return; }

            selectedItem = equipableItem;
            SetEquipmentBoxState(EquipmentBoxState.InStatConfirmation);
            itemBoxModel.statChanges = StatChangeLine.GetStatChanges(baseStats, selectedEquipmentModel.equipment, selectedItem, selectedEquipLocation);
            SetHighlightedChoice(confirmChoices[0]);
        }

        private void ImplementSetUpChoiceOptions()
        {
            choiceOptions.Clear();
            switch (uiState)
            {
                case EquipmentBoxState.InEquipmentSelection:
                    choiceOptions.AddRange(equipmentSlotHandles);
                    break;
                case EquipmentBoxState.InCharacterSelection:
                    choiceOptions.AddRange(characterChoices);
                    break;
                case EquipmentBoxState.InStatConfirmation:
                    choiceOptions.AddRange(confirmChoices);
                    break;
            }

            SetChoiceAvailable(choiceOptions.Count > 0);
        }

        public void ResetEquipmentBox(bool clearSelectedCharacter)
        {
            if (selectedCharacter != null && !clearSelectedCharacter)
            {
                ChooseCharacter(selectedCharacter); // Resets chosen item & slot -> pulls to equipment selection
            }
            else
            {
                ClearAllChoices();
                ChooseCharacter(null);
            }
        }

        private void ClearAllChoices()
        {
            // Note:  Covers every list, since only one of them is in choiceOptions at a time
            foreach (IUIChoice characterChoice in characterChoices) { characterChoice.Highlight(false); }
            foreach (InventorySlotHandle equipmentSlotHandle in equipmentSlotHandles) { equipmentSlotHandle.Highlight(false); }
            foreach (IUIChoice confirmChoice in confirmChoices) { confirmChoice.Highlight(false); }
            ClearChoiceSelections();
        }

        private void SetEquipmentBoxState(EquipmentBoxState setEquipmentBoxState, bool bypassSoloCheck = false)
        {
            if (setEquipmentBoxState == EquipmentBoxState.InCharacterSelection && !bypassSoloCheck && isPartySolo)
            {
                // Skip character selection on solo character
                destroyQueued = true;
                return;
            }

            uiState = setEquipmentBoxState;
            if (uiState != EquipmentBoxState.InStatConfirmation)
            {
                itemBoxModel.statChanges = Array.Empty<StatChangeLine>(); // Collapses the confirmation menu
                foreach (IUIChoice confirmChoice in confirmChoices) { confirmChoice.Highlight(false); }
            }
            SetUpChoiceOptions();
        }
        #endregion

        #region Interaction
        private void ChooseCharacter(CombatParticipant character, bool initializeCursor = true)
        {
            selectedEquipLocation = EquipLocation.None;
            selectedItem = null;
            if (character == null)
            {
                SetEquipmentBoxState(EquipmentBoxState.InCharacterSelection);
                return;
            }

            if (character != selectedCharacter)
            {
                selectedCharacter = character;
                itemBoxModel.characterName = selectedCharacter.GetCombatName();
                SetSelectedEquipment(selectedCharacter.GetComponent<Equipment>());
                foreach (InventorySlotHandle equipmentSlotHandle in equipmentSlotHandles) { equipmentSlotHandle.Highlight(false); }
                if (highlightedChoiceOption is InventorySlotHandle) { ClearChoiceSelections(); } // Cursor restarts for the new character
                RefreshEquipment();
            }
            SetEquipmentBoxState(EquipmentBoxState.InEquipmentSelection);

            if (initializeCursor) { InitializeCursor(); }
        }

        private void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false);
            SetEquipmentBoxState(EquipmentBoxState.InCharacterSelection, true);
        }

        private void InitializeCursor()
        {
            // Note:  Rows persist across equipment changes, so the cursor returns to the row it was last on (e.g. after a confirmation)
            if (!IsChoiceAvailable() || choiceOptions.Count == 0) { return; }
            if (IsChoiceAlive(highlightedChoiceOption) && choiceOptions.Contains(highlightedChoiceOption)) { return; }

            IUIChoice lastEquipmentSlot = equipmentSlotHandles.FirstOrDefault(equipmentSlotHandle => equipmentSlotHandle.model.isHighlighted);
            SetHighlightedChoice(lastEquipmentSlot ?? choiceOptions[0]);
        }
        #endregion

        #region EquipmentBehaviour
        private void SetSelectedEquipment(Equipment equipment)
        {
            if (selectedEquipmentModel != null) { selectedEquipmentModel.propertyChanged -= HandleSelectedEquipmentChanged; }
            selectedEquipmentModel = equipment != null ? GetOrBuildEquipmentModel(equipment) : null;
            if (selectedEquipmentModel != null) { selectedEquipmentModel.propertyChanged += HandleSelectedEquipmentChanged; }
        }

        private EquipmentModel GetOrBuildEquipmentModel(Equipment equipment)
        {
            if (equipmentModels.TryGetValue(equipment, out EquipmentModel equipmentModel)) { return equipmentModel; }

            equipmentModel = new EquipmentModel(equipment);
            equipmentModels[equipment] = equipmentModel;
            return equipmentModel;
        }

        private void HandleSelectedEquipmentChanged(object sender, BindablePropertyChangedEventArgs propertyChangedEventArgs)
        {
            RefreshEquipment();
            ResetEquipmentBox(false);
        }

        private void RefreshEquipment()
        {
            if (selectedEquipmentModel == null) { return; }

            IReadOnlyList<EquipmentSlot> equipmentSlots = selectedEquipmentModel.slots;
            for (int slot = 0; slot < equipmentSlots.Count; slot++)
            {
                EquipmentSlot equipmentSlot = equipmentSlots[slot];
                if (slot >= equipmentSlotHandles.Count) { BuildEquipmentSlotHandle(equipmentSlot.equipLocation); }

                string itemName = equipmentSlot.hasItem ? equipmentSlot.item.GetDisplayName() : localizedEmptyEquipmentItem.GetSafeLocalizedString();
                InventorySlotModel slotModel = equipmentSlotHandles[slot].model;
                slotModel.text = $"{LocalizationNames.GetLocalizedName(equipmentSlot.equipLocation)}:  {itemName}";
                slotModel.isShown = true;
            }
        }

        private void BuildEquipmentSlotHandle(EquipLocation equipLocation)
        {
            // Note:  Locations alternate columns, so choice order reads left-to-right, top-to-bottom for MoveCursor2D
            var equipmentSlotHandle = new InventorySlotHandle(menuView, equipmentSlotHandles.Count % 2 == 1, () => HandleEquipLocationChosen(equipLocation));
            menuView.AddEntry(equipmentSlotHandle);
            equipmentSlotHandles.Add(equipmentSlotHandle);
        }

        private void HandleEquipLocationChosen(EquipLocation equipLocation)
        {
            if (uiState is not (EquipmentBoxState.InEquipmentSelection or EquipmentBoxState.InCharacterSelection)) { return; }
            StandardChoiceExecution(() => ChooseEquipLocation(equipLocation), false);
        }
        #endregion

        #region UserBehaviour
        private void ChooseEquipLocation(EquipLocation equipLocation)
        {
            if (equipLocation == EquipLocation.None || selectedEquipmentModel == null) { return; }

            if (selectedEquipmentModel.equipment.HasItemInSlot(equipLocation))
            {
                var choiceActionPairs = new List<ChoiceActionPair>();
                var equipActionPair = new ChoiceActionPair(localizedOptionEquip.GetSafeLocalizedString(), () => ExecuteChooseEquipLocation(equipLocation));
                choiceActionPairs.Add(equipActionPair);
                var removeActionPair = new ChoiceActionPair(localizedOptionRemove.GetSafeLocalizedString(), () => ExecuteRemoveEquipment(equipLocation));
                choiceActionPairs.Add(removeActionPair);

                DialogueOptionBox equipmentOptionMenu = Instantiate(dialogueOptionBoxPrefab, transform.parent);
                equipmentOptionMenu.Setup(localizedOptionText.GetSafeLocalizedString());
                equipmentOptionMenu.OverrideChoiceOptions(choiceActionPairs);

                controller.AddInputReceiver(equipmentOptionMenu, () => ResetEquipmentBox(false));
                equipmentOptionMenu.ClearDisableCallbacksOnChoose(true);
                SetEquipmentBoxState(EquipmentBoxState.InEquipmentOptionMenu);
            }
            else
            {
                ExecuteChooseEquipLocation(equipLocation);
            }
        }

        private void ExecuteChooseEquipLocation(EquipLocation equipLocation)
        {
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

        private void ExecuteRemoveEquipment(EquipLocation equipLocation)
        {
            if (selectedEquipmentModel == null) { return; }

            selectedEquipmentModel.equipment.RemoveEquipment(equipLocation, true);
            SpawnMessage(localizedMessageUnequip.GetSafeLocalizedString());
        }

        private void SpawnMessage(string message)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(message);
            controller.AddInputReceiver(dialogueBox, null);
        }

        private void SpawnInventoryBox()
        {
            if (selectedEquipLocation == EquipLocation.None) { return; }

            EquipmentInventoryBox inventoryBox = Instantiate(equipmentInventoryBoxPrefab, transform.parent);
            inventoryBox.Setup(this, selectedEquipLocation, selectedCharacter, characterSlides);
            SetVisible(false);
            controller.AddInputReceiver(inventoryBox, () => SetVisible(true));
        }

        private void ConfirmEquipmentChange(bool confirm)
        {
            if (confirm)
            {
                selectedEquipmentModel.equipment.AddEquipment(selectedItem, true);
            }
            else
            {
                // Reset chosen item & slot -> pulls to equipment selection
                ChooseCharacter(selectedCharacter);
            }
        }
        #endregion

        #region Interfaces
        private bool TryBackFromStatConfirmation(ControllerInputType controllerInputType)
        {
            ResetEquipmentBox(false);
            return true;
        }

        private bool TryBackFromEquipmentSelection(ControllerInputType controllerInputType)
        {
            ResetEquipmentBox(true);
            return true;
        }
        #endregion
    }
}
