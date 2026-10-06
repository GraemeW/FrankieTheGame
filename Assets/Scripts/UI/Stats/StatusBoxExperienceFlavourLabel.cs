using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Stats.UI
{
    [UxmlElement]
    public sealed partial class StatusBoxExperienceFlavourLabel : ModelBoundLabel
    {
        public StatusBoxExperienceFlavourLabel() : base(nameof(StatusBoxModel.experienceFlavourText)) { }
    }
}
