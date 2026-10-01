using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    // Save slot row:  reuses the choice-entry block classes for marker visibility and hover tint
    [UxmlElement]
    public sealed partial class SaveSlotElement : Button
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime

        // Const Tunables
        private const string _ussClassName = "save-slot";
        private const string _indexUssClassName = _ussClassName + "__index";
        private const string _nameUssClassName = _ussClassName + "__name";
        private const string _levelLabelUssClassName = _ussClassName + "__level-label";
        private const string _levelUssClassName = _ussClassName + "__level";

        // State
        private bool internalHighlighted = false;

        // Cached References
        private readonly Label indexLabel;
        private readonly Label nameLabel;
        private readonly Label levelLabel;
        private readonly Label levelValueLabel;

        // Bound Properties
        [CreateProperty, UxmlAttribute] public bool highlighted
        {
            get => internalHighlighted;
            set
            {
                internalHighlighted = value;
                ChoiceRowParts.SetHighlighted(this, value);
            }
        }

        [UxmlAttribute] public string indexText
        {
            get => indexLabel.text;
            set => indexLabel.text = value;
        }

        [UxmlAttribute] public string nameText
        {
            get => nameLabel.text;
            set => nameLabel.text = value;
        }

        [UxmlAttribute] public string levelLabelText
        {
            get => levelLabel.text;
            set => levelLabel.text = value;
        }

        [UxmlAttribute] public string levelText
        {
            get => levelValueLabel.text;
            set => levelValueLabel.text = value;
        }

        // Constructor
        public SaveSlotElement()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            ChoiceRowParts.Initialize(this);
            AddToClassList(_ussClassName);

            indexLabel = AddBoundLabel(_indexUssClassName, nameof(SaveSlotModel.indexText));
            nameLabel = AddBoundLabel(_nameUssClassName, nameof(SaveSlotModel.characterName));
            levelLabel = AddBoundLabel(_levelLabelUssClassName, nameof(SaveSlotModel.levelLabel));
            levelValueLabel = AddBoundLabel(_levelUssClassName, nameof(SaveSlotModel.levelText));

            SetBinding(nameof(highlighted), UIToolkitBindings.ToTarget(nameof(SaveSlotModel.isHighlighted)));
        }

        #region PrivateMethods
        private Label AddBoundLabel(string setUSSClassName, string sourcePropertyName)
        {
            Label label = ChoiceRowParts.AddLabel(this, sourcePropertyName);
            label.AddToClassList(setUSSClassName);
            return label;
        }
        #endregion
    }
}
