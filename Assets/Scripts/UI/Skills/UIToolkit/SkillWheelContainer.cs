using UnityEngine.UIElements;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class SkillWheelContainer : VisualElement
    {
        public SkillWheelContainer()
        {
            pickingMode = PickingMode.Ignore;
        }
    }
}
