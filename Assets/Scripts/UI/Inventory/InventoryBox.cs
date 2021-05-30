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

namespace Frankie.Inventory.UI
{
    public class InventoryBox : DialogueOptionBox
    {
        // Tunables
        [Header("Data Links")]
        [SerializeField] TextMeshProUGUI selectedCharacterNameField;
        [Header("Parents")]
        [SerializeField] Transform leftItemContainer = null;
        [SerializeField] Transform rightItemContainer = null;
        [Header("Prefabs")]
        [SerializeField] GameObject dialogueBoxPrefab = null;
        [SerializeField] GameObject dialogueOptionBoxPrefab = null;
        [SerializeField] GameObject inventoryItemFieldPrefab = null;
        [Header("Info/Messages")]
        [SerializeField] string optionInspect = "Inspect";
        [SerializeField] string optionEquip = "Equip";
        [SerializeField] string optionUse = "Use";
        [Tooltip("Include {0} for character name")] [SerializeField] string messageBusyInCooldown = "{0} is busy twirling, twirling.";

        // State
        InventoryBoxState inventoryBoxState = InventoryBoxState.inCharacterSelection;
        CombatParticipant selectedCharacter = null;
        Knapsack selectedKnapsack = null;
        List<DialogueChoiceOption> playerSelectChoiceOptions = new List<DialogueChoiceOption>();
        List<InventoryItemField> inventoryItemChoiceOptions = new List<InventoryItemField>();

        // Cached References
        IStandardPlayerInputCaller standardPlayerInputCaller = null;
        BattleController battleController = null;
        List<CharacterSlide> characterSlides = null;

