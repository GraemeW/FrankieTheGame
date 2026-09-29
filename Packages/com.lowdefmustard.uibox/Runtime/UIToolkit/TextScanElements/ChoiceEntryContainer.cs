using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class ChoiceEntryContainer : VisualElement
    {
        private const string _ussClassName = "choice-entries";
        private const string _horizontalUssClassName = _ussClassName + "--horizontal";
        private const string _verticalUssClassName = _ussClassName + "--vertical";

        public ChoiceEntryContainer()
        {
            AddToClassList(_ussClassName);
            SetLayout(ChoiceLayout.Horizontal);
        }

        public void SetLayout(ChoiceLayout choiceLayout)
        {
            EnableInClassList(_horizontalUssClassName, choiceLayout == ChoiceLayout.Horizontal);
            EnableInClassList(_verticalUssClassName, choiceLayout == ChoiceLayout.Vertical);
        }
    }
}
