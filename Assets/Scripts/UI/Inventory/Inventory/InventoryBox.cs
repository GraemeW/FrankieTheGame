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
using Frankie.Speech.UI;
using Frankie.Combat.UI;
using Frankie.Stats;
using Frankie.Stats.UI;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public class InventoryBox : UIBox<InventoryBoxState>, ILocalizable
    {
        // Tunables
        [Header("Prefabs")]
        [SerializeField] protected DialogueBox dialogueBoxPrefab;
        [SerializeField] protected DialogueOptionBox dialogueOptionBoxPrefab;
        [SerializeField] private InventoryMoveBox inventoryMoveBoxPrefab;
        [Header("Info/Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] protected LocalizedString localizedOptionInspect;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] protected LocalizedString localizedOptionUse;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] protected LocalizedString localizedOptionMove;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] protected LocalizedString localizedOptionDrop;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] protected LocalizedString localizedConfirmChoiceAffirmative;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] protected LocalizedString localizedConfirmChoiceNegative;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageCannotUseItemInWorld;
        [Header("Include {0} for character name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageBusyInCooldown;
        [Header("Include {0} for user, {1} for item, {2} for target")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageUseItemInWorld;
        [Header("Include {0} for item name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageDropItem;

        // State -- UI
        private readonly ItemBoxModel itemBoxModel = new();
        private readonly List<IUIChoice> characterChoices = new();
        private readonly List<InventorySlotHandle> slotHandles = new(); // One per knapsack slot, re-texted as the knapsack changes
        private readonly List<IUIChoice> itemChoices = new(); // Selectable subset of the slot handles
        private readonly Dictionary<Knapsack, KnapsackModel> knapsackModels = new();

        // State
        private bool isPartySolo = false;
        private int selectedItemSlot = -1;
        protected CombatParticipant selectedCharacter;
        protected KnapsackModel selectedKnapsackModel { get; private set; }
        private BattleActionData battleActionData;

        // Cached References
        private UIToolkitMenuView menuView;
        private BattleController battleController;
        private PartyCombatConduit partyCombatConduit;
        private IReadOnlyList<BattleEntity> partyBattleEntities = Array.Empty<BattleEntity>();
        private readonly List<ICharacterSlide> characterSlides = new();

        // Events
        public event Action<BattleEntitySelectionType, IEnumerable<BattleEntity>> targetCharacterChanged;

        // UIBox Configuration
        protected override EnumLookup<InventoryBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var inventoryConfiguration = new EnumLookup<InventoryBoxState,UIBoxStateBehaviour>();
            inventoryConfiguration.TrySet(InventoryBoxState.InCharacterSelection,
                new UIBoxStateBehaviour(
                    setupChoiceOptions: TrySetupChoiceOptionsFromCharacterSelection,
                    choose: _ => StandardChoose(null),
                    moveCursor: (input, _) => StandardMoveCursor(input, CursorMovementStyle.Horizontal))
            );
            inventoryConfiguration.TrySet(InventoryBoxState.InKnapsack,
                new UIBoxStateBehaviour(
                    setupChoiceOptions: TrySetupChoiceOptionsFromKnapsack,
                    choose: _ => StandardChoose(null),
                    moveCursor: (input, _) => MoveCursor2D(input),
                    tryHandleBackNavigation: TryBackFromKnapsack)
            );
            inventoryConfiguration.TrySet(InventoryBoxState.InCharacterTargeting,
                new UIBoxStateBehaviour(
                    // Targeting has no choices of its own - keep input available (avoid short circuit on user control)
                    setupChoiceOptions: () => SetChoiceAvailable(true),
                    reconcileChoiceOptions: () => SetChoiceAvailable(true),
                    choose: _ => TryUseItem(),
                    moveCursor: (input, _) => TryTargetCharacter(input))
            );
            return inventoryConfiguration;
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            uiState = InventoryBoxState.InCharacterSelection;
            menuView = GetComponent<UIToolkitMenuView>();
            menuView.SetDataSource(itemBoxModel);
        }

        protected override void EnableTriggered()
        {
            SubscribeCharacterSlides(true);
        }

        protected override void DisableTriggered()
        {
            SubscribeCharacterSlides(false);
        }

        protected override void DestroyTriggered()
        {
            SetSelectedKnapsack(null);
            foreach (KnapsackModel knapsackModel in knapsackModels.Values) { knapsackModel.Dispose(); }
            knapsackModels.Clear();
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public virtual List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedOptionText.TableEntryReference,
                localizedOptionInspect.TableEntryReference,
                localizedOptionUse.TableEntryReference,
                localizedOptionMove.TableEntryReference,
                localizedOptionDrop.TableEntryReference,
                localizedConfirmChoiceAffirmative.TableEntryReference,
                localizedConfirmChoiceNegative.TableEntryReference,
                localizedMessageBusyInCooldown.TableEntryReference,
                localizedMessageUseItemInWorld.TableEntryReference,
                localizedMessageDropItem.TableEntryReference,
            };
        }
        #endregion

        #region Setup
        public void Setup(BaseController baseController, PartyCombatConduit setPartyCombatConduit, List<ICharacterSlide> setCharacterSlides, bool useSoloAutoSelect = true)
        {
            if (baseController == null || setPartyCombatConduit == null) { destroyQueued = true;  return; }

            controller = baseController;
            partyCombatConduit = setPartyCombatConduit;
            isPartySolo = partyCombatConduit.IsPartySolo();
            battleController = baseController as BattleController;

            // Note:  Choice execution raises ItemSelected (sound) - no further raise on choosing a character
            var partySelector = new PartySelector(partyCombatConduit, AddNonDestroyChoiceOption, character => ChooseCharacter(character), SoftChooseCharacter);
            characterChoices.AddRange(partySelector.choices);
            partyBattleEntities = partySelector.battleEntities;
            if (battleController == null) { SetCharacterSlides(setCharacterSlides); } // Battle controller handles slides separately

            SetInventoryBoxState(InventoryBoxState.InCharacterSelection, true);
            ShowCursorOnAnyInteraction(ControllerInputType.Execute);
            if (useSoloAutoSelect && isPartySolo) { Choose(null); }
        }

        // For derivative Inventory Boxes w/ single party member instantiation for specific application
        protected void Setup(CombatParticipant character, List<ICharacterSlide> setCharacterSlides)
        {
            SetCharacterSlides(setCharacterSlides);

            IUIChoice characterChoice = AddNonDestroyChoiceOption(character.GetCombatName(), () => ChooseCharacter(character));
            characterChoices.Add(characterChoice);
            SetHighlightedChoice(characterChoice); // Marks the header as the owner of the items below (as per the party header)
            ChooseCharacter(character);
        }

        private void SetCharacterSlides(List<ICharacterSlide> setCharacterSlides)
        {
            characterSlides.Clear();
            if (setCharacterSlides != null) { characterSlides.AddRange(setCharacterSlides); }
            SubscribeCharacterSlides(true);
        }

        private void SubscribeCharacterSlides(bool enable)
        {
            if (battleController != null) { return; } // Battle controller handles slides separately

            foreach (ICharacterSlide characterSlide in characterSlides)
            {
                targetCharacterChanged -= characterSlide.HighlightSlide;
                characterSlide.RemoveButtonClickEvents();
                
                if (!enable) { characterSlide.HighlightSlide(BattleEntitySelectionType.Target, null); } // Clear any targeting highlight on exit
                else
                {
                    targetCharacterChanged += characterSlide.HighlightSlide;
                    characterSlide.AddButtonClickEvent(delegate { SlideUseItemOnTarget(characterSlide.GetBattleEntity()); });
                }
            }
        }

        private void TrySetupChoiceOptionsFromCharacterSelection()
        {
            choiceOptions.Clear();
            selectedItemSlot = -1;
            choiceOptions.AddRange(characterChoices);
            SetChoiceAvailable(choiceOptions.Count > 0);
        }

        private void TrySetupChoiceOptionsFromKnapsack()
        {
            choiceOptions.Clear();
            selectedItemSlot = -1;
            choiceOptions.AddRange(itemChoices);
            SetChoiceAvailable(choiceOptions.Count > 0);
        }

        private void ReInitializeToCharacterSelection()
        {
            ClearAllChoices();
            ChooseCharacter(null);
            ShowCursorOnAnyInteraction(ControllerInputType.Execute);
        }

        private void ClearAllChoices()
        {
            // Note:  Covers both lists, since only one of them is in choiceOptions at a time
            foreach (IUIChoice characterChoice in characterChoices) { characterChoice.Highlight(false); }
            foreach (InventorySlotHandle slotHandle in slotHandles) { slotHandle.Highlight(false); }
            ClearChoiceSelections();
        }

        protected void SetInventoryBoxState(InventoryBoxState setInventoryBoxState, bool bypassSoloCheck = false)
        {
            if (setInventoryBoxState == InventoryBoxState.InCharacterSelection && !bypassSoloCheck && isPartySolo)
            {
                // Skip character selection on solo character
                destroyQueued = true;
                return;
            }

            uiState = setInventoryBoxState;
            if (uiState == InventoryBoxState.InCharacterSelection) { battleActionData = null; } // Reset battle action data on selected character changed
            SetUpChoiceOptions();
        }

        protected DialogueBox SpawnDialogueBox(string text, List<ChoiceActionPair> choiceActionPairs = null)
        {
            bool isSimpleDialogueBox = choiceActionPairs == null;
            DialogueBox dialogueBox = isSimpleDialogueBox ? Instantiate(dialogueBoxPrefab, transform.parent) : Instantiate(dialogueOptionBoxPrefab, transform.parent);
            dialogueBox.AddText(text);
            if (!isSimpleDialogueBox) { dialogueBox.OverrideChoiceOptions(choiceActionPairs); }
            return dialogueBox;
        }
        #endregion

        #region Interaction
        protected virtual void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false);
            SetInventoryBoxState(InventoryBoxState.InCharacterSelection, true);
        }

        protected virtual void ChooseCharacter(CombatParticipant character, bool initializeCursor = true)
        {
            if (character == null)
            {
                SetInventoryBoxState(InventoryBoxState.InCharacterSelection);
                return;
            }

            UpdateKnapsackView(character);
            battleActionData = new BattleActionData(selectedCharacter);
            SetInventoryBoxState(InventoryBoxState.InKnapsack);

            if (!IsChoiceAvailable()) { SetInventoryBoxState(InventoryBoxState.InCharacterSelection, true); return; }
            if (initializeCursor) { SetHighlightedChoice(choiceOptions[0]); }
        }

        protected void UpdateKnapsackView(CombatParticipant character)
        {
            if (character == null || character == selectedCharacter) { return; }

            selectedCharacter = character;
            itemBoxModel.characterName = selectedCharacter.GetCombatName();
            SetSelectedKnapsack(selectedCharacter.GetComponent<Knapsack>());
            RefreshKnapsackContents();
        }

        protected virtual void ChooseItem(int inventorySlot)
        {
            List<ChoiceActionPair> choiceActionPairs = GetChoiceActionPairs(inventorySlot);
            switch (choiceActionPairs.Count)
            {
                case 0:
                    return;
                case 1:
                    choiceActionPairs[0].action?.Invoke();
                    return;
            }

            SetInventoryBoxState(InventoryBoxState.InItemDetail);
            DialogueBox dialogueOptionBox = SpawnDialogueBox(localizedOptionText.GetSafeLocalizedString(), choiceActionPairs);
            controller.AddInputReceiver(dialogueOptionBox, ResetSelectState);
            dialogueOptionBox.ClearDisableCallbacksOnChoose(true);
        }

        private bool TryTargetCharacter(ControllerInputType controllerInputType)
        {
            TargetingNavigationType targetingNavigationType = TargetingStrategy.ConvertPlayerInputToTargeting(controllerInputType);
            bool gotNextTarget = GetNextTarget(targetingNavigationType);
            if (gotNextTarget) { return true; }

            SetInventoryBoxState(InventoryBoxState.InKnapsack);
            return false;
        }

        private bool GetNextTarget(TargetingNavigationType targetingNavigationType, IEnumerable<BattleEntity> activeCharacters = null)
        {
            var actionItem = selectedKnapsack.GetItemInSlot(selectedItemSlot) as ActionItem;
            if (actionItem == null) { return false; }

            battleActionData ??= new BattleActionData(selectedCharacter);
            activeCharacters ??= partyBattleEntities;
            actionItem.SetTargets(targetingNavigationType, battleActionData, activeCharacters, null);
            if (!battleActionData.HasTargets()) { return false; }

            targetCharacterChanged?.Invoke(BattleEntitySelectionType.Target, battleActionData.GetTargets());
            return true;
        }

        private bool TryUseItem()
        {
            if (uiState != InventoryBoxState.InCharacterTargeting) { return false; }

            InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(selectedItemSlot);
            if (inventoryItem == null || battleActionData == null) { return false; }
            if (!battleActionData.HasTargets())
            {
                GetNextTarget(TargetingNavigationType.Hold);
                return false;
            }

            string senderName = selectedCharacter != null ? selectedCharacter.GetCombatName() : "";
            string itemName = inventoryItem.GetDisplayName();
            var targetCharacterNames = string.Join(", ", battleActionData.GetTargets().Select(x => x.combatParticipant.GetCombatName()).ToList());
            if (!selectedKnapsack.UseItemInSlot(selectedItemSlot, battleActionData.GetTargets())) { return false; }

            TriggerUIBoxModified(ReceiverModifiedType.ItemSelected, new ReceiverModifiedData(this));
            DialogueBox useDialogueBox = SpawnDialogueBox(string.Format(localizedMessageUseItemInWorld.GetSafeLocalizedString(), senderName, itemName, targetCharacterNames));
            controller.AddInputReceiver(useDialogueBox, ResetSelectState);

            ResetSelectState();
            return true;
        }

        private void SlideUseItemOnTarget(BattleEntity battleEntity)
        {
            if (uiState != InventoryBoxState.InCharacterTargeting) { return; }
            if (!GetNextTarget(TargetingNavigationType.Hold, new[] { battleEntity })) { SetInventoryBoxState(InventoryBoxState.InKnapsack); return; } // Verify passed combatParticipant is valid target
            TryUseItem();
        }

        protected void ResetSelectState()
        {
            selectedItemSlot = -1;
            targetCharacterChanged?.Invoke(BattleEntitySelectionType.Target, null);

            if (selectedCharacter == null || selectedKnapsack == null || selectedKnapsack.IsEmpty())
            {
                ReInitializeToCharacterSelection();
                return;
            }
            battleActionData = new BattleActionData(selectedCharacter);
            SetInventoryBoxState(InventoryBoxState.InKnapsack);
        }

        private bool TryBackFromKnapsack(ControllerInputType controllerInputType)
        {
            ClearAllChoices();
            SetInventoryBoxState(InventoryBoxState.InCharacterSelection);
            return true;
        }
        #endregion

        #region KnapsackBehaviour
        protected Knapsack selectedKnapsack => selectedKnapsackModel?.knapsack;
        protected void SetStatChanges(IReadOnlyList<StatChangeLine> statChanges) => itemBoxModel.statChanges = statChanges;

        protected virtual void RefreshKnapsackContents()
        {
            itemChoices.Clear();
            IReadOnlyList<KnapsackSlot> knapsackSlots = selectedKnapsackModel != null ? selectedKnapsackModel.slots : Array.Empty<KnapsackSlot>();
            for (int slot = 0; slot < Mathf.Max(knapsackSlots.Count, slotHandles.Count); slot++)
            {
                InventorySlotHandle slotHandle = GetOrBuildSlotHandle(slot);
                if (slot >= knapsackSlots.Count) { slotHandle.model.isShown = false; }

                if (slot < knapsackSlots.Count && ConfigureSlot(slotHandle.model, knapsackSlots[slot])) { itemChoices.Add(slotHandle); }
                else { slotHandle.Highlight(false); }
            }

            if (highlightedChoiceOption is InventorySlotHandle && !itemChoices.Contains(highlightedChoiceOption)) { ClearChoiceSelections(); }
            if (uiState == InventoryBoxState.InKnapsack) { SetUpChoiceOptions(); }
        }

        // Returns whether the slot is selectable
        protected virtual bool ConfigureSlot(InventorySlotModel slotModel, KnapsackSlot knapsackSlot)
        {
            slotModel.text = knapsackSlot.hasItem ? knapsackSlot.item.GetDisplayName() : "";
            slotModel.isEquipped = knapsackSlot.isEquipped;
            slotModel.isDimmed = false;
            slotModel.isShown = knapsackSlot.hasItem;
            return knapsackSlot.hasItem;
        }

        private InventorySlotHandle GetOrBuildSlotHandle(int slot)
        {
            // Note:  Slots alternate columns, so choice order (by slot) reads left-to-right, top-to-bottom for MoveCursor2D
            while (slotHandles.Count <= slot)
            {
                int inventorySlot = slotHandles.Count;
                var slotHandle = new InventorySlotHandle(menuView, inventorySlot % 2 == 1, () => HandleSlotChosen(inventorySlot));
                menuView.AddEntry(slotHandle);
                slotHandles.Add(slotHandle);
            }
            return slotHandles[slot];
        }

        private void HandleSlotChosen(int inventorySlot)
        {
            if (uiState is not (InventoryBoxState.InKnapsack or InventoryBoxState.InCharacterSelection)) { return; }
            StandardChoiceExecution(() => ChooseItem(inventorySlot), false);
        }

        private void SetSelectedKnapsack(Knapsack knapsack)
        {
            if (selectedKnapsackModel != null) { selectedKnapsackModel.propertyChanged -= HandleSelectedKnapsackChanged; }
            selectedKnapsackModel = knapsack != null ? GetOrBuildKnapsackModel(knapsack) : null;
            if (selectedKnapsackModel != null) { selectedKnapsackModel.propertyChanged += HandleSelectedKnapsackChanged; }
        }

        private KnapsackModel GetOrBuildKnapsackModel(Knapsack knapsack)
        {
            if (knapsackModels.TryGetValue(knapsack, out KnapsackModel knapsackModel)) { return knapsackModel; }

            knapsackModel = new KnapsackModel(knapsack);
            knapsackModels[knapsack] = knapsackModel;
            return knapsackModel;
        }

        private void HandleSelectedKnapsackChanged(object sender, BindablePropertyChangedEventArgs propertyChangedEventArgs) => RefreshKnapsackContents();
        #endregion

        #region ItemBehaviour
        protected virtual List<ChoiceActionPair> GetChoiceActionPairs(int inventorySlot)
        {
            var choiceActionPairs = new List<ChoiceActionPair>();
            if (selectedKnapsack == null) { return choiceActionPairs; }
            InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
            if (inventoryItem == null) { return choiceActionPairs; }

            // Use
            if (inventoryItem.GetType() == typeof(ActionItem))
            {
                var useActionPair = new ChoiceActionPair(localizedOptionUse.GetSafeLocalizedString(), () => Use(inventorySlot));
                choiceActionPairs.Add(useActionPair);
            }
            // Inspect
            var inspectActionPair = new ChoiceActionPair(localizedOptionInspect.GetSafeLocalizedString(), () => Inspect(inventorySlot));
            choiceActionPairs.Add(inspectActionPair);

            // Move
            var moveActionPair = new ChoiceActionPair(localizedOptionMove.GetSafeLocalizedString(), () => Move(inventorySlot));
            choiceActionPairs.Add(moveActionPair);

            // Drop
            if (inventoryItem.IsDroppable())
            {
                var dropActionPair = new ChoiceActionPair(localizedOptionDrop.GetSafeLocalizedString(), () => Drop(inventorySlot));
                choiceActionPairs.Add(dropActionPair);
            }

            return choiceActionPairs;
        }
        #endregion

        #region UserBehaviour
        private void Inspect(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }
            DialogueBox dialogueBox = SpawnDialogueBox(selectedKnapsack.GetItemInSlot(inventorySlot).GetDetail());
            controller.AddInputReceiver(dialogueBox, ResetSelectState);
        }

        private void Move(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }

            InventoryMoveBox inventoryMoveBox = Instantiate(inventoryMoveBoxPrefab, transform.parent);
            inventoryMoveBox.Setup(controller, partyCombatConduit, selectedKnapsack, inventorySlot, characterSlides);
            SetVisible(false);
            controller.AddInputReceiver(inventoryMoveBox, () =>
            {
                ResetSelectState();
                SetVisible(true);
            });

            SetInventoryBoxState(InventoryBoxState.InItemMoving);
        }

        private void Drop(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }
            if (!selectedKnapsack.HasItemInSlot(inventorySlot)) { return; }

            var choiceActionPairs = new List<ChoiceActionPair>();
            var confirmDrop = new ChoiceActionPair(localizedConfirmChoiceAffirmative.GetSafeLocalizedString(), () => ExecuteDrop(inventorySlot));
            choiceActionPairs.Add(confirmDrop);
            var rejectDrop = new ChoiceActionPair(localizedConfirmChoiceNegative.GetSafeLocalizedString(), () => ExecuteDrop(-1));
            choiceActionPairs.Add(rejectDrop);

            DialogueBox dialogueBox = SpawnDialogueBox(string.Format(localizedMessageDropItem.GetSafeLocalizedString(), selectedKnapsack.GetItemInSlot(inventorySlot).GetDisplayName()), choiceActionPairs);
            controller.AddInputReceiver(dialogueBox, ResetSelectState);
            return;

            // Local Functions
            void ExecuteDrop(int dropSlot) { if (dropSlot != -1) { selectedKnapsack.DropItem(dropSlot); }}
        }

        private void Use(int inventorySlot)
        {
            if (selectedKnapsack.GetItemInSlot(inventorySlot).GetType() != typeof(ActionItem)) { return; }

            if (battleController != null)
            {
                if (battleController.SetSelectedCharacter(selectedCharacter)) // Check for cooldown
                {
                    battleController.SetActiveBattleAction(selectedKnapsack.GetItemInSlot(inventorySlot) as ActionItem);
                    battleController.SetBattleActionArmed(true);
                    battleController.SetBattleState(BattleState.Combat, BattleOutcome.Undetermined);

                    // Prevent combat options from triggering -> proceed directly to target selection
                    ClearDisableCallbacks();
                    Destroy(gameObject);
                }
                else
                {
                    DisplayCharacterInCooldownMessage(selectedCharacter);
                }
            }
            else
            {
                selectedItemSlot = inventorySlot;
                bool hasValidTarget = GetNextTarget(TargetingNavigationType.Hold);
                battleActionData ??= new BattleActionData(selectedCharacter);
                if (!hasValidTarget)
                {
                    DialogueBox cannotUseDialogueBox = SpawnDialogueBox(string.Format(localizedMessageCannotUseItemInWorld.GetSafeLocalizedString()));
                    controller.AddInputReceiver(cannotUseDialogueBox, ResetSelectState);
                }
                SetInventoryBoxState(hasValidTarget ? InventoryBoxState.InCharacterTargeting : InventoryBoxState.InKnapsack);
            }
        }

        private void DisplayCharacterInCooldownMessage(CombatParticipant character)
        {
            DialogueBox dialogueBox = SpawnDialogueBox(string.Format(localizedMessageBusyInCooldown.GetSafeLocalizedString(), character.GetCombatName()));
            controller.AddInputReceiver(dialogueBox, ResetSelectState);
        }
        #endregion
    }
}
