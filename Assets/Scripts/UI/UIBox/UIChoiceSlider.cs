using Frankie.Control;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    public class UIChoiceSlider : UIChoice, IUIMoveInterceptor
    {
        // Tunables
        [SerializeField] private Slider slider;
        [SerializeField] protected float sliderAdjustmentStep = 0.1f;

        // Methods
        #region UnityMethods
        protected override void OnDestroy()
        {
            slider.onValueChanged.RemoveAllListeners();
            base.OnDestroy();
        }
        #endregion

        #region VirtualInterfaceMethods
        public override void UseChoice()
        {
            // No implementation needed for slider
        }   
        
        public bool TryMove(PlayerInputType playerInputType)
        {
            switch (playerInputType)
            {
                case PlayerInputType.NavigateLeft:
                    AdjustValue(-sliderAdjustmentStep);
                    return true;
                case PlayerInputType.NavigateRight:
                    AdjustValue(sliderAdjustmentStep);
                    return true;
                default:
                    return false;
            }
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
        
        #region PrivateMethods
        private void AdjustValue(float adjustment)
        {
            float updatedValue = slider.value + adjustment;
            SetSliderValue(Mathf.Clamp(updatedValue, slider.minValue, slider.maxValue));
        }
        #endregion
    }
}
