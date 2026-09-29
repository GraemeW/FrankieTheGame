using Unity.Properties;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // Leaf elements:  bind to a TextEntryModel / ChoiceEntryModel set as the element's dataSource
    [UxmlElement]
    public sealed partial class TextEntryElement : VisualElement
    {
        private const string _ussClassName = "text-entry";
        private const string _speechUssClassName = _ussClassName + "--speech";
        private const string _hiddenUssClassName = _ussClassName + "--hidden";
        private const string _bulletUssClassName = _ussClassName + "__bullet";
        private const string _labelUssClassName = _ussClassName + "__label";
        private const string _speechBullet = "•";

        // State
        private bool internalRevealed = true;

        [CreateProperty] public bool revealed
        {
            get => internalRevealed;
            set
            {
                internalRevealed = value;
                EnableInClassList(_hiddenUssClassName, !value);
            }
        }

        public TextEntryElement() : this(TextEntryType.Simple) { }

        public TextEntryElement(TextEntryType textEntryType)
        {
            AddToClassList(_ussClassName);
            if (textEntryType == TextEntryType.Speech)
            {
                AddToClassList(_speechUssClassName);
                var bullet = new Label(_speechBullet);
                bullet.AddToClassList(_bulletUssClassName);
                Add(bullet);
            }

            var label = new Label();
            label.AddToClassList(_labelUssClassName);
            label.SetBinding(nameof(Label.text), UIToolkitBindings.ToTarget(nameof(TextEntryModel.text)));
            Add(label);

            SetBinding(nameof(revealed), UIToolkitBindings.ToTarget(nameof(TextEntryModel.isRevealed)));
        }
    }
}
