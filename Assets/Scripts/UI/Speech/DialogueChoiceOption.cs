using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

namespace Frankie.Speech.UI
{
    public class DialogueChoiceOption : MonoBehaviour
    {
        // Tunables
        [SerializeField] protected GameObject selectionMarker = null;
        [Tooltip("Smallest values select first")] public int choiceOrder = 0;
        [SerializeField] TextMeshProUGUI textField = null;
        [SerializeField] protected Button button = null;
        [SerializeField] Color validChoiceColor = Color.white;
        [SerializeField] Color invalidChoiceColor = Color.gray;

        // State
        DialogueNode dialogueNode = null;
        List<UnityAction> onHighlightExtraListeners = new List<UnityAction>();

        // Cached Reference
        DialogueController dialogueController = null;

        // Unity Events
        public UnityEvent itemHighlighted;

        private void OnEnable()
        {
            if (onHighlightExtraListeners != null && onHighlightExtraListeners.Count > 0)
            {
                foreach (UnityAction unityAction in onHighlightExtraListeners)
                {
                    itemHighlighted.AddListener(unityAction);
                }
            }
        }

        private void OnDisable()
        {
            if (onHighlightExtraListeners != null && onHighlightExtraListeners.Count > 0)
            {
                foreach (UnityAction unityAction in onHighlightExtraListeners)
                {
                    itemHighlighted.RemoveListener(unityAction);
                }
            }
        }

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
            if (dialogueController != null)
            {
                dialogueController.highlightedNodeChanged -= Highlight;
            }
            itemHighlighted.RemoveAllListeners();
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

        public void SetValidColor(bool enable)
        {
            if (enable)
            {
                textField.color = validChoiceColor;
            }
            else
            {
                textField.color = invalidChoiceColor;
            }
        }

        public void AddOnHighlightListener(UnityAction unityAction)
        {
            onHighlightExtraListeners.Add(unityAction);

            if (gameObject.activeSelf)
            {
                itemHighlighted.AddListener(unityAction);
            }
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