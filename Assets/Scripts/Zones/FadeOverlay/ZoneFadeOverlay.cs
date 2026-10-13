using UnityEngine.UIElements;

namespace Frankie.Zones.UI
{
    [UxmlElement]
    public sealed partial class ZoneFadeOverlay : FadeOverlayElement
    {
        public ZoneFadeOverlay() : base(nameof(FaderModel.zoneFadeOpacity)) { }
    }
}
