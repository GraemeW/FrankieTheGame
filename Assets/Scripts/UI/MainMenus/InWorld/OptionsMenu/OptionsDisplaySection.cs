using UnityEngine.UIElements;

namespace Frankie.Menu.UI
{
    // Typed placement target for OptionsMenu entries (queried by type, see EntryHandle.containerType)
    [UxmlElement]
    public sealed partial class OptionsDisplaySection : VisualElement
    {
        public OptionsDisplaySection()
        {
            pickingMode = PickingMode.Ignore;
        }
    }
}
