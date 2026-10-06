using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class AbilitiesStatValueLabel : ModelBoundLabel
    {
        public AbilitiesStatValueLabel() : base(nameof(AbilitiesBoxModel.statText)) { }
    }
}
