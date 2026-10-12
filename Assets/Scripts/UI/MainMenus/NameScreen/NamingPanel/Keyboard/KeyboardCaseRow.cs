using UnityEngine.UIElements;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class KeyboardCaseRow : VisualElement
    {
        public KeyboardCaseRow()
        {
            pickingMode = PickingMode.Ignore;
        }
    }
}
