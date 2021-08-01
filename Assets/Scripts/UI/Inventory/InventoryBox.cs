using Frankie.Combat;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Combat.UI;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using UnityEngine.Events;

namespace Frankie.Inventory.UI
{
    public class InventoryBox : DialogueOptionBox, IUIItemHandler
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] TextMeshProUGUI selectedCharacterNameField = null;
        [Header("Parents")]
        [SerializeField] protected Transform leftItemContainer = null;
        [SerializeField] protected Transform rightItemContainer = null;
        [Header("Prefabs")]
        [SerializeField] protected GameObject dialogueBoxPrefab = null;
        [SerializeField] protected GameObject dialogueOptionBoxPrefab = null;
        [SerializeField] protected GameObject inventoryItemFieldPrefab = null;
        [SerializeField] GameObject inventoryMoveBoxPrefab = null;
        [Header("Info/Messages")]
        [SerializeField] protected string optionInspect = "Inspect";
        [SerializeField] protected string optionUse = "Use";
        [SerializeField] protected string optionMove = "Move";
        [SerializeField] protected string optionDrop = "Drop";
        [SerializeField] protected string confirmChoiceAffirmative = "Yes";
        [SerializeField] protected string confirmChoiceNegative = "No";
        [Tooltip("Include {0} for character name")] [SerializeField] string messageBusyInCooldown = "{0} is busy twirling, twirling.";
        [Tooltip("Include {0} for user, {1} for item, {2} for target")] [SerializeField] string messageUseItemInWorld = "{0} used {1} on {2}";
        [Tooltip("Include {0} for item name")] [SerializeField] string messageDropItem = "Are you sure you want to abandon {0}?"; 

        // State
        protected InventoryBoxState inventoryBoxState = InventoryBoxState.inCharacterSelection;
        List<DialogueChoiceOption> playerSelectChoiceOptions = new List<DialogueChoiceOption>();
        protected List<InventoryItemField> inventoryItemChoiceOptions = new List<InventoryItemField>();
        protected CombatParticipant selectedCharacter = null;
        protected Knapsack selectedKnapsack = null;
        int selectedItemSlot = -1;
        CombatParticipant targetCharacter = null;

        // Cached References
        protected IStandardPlayerInputCaller standardPlayerInputCaller = null;
        BattleController battleController = null;
        Party party = null;
        List<CharacterSlide> characterSlides = null;

        // Events
        public event Action<Enum> uiBoxStateChanged;
        public event Action<CombatParticipantType, CombatParticipant> targetCharacterChanged;

        protected override void Start()
        {
            // Do Nothing (skip base implementation)
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeCharacterSlides(true);
            ListenToKnapsack(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SubscribeCharacterSlides(false);
            ListenToKnapsack(false);
        }

        #region Setup
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party, List<CharacterSlide> characterSlides = null)
        {
            this.standardPlayerInputCaller = standardPlayerInputCaller;
            this.party = party;
            if (standardPlayerInputCaller.GetType() == typeof(BattleController))
            {
                battleController = standardPlayerInputCaller as BattleController;
            }
            else
            {
                this.characterSlides = characterSlides;
                SubscribeCharacterSlides(true);
            }
            SetGlobalCallbacks(standardPlayerInputCaller);

            int choiceIndex = 0;
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject characterFieldObject = Instantiate(optionPrefab, optionParent);
                DialogueChoiceOption dialogueChoiceOption = characterFieldObject.GetComponent<DialogueChoiceOption>();
                dialogueChoiceOption.SetChoiceOrder(choiceIndex);
                dialogueChoiceOption.SetText(character.GetCombatName());
                characterFieldObject.GetComponent<Button>().onClick.AddListener(delegate { ChooseCharacter(character); });
                dialogueChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(character); });

                playerSelectChoiceOptions.Add(dialogueChoiceOption);
                choiceIndex++;
            }
            SetInventoryBoxState(InventoryBoxState.inCharacterSelection);
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
        }

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, CombatParticipant character, List<CharacterSlide> characterSlides = null)
        {
            // Single party member instantiation for specific application

            this.standardPlayerInputCaller = standardPlayerInputCaller;
            this.characterSlides = characterSlides;
            SubscribeCharacterSlides(true);
            SetGlobalCallbacks(standardPlayerInputCaller);

            GameObject characterFieldObject = Instantiate(optionPrefab, optionParent);
            DialogueChoiceOption dialogueChoiceOption = characterFieldObject.GetComponent<DialogueChoiceOption>();
            dialogueChoiceOption.SetChoiceOrder(0);
            dialogueChoiceOption.SetText(character.GetCombatName());
            characterFieldObject.GetComponent<Button>().onClick.AddListener(delegate { ChooseCharacter(character); });
            playerSelectChoiceOptions.Add(dialogueChoiceOption);
            ChooseCharacter(character);
        }

        private void SubscribeCharacterSlides(bool enable)
        {
            if (characterSlides != null)
            {
                foreach (CharacterSlide characterSlide in characterSlides)
                {
                    if (enable)
                    {
                        targetCharacterChanged += characterSlide.HighlightSlide;
                        characterSlide.AddButtonClickEvent(delegate { UseItemOnTarget(characterSlide.GetCombatParticipant()); });
                    }
                    else
                    {
                        targetCharacterChanged -= characterSlide.HighlightSlide;
                        // Note:  Remove button click event listeners handled on battleSlide on disable (removes all listeners)
                    }
                }
            }
        }

        protected override void SetUpChoiceOptions()
        {
            if (inventoryBoxState == InventoryBoxState.inKnapsack || inventoryBoxState == InventoryBoxState.inCharacterSelection)
            {
                choiceOptions.Clear();
                if (inventoryBoxState == InventoryBoxState.inKnapsack)
                {
                    choiceOptions.AddRange(inventoryItemChoiceOptions.Cast<DialogueChoiceOption>().OrderBy(x => x.choiceOrder).ToList());
                }
                else if (inventoryBoxState == InventoryBoxState.inCharacterSelection)
                {
                    choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList());
                }

                if (choiceOptions.Count > 0) { isChoiceAvailable = true; }
                else { isChoiceAvailable = false; }
            }
            else
            {
                isChoiceAvailable = true; // avoid short circuit on user control for other states
                return;
            }
        }

        private void ReInitializeToCharacterSelection()
        {
            ClearChoiceSelections();
            ChooseCharacter(null);
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
        }

        protected override void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (DialogueChoiceOption dialogueChoiceOption in playerSelectChoiceOptions)
            {
                dialogueChoiceOption.Highlight(false);
            }
            foreach (InventoryItemField inventoryItemField in inventoryItemChoiceOptions)
            {
                inventoryItemField.Highlight(false);
            }
        }

        public void SetInventoryBoxState(InventoryBoxState inventoryBoxState)
        {
            this.inventoryBoxState = inventoryBoxState;
            SetUpChoiceOptions();

            if (uiBoxStateChanged != null)
            {
                uiBoxStateChanged.Invoke(inventoryBoxState);
            }
        }
        #endregion

        #region Interaction
        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            if (inventoryBoxState == InventoryBoxState.inCharacterSelection)
            {
                return base.MoveCursor(playerInputType);
            }
            else if (inventoryBoxState == InventoryBoxState.inKnapsack)
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
            else if (inventoryBoxState == InventoryBoxState.inCharacterTargeting)
            {
                if (playerInputType == PlayerInputType.NavigateRight || playerInputType == PlayerInputType.NavigateDown)
                {
                    GetNextTarget(true);
                }
                else if (playerInputType == PlayerInputType.NavigateLeft || playerInputType == PlayerInputType.NavigateUp)
                {
                    GetNextTarget(false);
                }
            }
            return false;
        }

        protected override bool Choose(string nodeID)
        {
            if (inventoryBoxState != InventoryBoxState.inCharacterTargeting)
            {
                return base.Choose(null);
            }
            else
            {
                if (selectedKnapsack.UseItemInSlot(selectedItemSlot, targetCharacter))
                {
                    handleGlobalInput = false;
                    GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
                    DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
                    dialogueBox.AddText(string.Format(messageUseItemInWorld, 
                        selectedCharacter.GetCombatName(), 
                        selectedKnapsack.GetItemInSlot(selectedItemSlot).GetDisplayName(), 
                        targetCharacter.GetCombatName()));
                    dialogueBox.SetGlobalCallbacks(standardPlayerInputCaller);
                    dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);

                    return true;
                }
                else
                { 
                    return false; 
                }
            }
        }

        protected void ChooseCharacter(CombatParticipant character, bool initializeCursor = true)
        {
            if (character == null)
            {
                selectedKnapsack = null;
                SetInventoryBoxState(InventoryBoxState.inCharacterSelection);
                return;
            }

            if (character != selectedCharacter)
            {
                OnDialogueBoxModified(DialogueBoxModifiedType.itemSelected, true);

                selectedCharacter = character;
                selectedCharacterNameField.text = selectedCharacter.GetCombatName();
                RefreshKnapsackContents();
            }
            SetInventoryBoxState(InventoryBoxState.inKnapsack);

            if (IsChoiceAvailable())
            {
                if (initializeCursor) { MoveCursor(PlayerInputType.NavigateRight); }
            }
            else
            {
                SetInventoryBoxState(InventoryBoxState.inCharacterSelection);
            }

        }

        private void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false);
            SetInventoryBoxState(InventoryBoxState.inCharacterSelection);
        }

        protected virtual void ChooseItem(int inventorySlot)
        {
            List<ChoiceActionPair> choiceActionPairs = GetChoiceActionPairs(inventorySlot);
            if (choiceActionPairs.Count == 0) { return; }
            else if (choiceActionPairs.Count == 1)
            {
                choiceActionPairs[0].ExecuteAction();
                return;
            }

            handleGlobalInput = false;
            SetInventoryBoxState(InventoryBoxState.inItemDetail);
            GameObject dialogueOptionBoxObject = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            DialogueOptionBox dialogueOptionBox = dialogueOptionBoxObject.GetComponent<DialogueOptionBox>();
            dialogueOptionBox.SetupSimpleChoices(choiceActionPairs);
            dialogueOptionBox.SetGlobalCallbacks(standardPlayerInputCaller);
            // Note:  Do not re-enable input control on callback
            // Control is setup and then passed back via ChoiceActionPair action menu
        }
        #endregion

        #region KnapsackBehaviour
        protected virtual void RefreshKnapsackContents()
        {
            if (!CleanUpOldKnapsack()) { return; } // Error handling for message received during deconstruction

            SetSelectedKnapsack(selectedCharacter.GetComponent<Knapsack>());
            for (int i = 0; i < selectedKnapsack.GetSize(); i++)
            {
                InventoryItemField inventoryItemField = null;
                if (i % 2 == 0)
                {
                    inventoryItemField = SetupItem(inventoryItemFieldPrefab, leftItemContainer, i);
                }
                else
                {
                    inventoryItemField = SetupItem(inventoryItemFieldPrefab, rightItemContainer, i);
                }

                if (selectedKnapsack.IsItemInSlotEquipped(i))
                {
                    inventoryItemField.SetEquipped(true);
                }
            }
        }

        protected bool CleanUpOldKnapsack()
        {
            if (leftItemContainer == null || rightItemContainer == null) { return false; } // Error handling for message received during deconstruction

            inventoryItemChoiceOptions.Clear();
            foreach (Transform child in leftItemContainer) { Destroy(child.gameObject); }
            foreach (Transform child in rightItemContainer) { Destroy(child.gameObject); }
            return true;
        }

        private void SetSelectedKnapsack(Knapsack knapsack)
        {
            ListenToKnapsack(false);
            selectedKnapsack = knapsack;
            ListenToKnapsack(true);
        }

        protected virtual void ListenToKnapsack(bool enable)
        {
            if (selectedKnapsack == null) { return; }

            if (enable)
            {
                selectedKnapsack.knapsackUpdated += RefreshKnapsackContents;
            }
            else
            {
                selectedKnapsack.knapsackUpdated -= RefreshKnapsackContents;
            }
        }
        #endregion

        #region ItemBehaviour
        protected virtual List<ChoiceActionPair> GetChoiceActionPairs(int inventorySlot)
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            if (selectedKnapsack == null) { return choiceActionPairs; }
            InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
            if (inventoryItem == null) { return choiceActionPairs; }

            // Use
            if (inventoryItem.GetType() == typeof(ActionItem))
            {
                ChoiceActionPair useActionPair = new ChoiceActionPair(optionUse, Use, inventorySlot);
                choiceActionPairs.Add(useActionPair);
            }
            // Inspect
            ChoiceActionPair inspectActionPair = new ChoiceActionPair(optionInspect, Inspect, inventorySlot);
            choiceActionPairs.Add(inspectActionPair);

            // Move
            ChoiceActionPair moveActionPair = new ChoiceActionPair(optionMove, Move, inventorySlot);
            choiceActionPairs.Add(moveActionPair);

            // Drop
            if (inventoryItem.IsDroppable())
            {
                ChoiceActionPair dropActionPair = new ChoiceActionPair(optionDrop, Drop, inventorySlot);
                choiceActionPairs.Add(dropActionPair);
            }

            return choiceActionPairs;
        }

        public virtual InventoryItemField SetupItem(GameObject inventoryItemFieldPrefab, Transform container, int selector)
        {
            CheckItemExists(selectedKnapsack, selector, out bool itemExists, out string itemName);
            return SpawnInventoryItemField(itemExists, itemName, inventoryItemFieldPrefab, container, selector);
        }

        private InventoryItemField SpawnInventoryItemField(bool itemExists, string itemName, GameObject inventoryItemFieldPrefab, Transform container, int selector)
        {
            GameObject inventoryItemFieldObject = Instantiate(inventoryItemFieldPrefab, container);
            InventoryItemField inventoryItemField = inventoryItemFieldObject.GetComponent<InventoryItemField>();
            inventoryItemField.SetChoiceOrder(selector);
            inventoryItemField.SetText(itemName);
            if (itemExists)
            {
                inventoryItemField.SetupButtonAction(this, ChooseItem, selector);
                inventoryItemChoiceOptions.Add(inventoryItemField);
            }

            return inventoryItemField;
        }

        private void CheckItemExists(Knapsack knapsack, int selector, out bool itemExists, out string itemName)
        {
            itemExists = false;
            itemName = "    ";
            if (knapsack.HasItemInSlot(selector))
            {
                itemExists = true;
                itemName = knapsack.GetItemInSlot(selector).GetDisplayName();
            }
        }
        #endregion

        #region UserBehaviour
        private void Inspect(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }

            handleGlobalInput = false;
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText(selectedKnapsack.GetItemInSlot(inventorySlot).GetDescription());
            dialogueBox.SetGlobalCallbacks(standardPlayerInputCaller);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        private void Move(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }

            handleGlobalInput = false;
            GameObject inventoryMoveBoxObject = Instantiate(inventoryMoveBoxPrefab, transform.parent);
            InventoryMoveBox inventoryMoveBox = inventoryMoveBoxObject.GetComponent<InventoryMoveBox>();
            inventoryMoveBox.Setup(standardPlayerInputCaller, party, selectedKnapsack, inventorySlot, characterSlides);
            inventoryMoveBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);

            canvasGroup.alpha = 0.0f;
            inventoryMoveBox.SetDisableCallback(this, DIALOGUE_CALLBACK_RESTORE_ALPHA);

            SetInventoryBoxState(InventoryBoxState.inItemMoving);
        }

        private void Drop(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }
            if (!selectedKnapsack.HasItemInSlot(inventorySlot)) { return; }

            handleGlobalInput = false;
            GameObject dialogueOptionBoxObject = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            DialogueOptionBox dialogueOptionBox = dialogueOptionBoxObject.GetComponent<DialogueOptionBox>();

            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            ChoiceActionPair confirmDrop = new ChoiceActionPair(confirmChoiceAffirmative, ExecuteDrop, inventorySlot);
            choiceActionPairs.Add(confirmDrop);
            ChoiceActionPair rejectDrop = new ChoiceActionPair(confirmChoiceNegative, ExecuteDrop, -1);
            choiceActionPairs.Add(rejectDrop);

            dialogueOptionBox.SetupSimpleChoices(choiceActionPairs, false, string.Format(messageDropItem, selectedKnapsack.GetItemInSlot(inventorySlot).GetDisplayName()));
            dialogueOptionBox.SetGlobalCallbacks(standardPlayerInputCaller);
            dialogueOptionBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        private void ExecuteDrop(int inventorySlot)
        {
            if (inventorySlot != -1)
            {
                selectedKnapsack.DropItem(inventorySlot);
            }
        }

        private void Use(int inventorySlot)
        {
            if (selectedKnapsack.GetItemInSlot(inventorySlot).GetType() != typeof(ActionItem)) { return; }

            if (battleController != null)
            {
                if (battleController.SetSelectedCharacter(selectedCharacter)) // Check for cooldown
                {
                    battleController.SetActiveBattleAction(new BattleAction(selectedKnapsack.GetItemInSlot(inventorySlot) as ActionItem));
                    battleController.SetBattleActionArmed(true);
                    battleController.SetBattleState(BattleState.Combat);
                    ClearDisableCallbacks(); // Prevent combat options from triggering -> proceed directly to target selection
                    Destroy(gameObject);
                }
                else
                {
                    DisplayCharacterInCooldownMessage(selectedCharacter);
                }
            }
            else
            {
                handleGlobalInput = true;
                selectedItemSlot = inventorySlot;
                SetInventoryBoxState(InventoryBoxState.inCharacterTargeting);
                GetNextTarget(true);
            }
        }

        private void GetNextTarget(bool traverseForward)
        {
            if (party == null) { return; }

            CombatParticipant newTargetCharacter = party.GetNextMember(targetCharacter, traverseForward);
            targetCharacter = newTargetCharacter;

            if (targetCharacterChanged != null)
            {
                targetCharacterChanged.Invoke(CombatParticipantType.Target, targetCharacter);
            }
        }

        public void UseItemOnTarget(CombatParticipant combatParticipant)
        {
            if (inventoryBoxState != InventoryBoxState.inCharacterTargeting) { return; }

            targetCharacter = combatParticipant;
            if (targetCharacterChanged != null)
            {
                targetCharacterChanged.Invoke(CombatParticipantType.Target, combatParticipant);
            }

            Choose(null);
        }

        private void DisplayCharacterInCooldownMessage(CombatParticipant character)
        {
            handleGlobalInput = false;
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText(string.Format(messageBusyInCooldown, character.GetCombatName()));
            dialogueBox.SetGlobalCallbacks(battleController);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }
        #endregion

        #region Interfaces
        public override void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return; }

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (inventoryBoxState == InventoryBoxState.inKnapsack)
                {
                    ClearChoiceSelections();
                    SetInventoryBoxState(InventoryBoxState.inCharacterSelection);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            base.HandleGlobalInput(playerInputType);
        }

        public override void HandleDialogueCallback(DialogueBox dialogueBox, string callbackMessage)
        {
            base.HandleDialogueCallback(dialogueBox, callbackMessage);

            if (callbackMessage == DIALOGUE_CALLBACK_ENABLE_INPUT)
            {
                selectedItemSlot = -1;
                if (targetCharacterChanged != null)
                {
                    targetCharacterChanged.Invoke(CombatParticipantType.Target, null);
                }

                if (selectedCharacter == null || selectedKnapsack == null)
                {
                    ReInitializeToCharacterSelection();
                }
                else
                {
                    if (selectedKnapsack.IsEmpty())
                    {
                        ReInitializeToCharacterSelection();
                    }
                    else
                    {
                        SetInventoryBoxState(InventoryBoxState.inKnapsack);
                    }
                }
            }
        }
        #endregion
    }
}
