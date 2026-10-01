using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    // Save slot row:  reuses the choice-entry block classes for marker visibility and hover tint
    [UxmlElement]
    public sealed partial class SaveSlotElement : Button
    {
        // Const Tunables
        private const string _ussClassName = "save-slot";
        private const string _indexUssClassName = _ussClassName + "__index";
        private const string _nameUssClassName = _ussClassName + "__name";
        private const string _levelLabelUssClassName = _ussClassName + "__level-label";
        private const string _levelUssClassName = _ussClassName + "__level";

        // State
        private bool internalHighlighted = false;

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

        // Constructor
        public SaveSlotElement()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            ChoiceRowParts.Initialize(this);
            AddToClassList(_ussClassName);

            AddBoundLabel(_indexUssClassName, nameof(SaveSlotModel.indexText));
            AddBoundLabel(_nameUssClassName, nameof(SaveSlotModel.characterName));
            AddBoundLabel(_levelLabelUssClassName, nameof(SaveSlotModel.levelLabel));
            AddBoundLabel(_levelUssClassName, nameof(SaveSlotModel.levelText));

            SetBinding(nameof(highlighted), UIToolkitBindings.ToTarget(nameof(SaveSlotModel.isHighlighted)));
        }

        #region PrivateMethods
        private void AddBoundLabel(string setUSSClassName, string sourcePropertyName)
        {
            ChoiceRowParts.AddLabel(this, sourcePropertyName).AddToClassList(setUSSClassName);
        }
        #endregion
    }
}
