using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // Shared construction for choice rows (choice-entry block):  selection marker, label, highlight state
    public static class ChoiceRowParts
    {
        // Const Tunables
        private const string _selectionMarker = ">";

        public static void Initialize(VisualElement row)
        {
            row.AddToClassList(USSClassNames.ChoiceEntry.block);
            var marker = new Label(_selectionMarker);
            marker.AddToClassList(USSClassNames.ChoiceEntry.marker);
            row.Add(marker);
        }

        public static Label AddLabel(VisualElement row, string sourcePropertyName)
        {
            var label = new Label();
            label.AddToClassList(USSClassNames.ChoiceEntry.label);
            label.SetBinding(nameof(Label.text), UIToolkitBindings.ToTarget(sourcePropertyName));
            row.Add(label);
            return label;
        }

        public static void SetHighlighted(VisualElement row, bool enable) => row.EnableInClassList(USSClassNames.ChoiceEntry.highlighted, enable);
    }
}
