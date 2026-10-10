using UnityEngine.UIElements;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class KeyboardSpecialKeys : KeyboardKeySet
    {
        public KeyboardSpecialKeys() : base(nameof(NamingPanelModel.specialColumns), NamingKeyboardLayout.defaultSpecialColumns) { }
    }
}
