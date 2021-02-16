using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Frankie.Speech.UI
{
    public class DialogueChoiceOption : MonoBehaviour
    {
        // Tunables
        [SerializeField] GameObject selectionMarker = null;
        [Tooltip("Smallest values select first")] public int choiceOrder = 0;
        [SerializeField] TextMeshProUGUI textField = null;

        // State
        DialogueNode dialogueNode = null;

        // Cached Reference
        DialogueController dialogueController = null;

        private void OnDestroy()
        {
            GetComponent<Button>().onClick.RemoveAllListeners();
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

        public void SetChoiceOrder(int choiceOrder)
        {
            this.choiceOrder = choiceOrder;
        }

        public void SetText(string text)
        {
            textField.text = text;
        }

        public void Highlight(DialogueNode dialogueNode)
        {
            if (this.dialogueNode == dialogueNode)
            {
                selectionMarker.SetActive(true);
            }
            else
            {
                selectionMarker.SetActive(false);
            }
        }

        public void Highlight(bool enable)
        {
            selectionMarker.SetActive(enable);
        }
    }
}