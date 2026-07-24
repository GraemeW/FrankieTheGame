using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Control;
using Frankie.Utils;

namespace Frankie.Speech.UI
{
    public class DialogueOptionBox : DialogueBox
    {
        protected override bool UsesNodeBasedDialogueFlow() => false;

        protected override void AwakeTriggered()
        {
            base.AwakeTriggered();
            clearVolatileOptionsOnEnable = false;
        }

        public override void Setup(string optionText)
        {
            base.Setup(optionText);

            if (dialogueController == null) { return; }
            List<ChoiceActionPair> choiceActionPairs = dialogueController.GetSimpleChoices();
            OverrideChoiceOptions(choiceActionPairs);

            int maxChoiceLength = choiceActionPairs.Aggregate(0, (current, choiceActionPair) => Mathf.Max(current, choiceActionPair.choice.Length));
            ConfigureChoiceLayout(choiceActionPairs.Count, maxChoiceLength);

            ShowCursorOnAnyInteraction(ControllerInputType.NavigateDown);
        }
    }
}
