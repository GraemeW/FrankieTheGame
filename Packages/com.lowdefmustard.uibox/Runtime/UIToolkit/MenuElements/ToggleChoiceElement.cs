using Unity.Properties;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // Choice row with a checkbox:  clicking the row toggles (via its handle)
    [UxmlElement]
    public sealed partial class ToggleChoiceElement : Button
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime

        // State
        private bool internalHighlighted = false;
        private bool internalIsOn = false;

        // Cached References
        private readonly Label label;

        [CreateProperty, UxmlAttribute] public bool highlighted
        {
            get => internalHighlighted;
            set
            {
                internalHighlighted = value;
                ChoiceRowParts.SetHighlighted(this, value);
            }
        }

        [CreateProperty, UxmlAttribute] public bool isOn
        {
            get => internalIsOn;
            set
            {
                internalIsOn = value;
                EnableInClassList(USSClassNames.ToggleChoice.on, value);
            }
        }

        [UxmlAttribute] public string labelText
        {
            get => label.text;
            set => label.text = value;
        }

        public ToggleChoiceElement()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            ChoiceRowParts.Initialize(this);
            AddToClassList(USSClassNames.ToggleChoice.block);
            label = ChoiceRowParts.AddLabel(this, nameof(ToggleChoiceModel.text));

            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList(USSClassNames.ToggleChoice.box);
            var check = new VisualElement { pickingMode = PickingMode.Ignore };
            check.AddToClassList(USSClassNames.ToggleChoice.check);
            box.Add(check);
            Add(box);

            SetBinding(nameof(highlighted), UIToolkitBindings.ToTarget(nameof(ToggleChoiceModel.isHighlighted)));
            SetBinding(nameof(isOn), UIToolkitBindings.ToTarget(nameof(ToggleChoiceModel.isOn)));
        }
    }
}
