using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Utils.UI
{
    public abstract class UIChoice : MonoBehaviour
    {
        // Tunables
        [SerializeField] protected GameObject selectionMarker;
        [Tooltip("Smallest values select first")] public int choiceOrder = 0;
        [SerializeField] private Color validChoiceColor = Color.white;
        [SerializeField] private Color invalidChoiceColor = Color.gray;
        [SerializeField] private Color selectHighlightColor = Color.lawnGreen;
        [SerializeField] private TextMeshProUGUI textField;

        // State
        private readonly List<UnityAction> onHighlightExtraListeners = new();
        private bool dimInvalidChoices = false;
        private bool highlightSelected = false;

        // Unity Events
        public UnityEvent itemHighlighted;

        #region UnityMethods
        private void OnEnable()
        {
            if (onHighlightExtraListeners is not { Count: > 0 }) { return; }
            
            foreach (UnityAction unityAction in onHighlightExtraListeners)
            {
                itemHighlighted.AddListener(unityAction);
            }
        }

        private void OnDisable()
        {
            if (onHighlightExtraListeners is not { Count: > 0 }) { return; }
            
            foreach (UnityAction unityAction in onHighlightExtraListeners)
            {
                itemHighlighted.RemoveListener(unityAction);
            }
        }

        protected virtual void OnDestroy()
        {
            itemHighlighted.RemoveAllListeners();
        }
        #endregion

        #region PublicMethods
        public abstract void UseChoice();

        public void SetChoiceOrder(int setChoiceOrder)
        {
            choiceOrder = setChoiceOrder;
        }

        public void SetText(string text)
        {
            textField.text = text;
        }

        public void SetValidColor(bool enable)
        {
            textField.color = enable ? validChoiceColor : invalidChoiceColor;
        }

        public void UseInvalidChoiceDimming(bool enable)
        {
            dimInvalidChoices = enable;
        }

        public void UseHighlightSelected(bool enable)
        {
            highlightSelected = enable;
        }

        public void DisableHighlightListeners()
        {
            for (int i = 0; i < itemHighlighted.GetPersistentEventCount(); i++)
            {
                itemHighlighted.SetPersistentListenerState(i, UnityEventCallState.Off);
            }
        }
        
        public void AddOnHighlightListener(UnityAction unityAction)
        {
            if (unityAction == null) { return; }

            onHighlightExtraListeners.Add(unityAction);
            if (gameObject.activeSelf) { itemHighlighted.AddListener(unityAction); }
        }

        public virtual void Highlight(bool enable)
        {
            if (selectionMarker != null) { selectionMarker.SetActive(enable); }
            if (dimInvalidChoices) { textField.color = enable ? validChoiceColor : invalidChoiceColor; }

            if (highlightSelected)
            {
                textField.color = enable ? selectHighlightColor : validChoiceColor;
                textField.fontStyle = enable ? FontStyles.Bold : FontStyles.Normal;
            }
            if (enable && itemHighlighted != null) { itemHighlighted.Invoke(); }
        }
        #endregion
    }
}
