using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Stats.UI
{
    [UxmlElement]
    public sealed partial class StatusBoxExperienceLabel : ModelBoundLabel
    {
        public StatusBoxExperienceLabel() : base(nameof(StatusBoxModel.experienceToLevelText)) { }
    }
}
