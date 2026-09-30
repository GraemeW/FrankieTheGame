using Unity.Properties;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class ChoiceEntryElement : Button
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime
        
        // Const Tunables
        private const string _ussClassName = "choice-entry";
        private const string _highlightedUssClassName = _ussClassName + "--highlighted";
        private const string _hiddenUssClassName = _ussClassName + "--hidden";
        private const string _markerUssClassName = _ussClassName + "__marker";
        private const string _labelUssClassName = _ussClassName + "__label";
        private const string _selectionMarker = ">";

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
                EnableInClassList(_hiddenUssClassName, !value);
            }
        }

        [CreateProperty, UxmlAttribute] public bool highlighted
        {
            get => internalHighlighted;
            set
            {
                internalHighlighted = value;
                EnableInClassList(_highlightedUssClassName, value);
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
            AddToClassList(_ussClassName);

            var marker = new Label(_selectionMarker);
            marker.AddToClassList(_markerUssClassName);
            Add(marker);

            label = new Label();
            label.AddToClassList(_labelUssClassName);
            label.SetBinding(nameof(Label.text), UIToolkitBindings.ToTarget(nameof(ChoiceEntryModel.text)));
            Add(label);

            SetBinding(nameof(revealed), UIToolkitBindings.ToTarget(nameof(ChoiceEntryModel.isRevealed)));
            SetBinding(nameof(highlighted), UIToolkitBindings.ToTarget(nameof(ChoiceEntryModel.isHighlighted)));
        }
    }
}
