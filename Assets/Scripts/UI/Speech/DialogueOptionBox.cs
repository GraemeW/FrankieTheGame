using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LowDefMustard.Control;
using LowDefMustard.Utils;

namespace Frankie.Speech.UI
{
    public class DialogueOptionBox : DialogueBox
    {
        protected override bool UsesNodeBasedDialogueFlow() => false;

        protected override void AwakeTriggered()
        {
            base.AwakeTriggered();
            clearChoiceOptionsOnEnable = false; // Simple options are set once (i.e. not re-created per dialogue node)
        }

        public override void Setup(string text)
        {
            base.Setup(text);

            if (dialogueController == null) { return; }
            List<ChoiceActionPair> choiceActionPairs = dialogueController.GetSimpleChoices();
            OverrideChoiceOptions(choiceActionPairs);

            int maxChoiceLength = choiceActionPairs.Aggregate(0, (current, choiceActionPair) => Mathf.Max(current, choiceActionPair.choice.Length));
            ConfigureChoiceLayout(choiceActionPairs.Count, maxChoiceLength);

            ShowCursorOnAnyInteraction(ControllerInputType.NavigateDown);
        }
    }
}
