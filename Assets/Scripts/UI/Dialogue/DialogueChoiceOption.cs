using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Dialogue.UI
{
    public class DialogueChoiceOption : MonoBehaviour
    {
        // Tunables
        [SerializeField] GameObject selectionMarker = null;
        [Tooltip("Smallest values select first")] public int choiceOrder = 0;

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