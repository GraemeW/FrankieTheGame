using UnityEngine.UIElements;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class NamingStageElement : VisualElement
    {
        public NamingStageElement()
        {
            pickingMode = PickingMode.Ignore;
        }
    }
}
