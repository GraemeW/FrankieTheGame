using UnityEngine.UIElements;

namespace Frankie.Menu.UI
{
    // Typed placement target for OptionsMenu entries (queried by type, see EntryHandle.containerType)
    [UxmlElement]
    public sealed partial class OptionsLanguageSection : VisualElement
    {
        public OptionsLanguageSection()
        {
            pickingMode = PickingMode.Ignore;
        }
    }
}