        // Events
        public event Action<InventoryBoxState> inventoryBoxStateChanged;
        public event Action<CombatParticipantType, CombatParticipant> selectedCharacterTargetChanged;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            // Do Nothing (skip base implementation)
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeCharacterSlides(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SubscribeCharacterSlides(false);
        }

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party, List<CharacterSlide> characterSlides = null)
        {
            this.standardPlayerInputCaller = standardPlayerInputCaller;
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

                if (choiceIndex == 0) { ChooseCharacter(character); SetInventoryBoxState(InventoryBoxState.inCharacterSelection); }
                choiceIndex++;

                playerSelectChoiceOptions.Add(dialogueChoiceOption);
            }
            SetUpChoiceOptions();
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
        }

        private void SubscribeCharacterSlides(bool enable)
        {
            if (characterSlides != null)
            {
                foreach (CharacterSlide characterSlide in characterSlides)
                {
                    if (enable)
                    {
                        selectedCharacterTargetChanged += characterSlide.HighlightSlide;
                    }
                    else
                    {
                        selectedCharacterTargetChanged -= characterSlide.HighlightSlide;
                    }
                }
            }
        }

        protected override void SetUpChoiceOptions()
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
            MoveCursor(PlayerInputType.NavigateRight); // Initialize Highlight
        }
        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            if (inventoryBoxState != InventoryBoxState.inKnapsack)
            {
                return base.MoveCursor(playerInputType);
            }
            else
            {
                // Support for 2-D movement across the inventory items
                if (highlightedChoiceOption == null) { return false; }

                bool validInput = false;
                int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
                if (choiceOptions.Count == 1)
                {
                    choiceIndex = 0;
                    validInput = true;
                }
                else if (playerInputType == PlayerInputType.NavigateRight)
                {
                    if (choiceIndex + 1 >= choiceOptions.Count) { choiceIndex = 0; }
                    else { choiceIndex++; }
                    validInput = true;
                }
                else if (playerInputType == PlayerInputType.NavigateLeft)
                {
                    if (choiceIndex <= 0) { choiceIndex = choiceOptions.Count - 1; }
                    else { choiceIndex--; }
                    validInput = true;
                }
                else if (playerInputType == PlayerInputType.NavigateDown)
                {
                    if (choiceIndex + 1 >= choiceOptions.Count || choiceOptions.Count == 2) { choiceIndex = 0; }
                    else { choiceIndex++; choiceIndex++; }
                    validInput = true;
                }
                else if (playerInputType == PlayerInputType.NavigateUp)
                {
                    if (choiceIndex <= 0 || choiceOptions.Count == 2) { choiceIndex = choiceOptions.Count - 1; }
                    else { choiceIndex--; choiceIndex--; }
                    validInput = true;
                }

                if (validInput)
                {
                    ClearChoiceSelections();
                    highlightedChoiceOption = choiceOptions[choiceIndex];
                    choiceOptions[choiceIndex].Highlight(true);
                    return true;
                }
                return false;
            }
        }

        private void SetInventoryBoxState(InventoryBoxState inventoryBoxState)
        {
            this.inventoryBoxState = inventoryBoxState;
            if (inventoryBoxStateChanged != null)
            {
                inventoryBoxStateChanged.Invoke(inventoryBoxState);
            }
        }

        private void ChooseCharacter(CombatParticipant character)
        {
            if (character != selectedCharacter)
            {
                OnDialogueBoxModified(DialogueBoxModifiedType.itemSelected, true);

                selectedCharacter = character;
                selectedCharacterNameField.text = selectedCharacter.GetCombatName();
                GenerateKnapsack();
            }
            SetInventoryBoxState(InventoryBoxState.inKnapsack);
            SetUpChoiceOptions();
        }

        private void ChooseItem(int inventorySlot)
        {
            handleGlobalInput = false;
            SetInventoryBoxState(InventoryBoxState.inItemDetail);
            GameObject dialogueOptionBoxObject = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            DialogueOptionBox dialogueOptionBox = dialogueOptionBoxObject.GetComponent<DialogueOptionBox>();
            dialogueOptionBox.SetupSimpleChoices(GetChoiceActionPairs(inventorySlot));
            dialogueOptionBox.SetGlobalCallbacks(standardPlayerInputCaller);
            // Note:  Do not re-enable input control on callback
            // Control is setup and then passed back via ChoiceActionPair action menu
        }

        private List<ChoiceActionPair> GetChoiceActionPairs(int inventorySlot)
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            // Use
            if (selectedKnapsack.GetItemInSlot(inventorySlot).GetType() == typeof(ActionItem))
            {
                ChoiceActionPair useActionPair = new ChoiceActionPair(optionUse, Use, inventorySlot);
                choiceActionPairs.Add(useActionPair);
            }
            // Equip
            if (battleController == null)
            {
                if (selectedKnapsack.GetItemInSlot(inventorySlot).GetType() == typeof(EquipableItem))
                {
                    ChoiceActionPair equipActionPair = new ChoiceActionPair(optionEquip, Equip, inventorySlot);
                    choiceActionPairs.Add(equipActionPair);
                }
            }
            // Inspect
            ChoiceActionPair inspectActionPair = new ChoiceActionPair(optionInspect, Inspect, inventorySlot);
            choiceActionPairs.Add(inspectActionPair);

            return choiceActionPairs;
        }

        private void GenerateKnapsack()
        {
            CleanUpOldKnapsack();
            selectedKnapsack = selectedCharacter.GetComponent<Knapsack>();
            for (int i =0; i < selectedKnapsack.GetSize(); i++)
            {
                if (selectedKnapsack.GetItemInSlot(i) == null) { continue; }
                if (i % 2 == 0)
                {
                    SetupInventoryItem(leftItemContainer, i);
                }
                else
                {
                    SetupInventoryItem(rightItemContainer, i);
                }
            }
            
        }

        private void SetupInventoryItem(Transform container, int slot)
        {
            GameObject inventoryItemFieldObject = Instantiate(inventoryItemFieldPrefab, container);
            InventoryItemField inventoryItemField = inventoryItemFieldObject.GetComponent<InventoryItemField>();
            inventoryItemField.SetChoiceOrder(slot);
            inventoryItemField.SetText(selectedKnapsack.GetItemInSlot(slot).GetDisplayName());
            inventoryItemField.SetupButtonAction(this, ChooseItem, slot);

            inventoryItemChoiceOptions.Add(inventoryItemField);
        }

        private void CleanUpOldKnapsack()
        {
            inventoryItemChoiceOptions.Clear();
            foreach (Transform child in leftItemContainer) { Destroy(child.gameObject); }
            foreach (Transform child in rightItemContainer) { Destroy(child.gameObject); }
            selectedKnapsack = null;
        }

        private void Inspect(int inventorySlot)
        {
            handleGlobalInput = false;
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText(selectedKnapsack.GetItemInSlot(inventorySlot).GetDescription());
            dialogueBox.SetGlobalCallbacks(standardPlayerInputCaller);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        private void Equip(int inventorySlot)
        {
            // TODO:  Implement equipment screen
            handleGlobalInput = false;
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText("This is where we would go to the equipment screen, if we had implemented it");
            dialogueBox.SetGlobalCallbacks(standardPlayerInputCaller);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
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
                // TODO:  Implement world behavior
                handleGlobalInput = false;
                GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
                DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
                dialogueBox.AddText("This is where we would use an item in the world, if we had implemented it");
                dialogueBox.SetGlobalCallbacks(standardPlayerInputCaller);
                dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
            }
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


        public override void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return; }

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (inventoryBoxState == InventoryBoxState.inKnapsack)
                {
                    ClearChoiceSelections();
                    inventoryBoxState = InventoryBoxState.inCharacterSelection;
                    SetUpChoiceOptions();
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
            if (callbackMessage == DIALOGUE_CALLBACK_ENABLE_INPUT)
            {
                handleGlobalInput = true;
                SetInventoryBoxState(InventoryBoxState.inKnapsack);
            }
            else if (callbackMessage == DIALOGUE_CALLBACK_DESTROY)
            {
                Destroy(gameObject);
            }
        }
    }
}
