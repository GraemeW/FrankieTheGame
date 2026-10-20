using UnityEngine.UIElements;

namespace Frankie.Rendering.UI
{
    [UxmlElement]
    public sealed partial class CinematicOverlayLabel : Label
    {
        public CinematicOverlayLabel()
        {
            pickingMode = PickingMode.Ignore;
            parseEscapeSequences = true; // Overlay strings carry literal "\n" line breaks
        }
    }
}
