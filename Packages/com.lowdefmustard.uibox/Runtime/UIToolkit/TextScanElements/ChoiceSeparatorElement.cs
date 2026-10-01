using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class ChoiceSeparatorElement : VisualElement
    {

        // State
        private ChoiceSeparatorType internalSeparatorType = ChoiceSeparatorType.Major;

        [UxmlAttribute] public ChoiceSeparatorType separatorType
        {
            get => internalSeparatorType;
            set
            {
                internalSeparatorType = value;
                EnableInClassList(USSClassNames.ChoiceSeparator.minor, value == ChoiceSeparatorType.Minor);
            }
        }

        public ChoiceSeparatorElement()
        {
            AddToClassList(USSClassNames.ChoiceSeparator.block);
            pickingMode = PickingMode.Ignore;
        }
    }
}
