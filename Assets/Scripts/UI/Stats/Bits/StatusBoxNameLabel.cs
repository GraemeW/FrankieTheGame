using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Stats.UI
{
    [UxmlElement]
    public sealed partial class StatusBoxNameLabel : ModelBoundLabel
    {
        public StatusBoxNameLabel() : base(nameof(StatusBoxModel.characterName)) { }
    }
}
