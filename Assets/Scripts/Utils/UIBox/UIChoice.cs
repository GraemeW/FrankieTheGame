using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    public abstract class UIChoice : MonoBehaviour
    {
        // Tunables
        [SerializeField] protected GameObject selectionMarker = null;
        [Tooltip("Smallest values select first")] public int choiceOrder = 0;
        [SerializeField] Color validChoiceColor = Color.white;
        [SerializeField] Color invalidChoiceColor = Color.gray;
        [SerializeField] TextMeshProUGUI textField = null;

        // State
        List<UnityAction> onHighlightExtraListeners = new List<UnityAction>();

        // Unity Events
        public UnityEvent itemHighlighted;

        #region UnityMethods
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

        protected virtual void OnDestroy()
        {
            itemHighlighted.RemoveAllListeners();
        }
        #endregion

        #region PublicMethods
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
            if (unityAction == null) { return; }

            onHighlightExtraListeners.Add(unityAction);

            if (gameObject.activeSelf)
            {
                itemHighlighted.AddListener(unityAction);
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
        #endregion
    }
}
