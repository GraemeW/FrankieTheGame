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
        [SerializeField] GameObject inventoryItemFieldPrefab = null;

        // State
        CombatParticipant selectedCharacter = null;
        bool browsingInventory = false;
        List<DialogueChoiceOption> playerSelectChoiceOptions = new List<DialogueChoiceOption>();
        List<InventoryItemField> inventoryItemChoiceOptions = new List<InventoryItemField>();

        protected override void Start()
        {
            // Do Nothing (skip base implementation)
        }

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party)
        {
            SetGlobalCallbacks(standardPlayerInputCaller);
            int choiceIndex = 0;
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject characterFieldObject = Instantiate(optionPrefab, optionParent);
                DialogueChoiceOption dialogueChoiceOption = characterFieldObject.GetComponent<DialogueChoiceOption>();
                dialogueChoiceOption.SetChoiceOrder(choiceIndex);
                dialogueChoiceOption.SetText(character.GetCombatName());
                characterFieldObject.GetComponent<Button>().onClick.AddListener(delegate { Choose(character); });

                if (choiceIndex == 0) { Choose(character); browsingInventory = false; }
                choiceIndex++;

                playerSelectChoiceOptions.Add(dialogueChoiceOption);
            }
            SetUpChoiceOptions();
        }

        protected override void SetUpChoiceOptions()
        {
            if (browsingInventory)
            {
                choiceOptions = inventoryItemChoiceOptions.Cast<DialogueChoiceOption>().ToList();
            }
            else
            {
                choiceOptions = playerSelectChoiceOptions;
            }

            if (choiceOptions.Count > 0) { isChoiceAvailable = true; }
            else { isChoiceAvailable = false; }
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
                if (playerInputType == PlayerInputType.NavigateRight)
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
                    if (choiceIndex + 1 >= choiceOptions.Count) { choiceIndex = 0; }
                    else { choiceIndex++; choiceIndex++; }
                    validInput = true;
                }
                else if (playerInputType == PlayerInputType.NavigateUp)
                {
                    if (choiceIndex <= 0) { choiceIndex = choiceOptions.Count - 1; }
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

        private void Choose(CombatParticipant character)
        {
            if (character != selectedCharacter)
            {
                OnDialogueBoxModified(DialogueBoxModifiedType.itemSelected, true);

                selectedCharacter = character;
                selectedCharacterNameField.text = selectedCharacter.GetCombatName();

                GenerateKnapsack(character);
                browsingInventory = true;
                SetUpChoiceOptions();
            }
        }

        private void Choose(Inventory inventory, int inventorySlot)
        {

        }

        private void GenerateKnapsack(CombatParticipant character)
        {
            CleanUpOldKnapsack();
            Inventory inventory = character.GetComponent<Inventory>();
            for (int i =0; i < inventory.GetSize(); i++)
            {
                if (inventory.GetItemInSlot(i) == null) { continue; }
                if (i % 2 == 0)
                {
                    SetupInventoryItem(leftItemContainer, inventory, i);
                }
                else
                {
                    SetupInventoryItem(rightItemContainer, inventory, i);
                }
            }
        }

        private void SetupInventoryItem(Transform container, Inventory inventory, int slot)
        {
            GameObject inventoryItemFieldObject = Instantiate(inventoryItemFieldPrefab, container);
            InventoryItemField inventoryItemField = inventoryItemFieldObject.GetComponent<InventoryItemField>();
            inventoryItemField.SetChoiceOrder(slot);
            inventoryItemField.SetText(inventory.GetItemInSlot(slot).GetDisplayName());
            inventoryItemField.GetButton().onClick.AddListener(delegate { Choose(inventory, slot); });

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
