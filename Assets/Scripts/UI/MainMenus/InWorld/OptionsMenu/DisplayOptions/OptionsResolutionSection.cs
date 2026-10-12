using UnityEngine.UIElements;

namespace Frankie.Menu.UI
{
    // Typed placement target for OptionsMenu entries (queried by type, see EntryHandle.containerType)
    [UxmlElement]
    public sealed partial class OptionsResolutionSection : VisualElement
    {
        public OptionsResolutionSection()
        {
            pickingMode = PickingMode.Ignore;
        }
    }
}
