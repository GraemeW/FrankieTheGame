using Frankie.Combat;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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
        [SerializeField] GameObject inventoryItemFieldPrefab = null;
        [Header("Messages")]
        [Tooltip("Include {0} for character name")] [SerializeField] string messageBusyInCooldown = "{0} is busy twirling, twirling.";

        // State
        CombatParticipant selectedCharacter = null;
        bool browsingInventory = false;
        List<DialogueChoiceOption> playerSelectChoiceOptions = new List<DialogueChoiceOption>();
        List<InventoryItemField> inventoryItemChoiceOptions = new List<InventoryItemField>();
        BattleController battleController = null;

        protected override void Start()
        {
            // Do Nothing (skip base implementation)
        }

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party, bool inCombat = false)
        {
            if (inCombat) { battleController = standardPlayerInputCaller as BattleController; }

            SetGlobalCallbacks(standardPlayerInputCaller);
            int choiceIndex = 0;
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject characterFieldObject = Instantiate(optionPrefab, optionParent);
                DialogueChoiceOption dialogueChoiceOption = characterFieldObject.GetComponent<DialogueChoiceOption>();
                dialogueChoiceOption.SetChoiceOrder(choiceIndex);
                dialogueChoiceOption.SetText(character.GetCombatName());
                characterFieldObject.GetComponent<Button>().onClick.AddListener(delegate { ChooseCharacter(character); });

                if (choiceIndex == 0) { ChooseCharacter(character); browsingInventory = false; }
                choiceIndex++;

                playerSelectChoiceOptions.Add(dialogueChoiceOption);
            }
            SetUpChoiceOptions();
            ShowCursorOnAnyInteraction(PlayerInputType.Execute);
        }

        protected override void SetUpChoiceOptions()
        {
            choiceOptions.Clear();
            if (browsingInventory)
            {
                choiceOptions.AddRange(inventoryItemChoiceOptions.Cast<DialogueChoiceOption>().OrderBy(x => x.choiceOrder).ToList());
            }
            else
            {
                choiceOptions.AddRange(playerSelectChoiceOptions.OrderBy(x => x.choiceOrder).ToList());
            }

            if (choiceOptions.Count > 0) { isChoiceAvailable = true; }
            else { isChoiceAvailable = false; }
            MoveCursor(PlayerInputType.NavigateRight); // Initialize Highlight
        }
        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            if (!browsingInventory)
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

        private void ChooseCharacter(CombatParticipant character)
        {
            if (character != selectedCharacter)
            {
                OnDialogueBoxModified(DialogueBoxModifiedType.itemSelected, true);

                selectedCharacter = character;
                selectedCharacterNameField.text = selectedCharacter.GetCombatName();

                GenerateKnapsack(character);
            }
            browsingInventory = true;
            SetUpChoiceOptions();
        }

        private void ChooseItem(CombatParticipant character, Knapsack knapsack, int inventorySlot)
        {
            if (knapsack.GetItemInSlot(inventorySlot).GetType() == typeof(ActionItem))
            {
                // TODO:  Implement in world behavior

                if (battleController != null)
                {
                    if (battleController.SetSelectedCharacter(character)) // Check for cooldown
                    {
                        battleController.SetActiveBattleAction(new BattleAction(knapsack.GetItemInSlot(inventorySlot) as ActionItem));
                        battleController.SetBattleActionArmed(true);
                        battleController.SetBattleState(BattleState.Combat);
                        ClearDisableCallbacks(); // Prevent combat options from triggering -> proceed directly to target selection
                        Destroy(gameObject);
                    }
                    else
                    {
                        handleGlobalInput = false;
                        GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, transform.parent);
                        DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
                        dialogueBox.AddText(string.Format(messageBusyInCooldown, character.GetCombatName()));
                        dialogueBox.SetGlobalCallbacks(battleController);
                        dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
                    }
                }
            }
            else
            {
                // TODO:  Implement other item behavior
            }
        }

        private void GenerateKnapsack(CombatParticipant character)
        {
            CleanUpOldKnapsack();
            Knapsack knapsack = character.GetComponent<Knapsack>();
            for (int i =0; i < knapsack.GetSize(); i++)
            {
                if (knapsack.GetItemInSlot(i) == null) { continue; }
                if (i % 2 == 0)
                {
                    SetupInventoryItem(leftItemContainer, character, knapsack, i);
                }
                else
                {
                    SetupInventoryItem(rightItemContainer, character, knapsack, i);
                }
            }
        }

        private void SetupInventoryItem(Transform container, CombatParticipant character, Knapsack knapsack, int slot)
        {
            GameObject inventoryItemFieldObject = Instantiate(inventoryItemFieldPrefab, container);
            InventoryItemField inventoryItemField = inventoryItemFieldObject.GetComponent<InventoryItemField>();
            inventoryItemField.SetChoiceOrder(slot);
            inventoryItemField.SetText(knapsack.GetItemInSlot(slot).GetDisplayName());
            inventoryItemField.GetButton().onClick.AddListener(delegate { ChooseItem(character, knapsack, slot); });

            inventoryItemChoiceOptions.Add(inventoryItemField);
        }

        private void CleanUpOldKnapsack()
        {
            inventoryItemChoiceOptions.Clear();
            foreach (Transform child in leftItemContainer) { Destroy(child.gameObject); }
            foreach (Transform child in rightItemContainer) { Destroy(child.gameObject); }
        }

        public override void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return; }

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (browsingInventory)
                {
                    ClearChoiceSelections();
                    browsingInventory = false;
                    SetUpChoiceOptions();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            base.HandleGlobalInput(playerInputType);
        }
    }
}
