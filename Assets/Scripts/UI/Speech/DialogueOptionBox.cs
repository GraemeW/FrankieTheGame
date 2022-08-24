using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Control;
using Frankie.Utils;

namespace Frankie.Speech.UI
{
    public class DialogueOptionBox : DialogueBox
    {
        public override void Setup(string optionText)
        {
            base.Setup(optionText);

            if (dialogueController == null) { return; }
            List<ChoiceActionPair> choiceActionPairs = dialogueController.GetSimpleChoices();
            OverrideChoiceOptions(choiceActionPairs);

            int maxChoiceLength = 0;
            foreach (ChoiceActionPair choiceActionPair in choiceActionPairs)
            {
                maxChoiceLength = Mathf.Max(maxChoiceLength, choiceActionPair.choice.Length);
            }
            ConfigureChoiceLayout(choiceActionPairs.Count, maxChoiceLength);
        }

        // Pass through implementations
        protected override bool Choose(string nodeID)
        {
            return StandardChoose(nodeID);
        }

        protected override bool PrepareChooseAction(PlayerInputType playerInputType)
        {
            return StandardPrepareChooseAction(playerInputType);
        }
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            return StandardHandleGlobalInput(playerInputType);
        }
    }
}