using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Frankie.Speech.UI
{
    public class DialogueChoiceOption : MonoBehaviour
    {
        // Tunables
        [SerializeField] protected GameObject selectionMarker = null;
        [Tooltip("Smallest values select first")] public int choiceOrder = 0;
        [SerializeField] TextMeshProUGUI textField = null;
        [SerializeField] Button button = null;

        // State
        DialogueNode dialogueNode = null;

        // Cached Reference
        DialogueController dialogueController = null;

        // Unity Events
        public UnityEvent itemHighlighted;

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
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

        public Button GetButton()
        {
            return button;
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

        public virtual void Highlight(bool enable)
        {
            selectionMarker.SetActive(enable);
            if (enable && itemHighlighted != null)
            {
                itemHighlighted.Invoke();
            }
        }
    }
}