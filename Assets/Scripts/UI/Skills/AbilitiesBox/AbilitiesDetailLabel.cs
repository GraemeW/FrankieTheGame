using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class AbilitiesDetailLabel : ModelBoundLabel
    {
        public AbilitiesDetailLabel() : base(nameof(AbilitiesBoxModel.detailText)) { }
    }
}
