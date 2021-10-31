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
        public override void Setup(string optionText)
        {
            base.Setup(optionText);

            if (dialogueController == null) { return; }
            OverrideChoiceOptions(dialogueController.GetSimpleChoices());
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