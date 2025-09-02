using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    public class UIChoiceSlider : UIChoice
    {
        // Tunables
        [SerializeField] Slider slider = null;

        // Methods
        #region UnityMethods
        protected override void OnDestroy()
        {
            slider.onValueChanged.RemoveAllListeners();
            base.OnDestroy();
        }
        #endregion

        #region PublicMethods
        public float GetSliderValue() => slider.value;

        public void SetSliderValue(float value)
        {
            slider.value = value;
        }

        public void AddOnValueChangeListener(UnityAction<float> unityAction)
        {
            if (unityAction == null) { return; }

            slider.onValueChanged.AddListener(unityAction);
        }
        #endregion
    }
}
