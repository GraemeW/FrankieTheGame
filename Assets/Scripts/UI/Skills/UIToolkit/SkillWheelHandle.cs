using System;
using UnityEngine.UIElements;
using LowDefMustard.Control;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class SkillWheelHandle : EntryHandle
    {
        // State
        private readonly SkillSelectionModel model;
        private readonly Action<ControllerInputType> onDirectionClicked;

        // Constructor
        public SkillWheelHandle(UIToolkitBoxView view, SkillSelectionModel model, Action<ControllerInputType> onDirectionClicked) : base(view, typeof(SkillWheelContainer))
        {
            this.model = model;
            this.onDirectionClicked = onDirectionClicked;
        }

        // Overrides
        protected override VisualElement CreateElement()
        {
            var skillWheelElement = new SkillWheelElement { dataSource = model };
            skillWheelElement.directionClicked += HandleDirectionClicked;
            return skillWheelElement;
        }

        protected override void UnhookElement(VisualElement detachingElement)
        {
            if (detachingElement is SkillWheelElement skillWheelElement) { skillWheelElement.directionClicked -= HandleDirectionClicked; }
        }

        private void HandleDirectionClicked(ControllerInputType direction)
        {
            if (isRemoved) { return; }
            onDirectionClicked?.Invoke(direction);
        }
    }
}
