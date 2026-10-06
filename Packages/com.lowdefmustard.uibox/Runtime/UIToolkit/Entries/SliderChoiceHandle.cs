using System;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox
{
    // Normalized (0-1) value:  left/right steps it; choosing does nothing
    public sealed class SliderChoiceHandle : ChoiceHandle, IUIMoveInterceptor
    {
        // State
        private readonly SliderChoiceModel model;
        private readonly float step;
        private readonly Action<float> onValueChanged;

        // Constructor
        public SliderChoiceHandle(UIToolkitBoxView view, string text, float value, float step, Action<float> onValueChanged, Type containerType = null) : base(view, null, null, containerType)
        {
            model = new SliderChoiceModel { text = text, value = Mathf.Clamp01(value) };
            this.step = step;
            this.onValueChanged = onValueChanged;
        }

        #region PublicMethods
        public float value => model.value;

        public void SetValue(float setValue)
        {
            float clampedValue = Mathf.Clamp01(setValue);
            if (Mathf.Approximately(clampedValue, model.value)) { return; }
            model.value = clampedValue;
            onValueChanged?.Invoke(clampedValue);
        }
        #endregion

        #region ChoiceHandle
        protected override VisualElement CreateChoiceElement()
        {
            var sliderChoiceElement = new SliderChoiceElement { dataSource = model };
            sliderChoiceElement.valueChanged += SetValue;
            return sliderChoiceElement;
        }

        protected override void UnhookElement(VisualElement detachingElement)
        {
            if (detachingElement is SliderChoiceElement sliderChoiceElement) { sliderChoiceElement.valueChanged -= SetValue; }
            base.UnhookElement(detachingElement);
        }

        protected override void SetHighlighted(bool enable) => model.isHighlighted = enable;
        public override void SetText(string text) => model.text = text;
        #endregion

        #region IUIMoveInterceptor
        public bool TryMove(ControllerInputType controllerInputType, out bool isHighlightMove)
        {
            isHighlightMove = false;
            switch (controllerInputType)
            {
                case ControllerInputType.NavigateLeft:
                    SetValue(model.value - step);
                    return true;
                case ControllerInputType.NavigateRight:
                    SetValue(model.value + step);
                    return true;
                default:
                    return false;
            }
        }
        #endregion
    }
}
