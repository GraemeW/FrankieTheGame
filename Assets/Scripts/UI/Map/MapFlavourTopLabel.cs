using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Zones.UI
{
    [UxmlElement]
    public sealed partial class MapFlavourTopLabel : ModelBoundLabel
    {
        public MapFlavourTopLabel() : base(nameof(MapModel.flavourTopText)) { }
    }
}
