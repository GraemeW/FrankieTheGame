using System;
using UnityEngine.UIElements;
using LowDefMustard.Control;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class SkillWheelElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "skill-wheel";
        private const string _rowUssClassName = _ussClassName + "__row";
        private const string _directionUssClassName = _ussClassName + "__direction";

        // Events
        public event Action<ControllerInputType> directionClicked;

        public SkillWheelElement()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;

            Add(CreateDirection(ControllerInputType.NavigateUp, nameof(SkillSelectionModel.upSkillText)));
            var middleRow = new VisualElement { pickingMode = PickingMode.Ignore };
            middleRow.AddToClassList(_rowUssClassName);
            middleRow.Add(CreateDirection(ControllerInputType.NavigateLeft, nameof(SkillSelectionModel.leftSkillText)));
            middleRow.Add(CreateDirection(ControllerInputType.NavigateRight, nameof(SkillSelectionModel.rightSkillText)));
            Add(middleRow);
            Add(CreateDirection(ControllerInputType.NavigateDown, nameof(SkillSelectionModel.downSkillText)));
        }

        private Button CreateDirection(ControllerInputType direction, string sourcePropertyName)
        {
            var directionButton = new Button(() => directionClicked?.Invoke(direction)) { focusable = false }; // Keyboard handled via IInputReceiver
            directionButton.AddToClassList(_directionUssClassName);
            directionButton.SetBinding(nameof(Button.text), UIToolkitBindings.ToTarget(sourcePropertyName));
            return directionButton;
        }
    }
}
