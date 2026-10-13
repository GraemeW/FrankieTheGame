using System;
using Unity.Properties;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // Choice row with a normalized (0-1) slider:  not a Button, so the slider owns pointer input (drag)
    [UxmlElement]
    public sealed partial class SliderChoiceElement : VisualElement
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime

        // State
        private bool internalHighlighted = false;

        // Cached References
        private readonly Label label;
        private readonly Slider slider;

        // Events
        public event Action<float> valueChanged; // Pointer-driven changes only (bound model updates don't notify)

        [CreateProperty, UxmlAttribute] public bool highlighted
        {
            get => internalHighlighted;
            set
            {
                internalHighlighted = value;
                ChoiceRowParts.SetHighlighted(this, value);
            }
        }

        [CreateProperty, UxmlAttribute] public float value
        {
            get => slider.value;
            set => slider.SetValueWithoutNotify(value);
        }

        [UxmlAttribute] public string labelText
        {
            get => label.text;
            set => label.text = value;
        }

        public SliderChoiceElement()
        {
            ChoiceRowParts.Initialize(this);
            AddToClassList(USSClassNames.SliderChoice.block);
            label = ChoiceRowParts.AddLabel(this, nameof(SliderChoiceModel.text));

            slider = new Slider(0f, 1f) { focusable = false }; // Keyboard/gamepad input handled via IInputReceiver
            slider.AddToClassList(USSClassNames.SliderChoice.slider);
            slider.RegisterValueChangedCallback(changeEvent => valueChanged?.Invoke(changeEvent.newValue));
            Add(slider);

            SetBinding(nameof(highlighted), UIToolkitBindings.ToTarget(nameof(SliderChoiceModel.isHighlighted)));
            SetBinding(nameof(value), UIToolkitBindings.ToTarget(nameof(SliderChoiceModel.value)));
        }
    }
}
