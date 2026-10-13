using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Zones.UI
{
    [UxmlElement]
    public sealed partial class MapFlavourBottomLabel : ModelBoundLabel
    {
        public MapFlavourBottomLabel() : base(nameof(MapModel.flavourBottomText)) { }
    }
}
