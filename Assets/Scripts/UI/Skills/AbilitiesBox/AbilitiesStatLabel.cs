using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class AbilitiesStatLabel : ModelBoundLabel
    {
        public AbilitiesStatLabel() : base(nameof(AbilitiesBoxModel.statLabel)) { }
    }
}
