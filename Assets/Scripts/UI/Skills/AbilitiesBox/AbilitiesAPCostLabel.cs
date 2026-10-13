using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class AbilitiesAPCostLabel : ModelBoundLabel
    {
        public AbilitiesAPCostLabel() : base(nameof(AbilitiesBoxModel.apCostLabel)) { }
    }
}
