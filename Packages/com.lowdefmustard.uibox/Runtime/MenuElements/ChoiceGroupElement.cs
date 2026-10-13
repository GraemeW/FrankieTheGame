using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class ChoiceGroupElement : VisualElement
    {

        public ChoiceGroupElement()
        {
            AddToClassList(USSClassNames.ChoiceGroup.block);
            pickingMode = PickingMode.Ignore;
        }
    }
}
