using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class ChoiceEntryContainer : VisualElement
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime
        
        // Const Tunables
        private const string _ussClassName = "choice-entries";
        private const string _horizontalUssClassName = _ussClassName + "--horizontal";
        private const string _verticalUssClassName = _ussClassName + "--vertical";
        private const string _emptyUssClassName = _ussClassName + "--empty";

        // State
        private ChoiceLayout internalChoiceLayout = ChoiceLayout.Horizontal;

        [UxmlAttribute] public ChoiceLayout choiceLayout
        {
            get => internalChoiceLayout;
            set => SetLayout(value);
        }

        public ChoiceEntryContainer()
        {
            AddToClassList(_ussClassName);
            SetLayout(ChoiceLayout.Horizontal);
            RegisterCallback<AttachToPanelEvent>(_ => RefreshEmptyState()); // Catches children added via UXML
        }

        // Note:  UI Toolkit raises no child-added event - owners call this after adding/removing entries
        public void RefreshEmptyState() => EnableInClassList(_emptyUssClassName, childCount == 0);

        public void SetLayout(ChoiceLayout setChoiceLayout)
        {
            internalChoiceLayout = setChoiceLayout;
            EnableInClassList(_horizontalUssClassName, setChoiceLayout == ChoiceLayout.Horizontal);
            EnableInClassList(_verticalUssClassName, setChoiceLayout == ChoiceLayout.Vertical);
        }
    }
}
