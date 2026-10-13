using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    [UxmlElement]
    public sealed partial class StatChangeListElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "stat-change-list";
        private const string _emptyUssClassName = _ussClassName + "--empty";
        private const string _lineUssClassName = "stat-change";
        private const string _betterUssClassName = _lineUssClassName + "--better";
        private const string _worseUssClassName = _lineUssClassName + "--worse";
        private const string _nameUssClassName = _lineUssClassName + "__name";
        private const string _oldValueUssClassName = _lineUssClassName + "__old-value";
        private const string _arrowUssClassName = _lineUssClassName + "__arrow";
        private const string _newValueUssClassName = _lineUssClassName + "__new-value";
        private const string _arrow = "-->";

        // State
        private IReadOnlyList<StatChangeLine> internalLines;

        [CreateProperty] public IReadOnlyList<StatChangeLine> lines
        {
            get => internalLines;
            set
            {
                internalLines = value;
                Rebuild();
            }
        }

        public StatChangeListElement()
        {
            AddToClassList(_ussClassName);
            AddToClassList(_emptyUssClassName);
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(lines), UIToolkitBindings.ToTarget(nameof(ItemBoxModel.statChanges)));
        }

        #region PrivateMethods
        private void Rebuild()
        {
            Clear();
            EnableInClassList(_emptyUssClassName, internalLines == null || internalLines.Count == 0);
            if (internalLines == null) { return; }
            foreach (StatChangeLine statChangeLine in internalLines)
            {
                var lineElement = new VisualElement { pickingMode = PickingMode.Ignore };
                lineElement.AddToClassList(_lineUssClassName);
                lineElement.EnableInClassList(_betterUssClassName, statChangeLine.direction == StatChangeDirection.Better);
                lineElement.EnableInClassList(_worseUssClassName, statChangeLine.direction == StatChangeDirection.Worse);
                lineElement.Add(CreateLabel(statChangeLine.name, _nameUssClassName));
                lineElement.Add(CreateLabel(statChangeLine.oldValue, _oldValueUssClassName));
                lineElement.Add(CreateLabel(_arrow, _arrowUssClassName));
                lineElement.Add(CreateLabel(statChangeLine.newValue, _newValueUssClassName));
                Add(lineElement);
            }
        }

        private static Label CreateLabel(string text, string labelUssClassName)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(labelUssClassName);
            return label;
        }
        #endregion
    }
}
