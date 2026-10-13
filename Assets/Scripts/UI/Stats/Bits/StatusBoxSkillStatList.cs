using UnityEngine.UIElements;

namespace Frankie.Stats.UI
{
    [UxmlElement]
    public sealed partial class StatusBoxSkillStatList : StatListElement
    {
        public StatusBoxSkillStatList() : base(nameof(StatusBoxModel.skillStats)) { }
    }
}
