using UnityEngine.UIElements;

namespace Frankie.Stats.UI
{
    [UxmlElement]
    public sealed partial class StatusBoxVitalStatList : StatListElement
    {
        public StatusBoxVitalStatList() : base(nameof(StatusBoxModel.vitalStats)) { }
    }
}
