using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Frankie.Dialogue.UI
{
    public class DialogueChoiceOption : MonoBehaviour
    {
        // Tunables
        [SerializeField] GameObject selectionMarker = null;
        [Tooltip("Smallest values select first")] public int choiceOrder = 0;
        [SerializeField] TextMeshProUGUI textField = null;

        public void SetChoiceOrder(int choiceOrder)
        {
            this.choiceOrder = choiceOrder;
        }

        public void SetText(string text)
        {
            textField.text = text;
        }

        public void Highlight(bool enable)
        {
            selectionMarker.SetActive(enable);
        }

        public bool IsHighlighted()
        {
            return selectionMarker.activeSelf;
        }
    }
}