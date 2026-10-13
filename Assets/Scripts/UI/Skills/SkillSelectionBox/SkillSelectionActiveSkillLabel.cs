using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class SkillSelectionActiveSkillLabel : ModelBoundLabel
    {
        // Const Tunables
        private const string _ussClassName = "active-skill-label";
        private const string _selectedUssClassName = _ussClassName + "--selected";

        // State
        private bool internalHasActiveSkill = false;

        [CreateProperty] public bool hasActiveSkill
        {
            get => internalHasActiveSkill;
            set
            {
                internalHasActiveSkill = value;
                EnableInClassList(_selectedUssClassName, value);
            }
        }

        public SkillSelectionActiveSkillLabel() : base(nameof(SkillSelectionModel.activeSkillText))
        {
            AddToClassList(_ussClassName);
            SetBinding(nameof(hasActiveSkill), UIToolkitBindings.ToTarget(nameof(SkillSelectionModel.hasActiveSkill)));
        }
    }
}
