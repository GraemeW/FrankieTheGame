using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class ChoiceEntryContainer : VisualElement
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime
        

        // State
        private ChoiceLayout internalChoiceLayout = ChoiceLayout.Horizontal;

        [UxmlAttribute] public ChoiceLayout choiceLayout
        {
            get => internalChoiceLayout;
            set => SetLayout(value);
        }

        public ChoiceEntryContainer()
        {
            AddToClassList(USSClassNames.ChoiceEntries.block);
            SetLayout(ChoiceLayout.Horizontal);
            RegisterCallback<AttachToPanelEvent>(_ => RefreshEmptyState()); // Catches children added via UXML
        }

        // Note:  UI Toolkit raises no child-added event - owners call this after adding/removing entries
        public void RefreshEmptyState() => EnableInClassList(USSClassNames.ChoiceEntries.empty, childCount == 0);

        public void SetLayout(ChoiceLayout setChoiceLayout)
        {
            internalChoiceLayout = setChoiceLayout;
            EnableInClassList(USSClassNames.ChoiceEntries.horizontal, setChoiceLayout == ChoiceLayout.Horizontal);
            EnableInClassList(USSClassNames.ChoiceEntries.vertical, setChoiceLayout == ChoiceLayout.Vertical);
        }
    }
}
