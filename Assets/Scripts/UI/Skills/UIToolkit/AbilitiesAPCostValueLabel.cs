using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class AbilitiesAPCostValueLabel : ModelBoundLabel
    {
        public AbilitiesAPCostValueLabel() : base(nameof(AbilitiesBoxModel.apCostText)) { }
    }
}
