using Unity.Properties;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // Leaf elements:  bind to a TextEntryModel / ChoiceEntryModel set as the element's dataSource
    [UxmlElement]
    public sealed partial class TextEntryElement : VisualElement
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime
        
        // Const Tunables
        private const string _ussClassName = "text-entry";
        private const string _speechUssClassName = _ussClassName + "--speech";
        private const string _hiddenUssClassName = _ussClassName + "--hidden";
        private const string _bulletUssClassName = _ussClassName + "__bullet";
        private const string _labelUssClassName = _ussClassName + "__label";
        private const string _speechBullet = "•";

        // State
        private bool internalRevealed = true;
        private TextEntryType internalEntryType = TextEntryType.Simple;

        // Cached References
        private readonly Label label;
        private Label bullet;

        [CreateProperty] public bool revealed
        {
            get => internalRevealed;
            set
            {
                internalRevealed = value;
                EnableInClassList(_hiddenUssClassName, !value);
            }
        }

        [UxmlAttribute] public TextEntryType entryType
        {
            get => internalEntryType;
            set
            {
                internalEntryType = value;
                ApplyEntryType();
            }
        }

        [UxmlAttribute] public string labelText
        {
            get => label.text;
            set => label.text = value;
        }

        public TextEntryElement() : this(TextEntryType.Simple) { }

        public TextEntryElement(TextEntryType textEntryType)
        {
            AddToClassList(_ussClassName);

            label = new Label();
            label.AddToClassList(_labelUssClassName);
            label.SetBinding(nameof(Label.text), UIToolkitBindings.ToTarget(nameof(TextEntryModel.text)));
            Add(label);

            SetBinding(nameof(revealed), UIToolkitBindings.ToTarget(nameof(TextEntryModel.isRevealed)));
            entryType = textEntryType;
        }

        private void ApplyEntryType()
        {
            bool isSpeech = internalEntryType == TextEntryType.Speech;
            EnableInClassList(_speechUssClassName, isSpeech);
            if (!isSpeech)
            {
                bullet?.RemoveFromHierarchy();
                return;
            }

            if (bullet == null)
            {
                bullet = new Label(_speechBullet);
                bullet.AddToClassList(_bulletUssClassName);
            }
            Insert(0, bullet);
        }
    }
}
