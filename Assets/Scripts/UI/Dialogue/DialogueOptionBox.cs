using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

namespace Frankie.Dialogue.UI
{
    public class DialogueOptionBox : DialogueBox
    {
        // State
        bool isChoiceAvailable = false;
        public List<DialogueChoiceOption> choiceOptions = new List<DialogueChoiceOption>();

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

        private void SetUpChoiceOptions()
        {
            choiceOptions.Clear();
            choiceOptions.AddRange(optionParent.gameObject.GetComponentsInChildren<DialogueChoiceOption>().OrderBy(x => x.choiceOrder).ToList());

            if (choiceOptions.Count > 0) { isChoiceAvailable = true; }
            else { isChoiceAvailable = false; }
        }

        // TODO:  Implement new unity input system

        protected override bool ShowCursorOnAnyInteraction()
        {
            if (highlightedChoiceOption == null && (Input.GetKeyDown(choiceInteractKey) ||
                Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S)))
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

        protected override bool MoveCursor()
        {
            if (highlightedChoiceOption == null) { return false; }

            bool validInput = false;
            int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.S))
            {
                if (choiceIndex + 1 >= choiceOptions.Count) { choiceIndex = 0; }
                else { choiceIndex++; }
                validInput = true;
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A))
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

        private void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (DialogueChoiceOption choiceOption in choiceOptions)
            {
                choiceOption.Highlight(false);
            }
        }
    }
}