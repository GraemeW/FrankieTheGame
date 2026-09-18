using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LowDefMustard.UIBox
{
    public class UIChoiceToggle : UIChoice
    {
        // Note:  Internal fields for test visibility
        
        // Tunables
        [SerializeField] internal Toggle toggle;

        // Methods
        #region UnityMethods
        protected override void OnDestroy()
        {
            toggle.onValueChanged.RemoveAllListeners();
            base.OnDestroy();
        }
        #endregion

        #region ClassMethods
        public override void UseChoice()
        {
            toggle.isOn = !toggle.isOn;
        }
        #endregion

        #region PublicMethods
        public bool GetToggleValue() => toggle.isOn;

        public void SetToggleValue(bool value)
        {
            toggle.isOn = value;
        }

        public void SetToggleValueSilently(bool value)
        {
            toggle.SetIsOnWithoutNotify(value);
        }

        public void AddOnValueChangeListener(UnityAction<bool> unityAction)
        {
            if (unityAction == null) { return; }
            toggle.onValueChanged.AddListener(unityAction);
        }
        #endregion
    }
}
