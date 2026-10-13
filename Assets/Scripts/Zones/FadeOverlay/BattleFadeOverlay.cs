using UnityEngine.UIElements;

namespace Frankie.Zones.UI
{
    [UxmlElement]
    public sealed partial class BattleFadeOverlay : FadeOverlayElement
    {
        public BattleFadeOverlay() : base(nameof(FaderModel.battleFadeOpacity)) { }
    }
}
