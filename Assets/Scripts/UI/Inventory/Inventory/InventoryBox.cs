using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Combat;
using Frankie.Control;
using Frankie.Utils;
using Frankie.Utils.UI;
using Frankie.Speech.UI;
using Frankie.Combat.UI;
using Frankie.Stats;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    public class InventoryBox : UIBox<InventoryBoxState>, IUIItemHandler, ILocalizable
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] private TextMeshProUGUI selectedCharacterNameField;
        [Header("Parents")]
        [SerializeField] protected Transform leftItemContainer;
        [SerializeField] protected Transform rightItemContainer;
        [Header("Prefabs")]
        [SerializeField] protected DialogueBox dialogueBoxPrefab;
        [SerializeField] protected DialogueOptionBox dialogueOptionBoxPrefab;
        [SerializeField] protected InventoryItemField inventoryItemFieldPrefab;
        [SerializeField] private GameObject inventoryMoveBoxPrefab;
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
        private readonly List<UIChoiceButton> playerSelectChoiceOptions = new();
        protected readonly List<InventoryItemField> inventoryItemChoiceOptions = new();
        
        // State
        private bool isPartySolo = false;
        private int selectedItemSlot = -1;
        protected CombatParticipant selectedCharacter;
        protected Knapsack selectedKnapsack;
        private BattleActionData battleActionData;

        // Cached References
        private BattleController battleController;
        private PartyCombatConduit partyCombatConduit;
        private readonly List<BattleEntity> partyBattleEntities = new();
        private readonly List<CharacterSlide> characterSlides = new();

        // Events
        public event Action<Enum> uiBoxStateChanged;
        public event Action<CombatParticipantType, IEnumerable<BattleEntity>> targetCharacterChanged;
        
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
                    setupChoiceOptions: () => SetChoiceAvailable(true), // avoid short circuit on user control for other states
                    choose: _ => TryUseItem(),
                    moveCursor: (input, _) => TryTargetCharacter(input))
            );
            return inventoryConfiguration;
        }
        
        #region UnityMethods
        protected override void AwakeTriggered()
        {
            uiState = InventoryBoxState.InCharacterSelection;
        }

        protected override void EnableTriggered()
        {
            SubscribeCharacterSlides(true);
            ListenToKnapsack(true);
        }

        protected override void DisabledTriggered()
        {
            SubscribeCharacterSlides(false);
            ListenToKnapsack(false);
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
        public void Setup(BaseController baseController, PartyCombatConduit setPartyCombatConduit, List<CharacterSlide> setCharacterSlides, bool useSoloAutoSelect = true)
        {
            if (baseController == null || setPartyCombatConduit == null) { destroyQueued = true;  return; }
            
            controller = baseController;
            partyCombatConduit = setPartyCombatConduit;
            isPartySolo = partyCombatConduit.IsPartySolo();
            setCharacterSlides ??= new List<CharacterSlide>();

            if (baseController.GetType() == typeof(BattleController))
            {
                battleController = baseController as BattleController;
            }
            else
            {
                partyBattleEntities.Clear();
                foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
                {
                    partyBattleEntities.Add(new BattleEntity(combatParticipant));
                }

                characterSlides.Clear();
                foreach (CharacterSlide characterSlide in setCharacterSlides) { characterSlides.Add(characterSlide); }
                SubscribeCharacterSlides(true);
            }

            SetupPartySelection();
            SetInventoryBoxState(InventoryBoxState.InCharacterSelection, true);
            ShowCursorOnAnyInteraction(ControllerInputType.Execute);
            if (useSoloAutoSelect && isPartySolo) { Choose(null); }
        }

        private void SetupPartySelection()
        {
            int choiceIndex = 0;
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                GameObject uiChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
                var uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceButton>();
                uiChoiceOption.SetChoiceOrder(choiceIndex);
                uiChoiceOption.DisableOnClickListeners();
                uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(combatParticipant); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(combatParticipant); });
                uiChoiceOption.SetText(combatParticipant.GetCombatName());
                uiChoiceOption.SetValidColor(choiceIndex == 0);
                uiChoiceOption.UseInvalidChoiceDimming(true);

                playerSelectChoiceOptions.Add(uiChoiceOption);
                choiceIndex++;
            }
        }

        // For derivative Inventory Boxes w/ single party member instantiation for specific application
        protected void Setup(CombatParticipant character, List<CharacterSlide> setCharacterSlides)
        {
            characterSlides.Clear();
            foreach (CharacterSlide characterSlide in setCharacterSlides) { characterSlides.Add(characterSlide); }
            SubscribeCharacterSlides(true);

            GameObject uiChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
            UIChoiceButton uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceButton>();
            uiChoiceOption.SetChoiceOrder(0);
            uiChoiceOption.SetText(character.GetCombatName());
            uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(character); });
            playerSelectChoiceOptions.Add(uiChoiceOption);
            ChooseCharacter(character);
        }

        private void SubscribeCharacterSlides(bool enable)
        {
            if (controller != null && controller.GetType() == typeof(BattleController)) { return; } // Battle controller handles slides separately
            if (characterSlides == null) { return; }
            
            foreach (CharacterSlide characterSlide in characterSlides)
            {
                targetCharacterChanged -= characterSlide.HighlightSlide;
                characterSlide.RemoveButtonClickEvents();
                if (enable)
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
            choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList());
            SetChoiceAvailable(choiceOptions.Count > 0);
        }
        
        private void TrySetupChoiceOptionsFromKnapsack()
        {
            choiceOptions.Clear();
            selectedItemSlot = -1;
            choiceOptions.AddRange(inventoryItemChoiceOptions.Cast<UIChoice>().OrderBy(x => x.choiceOrder).ToList());
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
            foreach (UIChoiceButton dialogueChoiceOption in playerSelectChoiceOptions)
            {
                dialogueChoiceOption.Highlight(false);
                dialogueChoiceOption.SetValidColor(dialogueChoiceOption == highlightedChoiceOption);
            }
            foreach (InventoryItemField inventoryItemField in inventoryItemChoiceOptions)
            {
                inventoryItemField.Highlight(false);
            }
            highlightedChoiceOption = null;
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

            uiBoxStateChanged?.Invoke(uiState);
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
            ChooseCharacter(character, false, false);
            SetInventoryBoxState(InventoryBoxState.InCharacterSelection, true);
        }
        
        protected virtual void ChooseCharacter(CombatParticipant character, bool initializeCursor = true, bool triggerUIBoxModified = true)
        {
            UpdateKnapsackView(character);
            battleActionData = new BattleActionData(selectedCharacter);
            SetInventoryBoxState(InventoryBoxState.InKnapsack);
            if (triggerUIBoxModified) { TriggerUIBoxModified(ReceiverModifiedType.ItemSelected, new ReceiverModifiedData(this)); }

            if (initializeCursor && IsChoiceAvailable()) { MoveCursor(ControllerInputType.NavigateRight, CursorMovementStyle.Combined); }
            if (!IsChoiceAvailable()) { SetInventoryBoxState(InventoryBoxState.InCharacterSelection, true); }
        }

        protected void UpdateKnapsackView(CombatParticipant character)
        {
            if (character == null)
            {
                selectedKnapsack = null;
                SetInventoryBoxState(InventoryBoxState.InCharacterSelection);
                return;
            }
            if (character == selectedCharacter) return;
            
            selectedCharacter = character;
            selectedCharacterNameField.text = selectedCharacter.GetCombatName();
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
        
        private bool GetNextTarget(TargetingNavigationType targetingNavigationType, IList<BattleEntity> activeCharacters = null)
        {
            var actionItem = selectedKnapsack.GetItemInSlot(selectedItemSlot) as ActionItem;
            if (actionItem == null) { return false; }

            battleActionData ??= new BattleActionData(selectedCharacter);
            activeCharacters ??= partyBattleEntities;
            actionItem.SetTargets(targetingNavigationType, battleActionData, activeCharacters, null);
            if (!battleActionData.HasTargets()) { return false; }

            targetCharacterChanged?.Invoke(CombatParticipantType.Foe, battleActionData.GetTargets());
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
            targetCharacterChanged?.Invoke(CombatParticipantType.Foe, null);

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
        protected void RefreshKnapsackContents()
        {
            if (!CleanUpOldKnapsack()) { return; } // Error handling for message received during deconstruction

            SetSelectedKnapsack(selectedCharacter.GetComponent<Knapsack>());
            PopulateKnapsackContents();
        }

        protected virtual void PopulateKnapsackContents()
        {
            for (int i = 0; i < selectedKnapsack.GetSize(); i++)
            {
                InventoryItemField inventoryItemField = (i % 2 == 0) ?
                    SetupItem(inventoryItemFieldPrefab, leftItemContainer, i) :
                    SetupItem(inventoryItemFieldPrefab, rightItemContainer, i);

                if (selectedKnapsack.IsItemInSlotEquipped(i))
                {
                    inventoryItemField.SetEquipped(true);
                }
            }
        }

        private bool CleanUpOldKnapsack()
        {
            if (leftItemContainer == null || rightItemContainer == null) { return false; } // Error handling for message received during deconstruction

            inventoryItemChoiceOptions.Clear();
            foreach (Transform child in leftItemContainer) { Destroy(child.gameObject); }
            foreach (Transform child in rightItemContainer) { Destroy(child.gameObject); }
            return true;
        }

        private void SetSelectedKnapsack(Knapsack knapsack)
        {
            ListenToKnapsack(false); // Remove subscription to current knapsack
            selectedKnapsack = knapsack;
            ListenToKnapsack(true); // Attach subscription to new knapsack
        }

        protected virtual void ListenToKnapsack(bool enable)
        {
            if (selectedKnapsack == null) { return; }
            selectedKnapsack.knapsackUpdated -= RefreshKnapsackContents;
            if (enable) { selectedKnapsack.knapsackUpdated += RefreshKnapsackContents; }
        }
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

        public virtual InventoryItemField SetupItem(InventoryItemField setInventoryItemFieldPrefab, Transform container, int selector)
        {
            CheckItemExists(selectedKnapsack, selector, out bool itemExists, out string itemName);
            return SpawnInventoryItemField(itemExists, itemName, setInventoryItemFieldPrefab, container, selector);
        }

        private InventoryItemField SpawnInventoryItemField(bool itemExists, string itemName, InventoryItemField setInventoryItemFieldPrefab, Transform container, int selector)
        {
            InventoryItemField inventoryItemField = Instantiate(setInventoryItemFieldPrefab, container);
            inventoryItemField.SetChoiceOrder(selector);
            inventoryItemField.SetText(itemName);
            if (itemExists)
            {
                inventoryItemField.SetupButtonAction(this, ChooseItem, selector);
                inventoryItemChoiceOptions.Add(inventoryItemField);
            }

            return inventoryItemField;
        }

        private static void CheckItemExists(Knapsack knapsack, int selector, out bool itemExists, out string itemName)
        {
            itemExists = false;
            itemName = "    "; 
            
            if (!knapsack.HasItemInSlot(selector)) { return; }
            itemExists = true;
            itemName = knapsack.GetItemInSlot(selector).GetDisplayName();
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

            GameObject inventoryMoveBoxObject = Instantiate(inventoryMoveBoxPrefab, transform.parent);
            var inventoryMoveBox = inventoryMoveBoxObject.GetComponent<InventoryMoveBox>();
            inventoryMoveBox.Setup(controller, partyCombatConduit, selectedKnapsack, inventorySlot, characterSlides);
            canvasGroup.alpha = 0.0f;
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
