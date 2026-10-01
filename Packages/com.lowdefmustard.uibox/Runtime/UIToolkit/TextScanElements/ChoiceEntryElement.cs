using Unity.Properties;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class ChoiceEntryElement : Button
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime
        

        // State
        private bool internalRevealed = true;
        private bool internalHighlighted = false;

        // Cached References
        private readonly Label label;

        [CreateProperty] public bool revealed
        {
            get => internalRevealed;
            set
            {
                internalRevealed = value;
                EnableInClassList(USSClassNames.ChoiceEntry.hidden, !value);
            }
        }

        [CreateProperty, UxmlAttribute] public bool highlighted
        {
            get => internalHighlighted;
            set
            {
                internalHighlighted = value;
                ChoiceRowParts.SetHighlighted(this, value);
            }
        }

        [UxmlAttribute] public string labelText
        {
            get => label.text;
            set => label.text = value;
        }

        public ChoiceEntryElement()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            ChoiceRowParts.Initialize(this);
            label = ChoiceRowParts.AddLabel(this, nameof(ChoiceEntryModel.text));

            SetBinding(nameof(revealed), UIToolkitBindings.ToTarget(nameof(ChoiceEntryModel.isRevealed)));
            SetBinding(nameof(highlighted), UIToolkitBindings.ToTarget(nameof(ChoiceEntryModel.isHighlighted)));
        }
    }
}
