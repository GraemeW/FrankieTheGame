using System;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    public sealed class ToggleChoiceHandle : ChoiceHandle
    {
        // State
        private readonly ToggleChoiceModel model;
        private readonly Action<bool> onValueChanged;

        // Constructor
        public ToggleChoiceHandle(UIToolkitBoxView view, string text, bool isOn, Action<bool> onValueChanged, Type containerType = null) : base(view, null, null, containerType)
        {
            model = new ToggleChoiceModel { text = text, isOn = isOn };
            this.onValueChanged = onValueChanged;
        }

        #region PublicMethods
        public bool isOn => model.isOn;

        public void SetValue(bool setValue)
        {
            if (model.isOn == setValue) { return; }
            model.isOn = setValue;
            onValueChanged?.Invoke(setValue);
        }

        public void SetValueWithoutNotify(bool setValue) => model.isOn = setValue;
        #endregion

        #region ChoiceHandle
        protected override VisualElement CreateChoiceElement() => new ToggleChoiceElement { dataSource = model };
        protected override void Choose() => SetValue(!model.isOn);
        protected override void SetHighlighted(bool enable) => model.isHighlighted = enable;
        public override void SetText(string text) => model.text = text;
        #endregion
    }
}
