using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Frankie.Control;
using Frankie.Core;
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

        protected override void Start()
        {
            SetupSimpleChoices();
        }

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

        protected virtual void SetUpChoiceOptions()
        {
            if (clearVolatileOptionsOnEnable) { choiceOptions.Clear(); }
            choiceOptions.AddRange(optionParent.gameObject.GetComponentsInChildren<DialogueChoiceOption>().OrderBy(x => x.choiceOrder).ToList());

            if (choiceOptions.Count > 0) { isChoiceAvailable = true; }
            else { isChoiceAvailable = false; }
        }

        private void SetupSimpleChoices()
        {
            if (dialogueController != null && dialogueController.IsSimpleMessage())
            {
                AddText(dialogueController.GetSimpleMessage());

                choiceOptions.Clear();
                foreach (ChoiceActionPair choiceActionPair in dialogueController.GetSimpleChoices())
                {
                    if (choiceActionPair.isComplexAction)
                    {
                        AddChoiceOption(choiceActionPair.choice, choiceActionPair.complexAction, choiceActionPair.complexActionParameter);
                    }
                    else if (!choiceActionPair.isComplexAction)
                    {
                        AddChoiceOption(choiceActionPair.choice, choiceActionPair.action);
                    }
                }
                isChoiceAvailable = true;
            }
        }

        private void AddChoiceOption(string choiceText, Action functionCall)
        {
            GameObject choiceObject = Instantiate(optionPrefab, optionParent);
            DialogueChoiceOption dialogueChoiceOption = choiceObject.GetComponent<DialogueChoiceOption>();
            dialogueChoiceOption.SetChoiceOrder(choiceOptions.Count + 1);
            dialogueChoiceOption.SetText(choiceText);
            choiceOptions.Add(dialogueChoiceOption);
            dialogueChoiceOption.GetButton().onClick.AddListener(delegate { functionCall.Invoke();  Destroy(gameObject); });
        }

        private void AddChoiceOption(string choiceText, Action<string> functionCall, string functionParameter)
        {
            GameObject choiceObject = Instantiate(optionPrefab, optionParent);
            DialogueChoiceOption dialogueChoiceOption = choiceObject.GetComponent<DialogueChoiceOption>();
            dialogueChoiceOption.SetChoiceOrder(choiceOptions.Count + 1);
            dialogueChoiceOption.SetText(choiceText);
            choiceOptions.Add(dialogueChoiceOption);
            dialogueChoiceOption.GetButton().onClick.AddListener(delegate { functionCall.Invoke(functionParameter); Destroy(gameObject); });
        }

        public override void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return; }

            if (!IsChoiceAvailable()) { return; }
            if (ShowCursorOnAnyInteraction(playerInputType)) { return; }
            if (PrepareChooseAction(playerInputType)) { return; }
            if (MoveCursor(playerInputType)) { return; }
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

            bool validInput = false;
            int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
            if (playerInputType == PlayerInputType.NavigateRight || playerInputType == PlayerInputType.NavigateDown)
            {
                if (choiceIndex + 1 >= choiceOptions.Count) { choiceIndex = 0; }
                else { choiceIndex++; }
                validInput = true;
            }
            else if (playerInputType == PlayerInputType.NavigateUp || playerInputType == PlayerInputType.NavigateLeft)
            {
                if (choiceIndex <= 0) { choiceIndex = choiceOptions.Count - 1; }
                else { choiceIndex--; }
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

        protected override bool PrepareChooseAction(PlayerInputType playerInputType)
        {
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

        protected void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (DialogueChoiceOption choiceOption in choiceOptions)
            {
                choiceOption.Highlight(false);
            }
        }
    }
}