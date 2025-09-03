using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Frankie.Utils.UI;

namespace Frankie.Speech.UI
{
    public class DialogueChoiceOption : UIChoiceButton
    {
        // State
        DialogueNode dialogueNode = null;

        // Cached References
        DialogueController dialogueController = null;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (dialogueController != null)
            {
                dialogueController.highlightedNodeChanged -= Highlight;
            }
        }

        public void Setup(DialogueController dialogueController, DialogueNode dialogueNode)
        {
            this.dialogueController = dialogueController;
            this.dialogueNode = dialogueNode;
            dialogueController.highlightedNodeChanged += Highlight;
        }

        public void Highlight(DialogueNode dialogueNode)
        {
            if (this.dialogueNode == dialogueNode)
            {
                selectionMarker.SetActive(true);
                if (itemHighlighted != null)
                {
                    itemHighlighted.Invoke();
                }
            }
            else
            {
                selectionMarker.SetActive(false);
            }
        }
    }
}