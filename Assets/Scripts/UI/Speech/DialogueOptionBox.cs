using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Control;
using System;

namespace Frankie.Speech.UI
{
    public class DialogueOptionBox : DialogueBox
    {
        [SerializeField] bool clearVolatileOptionsOnEnable = true;

        // State
        protected bool isChoiceAvailable = false;
        protected List<DialogueChoiceOption> choiceOptions = new List<DialogueChoiceOption>();
        protected DialogueChoiceOption highlightedChoiceOption = null;

        protected override void OnEnable()
        {
            base.OnEnable();
            SetUpChoiceOptions();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearChoiceSelections();
        }

        public override void Setup(string optionText)
        {
            base.Setup(optionText);

            if (dialogueController == null) { return; }
            SetupSimpleChoices(dialogueController.GetSimpleChoices());
        }

        protected virtual void SetUpChoiceOptions()
        {
            if (clearVolatileOptionsOnEnable) { choiceOptions.Clear(); }
            choiceOptions.AddRange(optionParent.gameObject.GetComponentsInChildren<DialogueChoiceOption>().OrderBy(x => x.choiceOrder).ToList());

            if (choiceOptions.Count > 0) { isChoiceAvailable = true; }
            else { isChoiceAvailable = false; }
        }

        public void SetupSimpleChoices(List<ChoiceActionPair> choiceActionPairs)
        {
            choiceOptions.Clear();
            foreach (ChoiceActionPair choiceActionPair in choiceActionPairs)
            {
                if (choiceActionPair.choiceActionPairType == ChoiceActionPairType.SimpleString)
                {
                    AddChoiceOption(choiceActionPair.choice, choiceActionPair.simpleStringAction, choiceActionPair.stringActionParameter);
                }
                else if (choiceActionPair.choiceActionPairType == ChoiceActionPairType.SimpleInt)
                {
                    AddChoiceOption(choiceActionPair.choice, choiceActionPair.simpleIntAction, choiceActionPair.intActionParameter);
                }
                else if (choiceActionPair.choiceActionPairType == ChoiceActionPairType.Simple)
                {
                    AddChoiceOption(choiceActionPair.choice, choiceActionPair.simpleAction);
                }
            }
            isChoiceAvailable = true;
        }

        private void AddChoiceOption(string choiceText, Action functionCall)
        {
            DialogueChoiceOption dialogueChoiceOption = AddChoiceOptionTemplate(choiceText);
            dialogueChoiceOption.GetButton().onClick.AddListener(delegate { functionCall.Invoke();  Destroy(gameObject); });
        }

        private void AddChoiceOption(string choiceText, Action<string> functionCall, string functionParameter)
        {
            DialogueChoiceOption dialogueChoiceOption = AddChoiceOptionTemplate(choiceText);
            dialogueChoiceOption.GetButton().onClick.AddListener(delegate { functionCall.Invoke(functionParameter); Destroy(gameObject); });
        }

        private void AddChoiceOption(string choiceText, Action<int> functionCall, int functionParameter)
        {
            DialogueChoiceOption dialogueChoiceOption = AddChoiceOptionTemplate(choiceText);
            dialogueChoiceOption.GetButton().onClick.AddListener(delegate { functionCall.Invoke(functionParameter); Destroy(gameObject); });
        }

        private DialogueChoiceOption AddChoiceOptionTemplate(string choiceText)
        {
            GameObject choiceObject = Instantiate(optionPrefab, optionParent);
            DialogueChoiceOption dialogueChoiceOption = choiceObject.GetComponent<DialogueChoiceOption>();
            dialogueChoiceOption.SetChoiceOrder(choiceOptions.Count + 1);
            dialogueChoiceOption.SetText(choiceText);
            choiceOptions.Add(dialogueChoiceOption);
            return dialogueChoiceOption;
        }

        // Abstract Method Implementation
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (base.HandleGlobalInput(playerInputType)) { return true; }

            if (!IsChoiceAvailable()) { return false; } // Childed objects can still accept input on no choices available
            if (ShowCursorOnAnyInteraction(playerInputType)) { return true; }
            if (PrepareChooseAction(playerInputType)) { return true; }
            if (MoveCursor(playerInputType)) { return true; }

            return false;
        }

        protected override bool ShowCursorOnAnyInteraction(PlayerInputType playerInputType)
        {
            if (choiceOptions.Count == 0) { return false; }

            if (highlightedChoiceOption == null && playerInputType != PlayerInputType.DefaultNone)
            {
                highlightedChoiceOption = choiceOptions[0];
                highlightedChoiceOption.Highlight(true);
                return true;
            }
            return false;
        }

        protected override bool IsChoiceAvailable()
        {
            return isChoiceAvailable;
        }

        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            if (highlightedChoiceOption == null) { return false; }

            int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
            bool validInput = base.MoveCursor(playerInputType, ref choiceIndex, choiceOptions.Count);

            if (validInput)
            {
                ClearChoiceSelections();
                highlightedChoiceOption = choiceOptions[choiceIndex];
                choiceOptions[choiceIndex].Highlight(true);
                return true;
            }
            return false;
        }

        protected bool MoveCursor2D(PlayerInputType playerInputType, ref int choiceIndex)
        {
            return base.MoveCursor2D(playerInputType, ref choiceIndex, choiceOptions.Count);
        }

        protected override bool PrepareChooseAction(PlayerInputType playerInputType)
        {
            // Choose(null) since not passing a nodeID, not a standard dialogue -- irrelevant in context of override
            if (playerInputType == PlayerInputType.Execute)
            {
                return Choose(null);
            }
            return false;
        }

        protected override bool Choose(string nodeID)
        {
            if (highlightedChoiceOption != null)
            {
                highlightedChoiceOption.GetButton().onClick.Invoke();
                return true;
            }
            return false;
        }

        protected virtual void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (DialogueChoiceOption choiceOption in choiceOptions)
            {
                choiceOption.Highlight(false);
            }
        }
    }
}