using Frankie.Combat;
using Frankie.Control;
using Frankie.Utils;
using Frankie.Utils.UI;
using Frankie.Speech.UI;
using Frankie.Combat.UI;
using Frankie.Stats;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
using System;

namespace Frankie.Inventory.UI
{
    public class InventoryBox : UIBox, IUIItemHandler
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] TextMeshProUGUI selectedCharacterNameField = null;
        [Header("Parents")]
        [SerializeField] protected Transform leftItemContainer = null;
        [SerializeField] protected Transform rightItemContainer = null;
        [Header("Prefabs")]
        [SerializeField] protected DialogueBox dialogueBoxPrefab = null;
        [SerializeField] protected DialogueOptionBox dialogueOptionBoxPrefab = null;
        [SerializeField] protected InventoryItemField inventoryItemFieldPrefab = null;
        [SerializeField] GameObject inventoryMoveBoxPrefab = null;
        [Header("Info/Messages")]
        [SerializeField] protected string optionText = "What do you want to do?";
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
        List<UIChoiceOption> playerSelectChoiceOptions = new List<UIChoiceOption>();
        protected List<InventoryItemField> inventoryItemChoiceOptions = new List<InventoryItemField>();
        protected CombatParticipant selectedCharacter = null;
        protected Knapsack selectedKnapsack = null;
        int selectedItemSlot = -1;
        IEnumerable<CombatParticipant> targetCharacters = null;

        // Cached References
        BattleController battleController = null;
        Party party = null;
        List<CharacterSlide> characterSlides = null;

        // Events
        public event Action<Enum> uiBoxStateChanged;
        public event Action<CombatParticipantType, IEnumerable<CombatParticipant>> targetCharacterChanged;

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
            controller = standardPlayerInputCaller;
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

            int choiceIndex = 0;
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject uiChoiceOptionObject = Instantiate(optionPrefab, optionParent);
                UIChoiceOption uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceOption>();
                uiChoiceOption.SetChoiceOrder(choiceIndex);
                uiChoiceOption.SetText(character.GetCombatName());
                uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(character); });
                uiChoiceOption.AddOnHighlightListener(delegate { SoftChooseCharacter(character); });

                if (choiceIndex == 0) { SoftChooseCharacter(character); }

                playerSelectChoiceOptions.Add(uiChoiceOption);
                choiceIndex++;
            }
            SetInventoryBoxState(InventoryBoxState.inCharacterSelection);
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
        }

        public void Setup(CombatParticipant character, List<CharacterSlide> characterSlides = null)
        {
            // Single party member instantiation for specific application
            this.characterSlides = characterSlides;
            SubscribeCharacterSlides(true);

            GameObject uiChoiceOptionObject = Instantiate(optionPrefab, optionParent);
            UIChoiceOption uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceOption>();
            uiChoiceOption.SetChoiceOrder(0);
            uiChoiceOption.SetText(character.GetCombatName());
            uiChoiceOption.AddOnClickListener(delegate { ChooseCharacter(character); });
            playerSelectChoiceOptions.Add(uiChoiceOption);
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
                selectedItemSlot = -1;
                if (inventoryBoxState == InventoryBoxState.inKnapsack)
                {
                    choiceOptions.AddRange(inventoryItemChoiceOptions.Cast<UIChoiceOption>().OrderBy(x => x.choiceOrder).ToList());
                }
                else if (inventoryBoxState == InventoryBoxState.inCharacterSelection)
                {
                    choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList());
                }

                SetChoiceAvailable(choiceOptions.Count > 0);
            }
            else
            {
                SetChoiceAvailable(true) ; // avoid short circuit on user control for other states
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
            foreach (UIChoiceOption dialogueChoiceOption in playerSelectChoiceOptions)
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

            uiBoxStateChanged?.Invoke(inventoryBoxState);
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
                    if (!GetNextTarget(true))
                    {
                        SetInventoryBoxState(InventoryBoxState.inKnapsack);
                    }
                }
                else if (playerInputType == PlayerInputType.NavigateLeft || playerInputType == PlayerInputType.NavigateUp)
                {
                    if (!GetNextTarget(false))
                    {
                        SetInventoryBoxState(InventoryBoxState.inKnapsack);
                    }
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
                InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(selectedItemSlot);
                string senderName = selectedCharacter.GetCombatName();
                string itemName = inventoryItem.GetDisplayName();
                string targetCharacterNames = string.Join(", ", targetCharacters.Select(x => x.GetCombatName()).ToList());

                if (selectedKnapsack.UseItemInSlot(selectedItemSlot, targetCharacters))
                {
                    DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);

                    dialogueBox.AddText(string.Format(messageUseItemInWorld, senderName, itemName, targetCharacterNames));
                    PassControl(dialogueBox);

                    return true;
                }
                else
                {
                    return false; 
                }
            }
        }

        protected virtual void ChooseCharacter(CombatParticipant character, bool initializeCursor = true)
        {
            UpdateKnapsackView(character);
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

        protected virtual void SoftChooseCharacter(CombatParticipant character)
        {
            ChooseCharacter(character, false);
            SetInventoryBoxState(InventoryBoxState.inCharacterSelection);
        }

        protected void UpdateKnapsackView(CombatParticipant character)
        {
            if (character == null)
            {
                selectedKnapsack = null;
                SetInventoryBoxState(InventoryBoxState.inCharacterSelection);
                return;
            }

            if (character != selectedCharacter)
            {
                OnUIBoxModified(UIBoxModifiedType.itemSelected, true);

                selectedCharacter = character;
                selectedCharacterNameField.text = selectedCharacter.GetCombatName();
                RefreshKnapsackContents();
            }
        }

        protected virtual void ChooseItem(int inventorySlot)
        {
            List<ChoiceActionPair> choiceActionPairs = GetChoiceActionPairs(inventorySlot);
            if (choiceActionPairs.Count == 0) { return; }
            else if (choiceActionPairs.Count == 1)
            {
                choiceActionPairs[0].action?.Invoke();
                return;
            }

            SetInventoryBoxState(InventoryBoxState.inItemDetail);
            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            dialogueOptionBox.Setup(optionText);
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            PassControl(dialogueOptionBox);
            dialogueOptionBox.ClearDisableCallbacksOnChoose(true);
        }
        #endregion

        #region KnapsackBehaviour
        protected virtual void RefreshKnapsackContents()
        {
            if (!CleanUpOldKnapsack()) { return; } // Error handling for message received during deconstruction

            SetSelectedKnapsack(selectedCharacter.GetComponent<Knapsack>());
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
                ChoiceActionPair useActionPair = new ChoiceActionPair(optionUse, () => Use(inventorySlot));
                choiceActionPairs.Add(useActionPair);
            }
            // Inspect
            ChoiceActionPair inspectActionPair = new ChoiceActionPair(optionInspect, () => Inspect(inventorySlot));
            choiceActionPairs.Add(inspectActionPair);

            // Move
            ChoiceActionPair moveActionPair = new ChoiceActionPair(optionMove, () => Move(inventorySlot));
            choiceActionPairs.Add(moveActionPair);

            // Drop
            if (inventoryItem.IsDroppable())
            {
                ChoiceActionPair dropActionPair = new ChoiceActionPair(optionDrop, () => Drop(inventorySlot));
                choiceActionPairs.Add(dropActionPair);
            }

            return choiceActionPairs;
        }

        public virtual InventoryItemField SetupItem(InventoryItemField inventoryItemFieldPrefab, Transform container, int selector)
        {
            CheckItemExists(selectedKnapsack, selector, out bool itemExists, out string itemName);
            return SpawnInventoryItemField(itemExists, itemName, inventoryItemFieldPrefab, container, selector);
        }

        private InventoryItemField SpawnInventoryItemField(bool itemExists, string itemName, InventoryItemField inventoryItemFieldPrefab, Transform container, int selector)
        {
            InventoryItemField inventoryItemField = Instantiate(inventoryItemFieldPrefab, container);
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

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(selectedKnapsack.GetItemInSlot(inventorySlot).GetDescription());
            PassControl(dialogueBox);
        }

        private void Move(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }

            GameObject inventoryMoveBoxObject = Instantiate(inventoryMoveBoxPrefab, transform.parent);
            InventoryMoveBox inventoryMoveBox = inventoryMoveBoxObject.GetComponent<InventoryMoveBox>();
            inventoryMoveBox.Setup(controller, party, selectedKnapsack, inventorySlot, characterSlides);
            canvasGroup.alpha = 0.0f;
            PassControl(this, new Action[] { () => EnableInput(true), () => SetVisible(true) }, inventoryMoveBox, controller);

            SetInventoryBoxState(InventoryBoxState.inItemMoving);
        }

        private void Drop(int inventorySlot)
        {
            if (selectedKnapsack == null) { return; }
            if (!selectedKnapsack.HasItemInSlot(inventorySlot)) { return; }

            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);

            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            ChoiceActionPair confirmDrop = new ChoiceActionPair(confirmChoiceAffirmative, () => ExecuteDrop(inventorySlot));
            choiceActionPairs.Add(confirmDrop);
            ChoiceActionPair rejectDrop = new ChoiceActionPair(confirmChoiceNegative, () => ExecuteDrop(-1));
            choiceActionPairs.Add(rejectDrop);

            dialogueOptionBox.Setup(string.Format(messageDropItem, selectedKnapsack.GetItemInSlot(inventorySlot).GetDisplayName()));
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            PassControl(dialogueOptionBox);
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
                    battleController.SetActiveBattleAction(selectedKnapsack.GetItemInSlot(inventorySlot) as ActionItem);
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
                selectedItemSlot = inventorySlot;
                handleGlobalInput = true;
                targetCharacters = new CombatParticipant[] { null };
                if (GetNextTarget(true))
                {
                    SetInventoryBoxState(InventoryBoxState.inCharacterTargeting);
                }
                else
                {
                    SetInventoryBoxState(InventoryBoxState.inKnapsack);
                }
            }
        }

        private bool GetNextTarget(bool? traverseForward)
        {
            ActionItem actionItem = selectedKnapsack.GetItemInSlot(selectedItemSlot) as ActionItem;
            if (actionItem == null) { return false; }

            targetCharacters = actionItem.GetTargets(traverseForward, targetCharacters, party.GetParty(), null);
            if (targetCharacters == null || targetCharacters.Count() == 0)
            {
                return false;
            }

            targetCharacterChanged?.Invoke(CombatParticipantType.Target, targetCharacters);
            return true;
        }

        public void UseItemOnTarget(CombatParticipant combatParticipant)
        {
            if (inventoryBoxState != InventoryBoxState.inCharacterTargeting) { return; }

            targetCharacters = new[] { combatParticipant };
            if (!GetNextTarget(null)) { SetInventoryBoxState(InventoryBoxState.inKnapsack); return; }

            targetCharacterChanged?.Invoke(CombatParticipantType.Target, new[] { combatParticipant });
            Choose(null);
        }

        private void DisplayCharacterInCooldownMessage(CombatParticipant character)
        {
            handleGlobalInput = false;
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(string.Format(messageBusyInCooldown, character.GetCombatName()));
            PassControl(dialogueBox);
        }
        #endregion

        #region Interfaces
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (inventoryBoxState == InventoryBoxState.inKnapsack)
                {
                    ClearChoiceSelections();
                    SetInventoryBoxState(InventoryBoxState.inCharacterSelection);
                    return true;
                }
            }
            return base.HandleGlobalInput(playerInputType);
        }

        protected override void EnableInput(bool enable)
        {
            if (!enable) { handleGlobalInput = false; return; }

            selectedItemSlot = -1;
            targetCharacterChanged?.Invoke(CombatParticipantType.Target, null);

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
            handleGlobalInput = true;
        }
        #endregion
    }
}
