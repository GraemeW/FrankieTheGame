using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    public class UIChoiceToggle : UIChoice
    {
        // Tunables
        [SerializeField] Toggle toggle = null;

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
