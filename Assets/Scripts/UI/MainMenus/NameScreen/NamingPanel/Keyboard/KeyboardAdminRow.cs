using UnityEngine.UIElements;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class KeyboardAdminRow : VisualElement
    {
        public KeyboardAdminRow()
        {
            pickingMode = PickingMode.Ignore;
        }
    }
}
