using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Stats.UI
{
    public abstract class StatListElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "stat-list";
        private const string _lineUssClassName = "stat-line";
        private const string _nameUssClassName = _lineUssClassName + "__name";
        private const string _valueUssClassName = _lineUssClassName + "__value";

        // State
        private IReadOnlyList<StatLine> internalLines;

        [CreateProperty] public IReadOnlyList<StatLine> lines
        {
            get => internalLines;
            set
            {
                internalLines = value;
                Rebuild();
            }
        }

        protected StatListElement(string sourcePropertyName)
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(lines), UIToolkitBindings.ToTarget(sourcePropertyName));
        }

        #region PrivateMethods
        private void Rebuild()
        {
            Clear();
            if (internalLines == null) { return; }
            foreach (StatLine statLine in internalLines)
            {
                var lineElement = new VisualElement { pickingMode = PickingMode.Ignore };
                lineElement.AddToClassList(_lineUssClassName);
                lineElement.Add(CreateLabel(statLine.name, _nameUssClassName));
                lineElement.Add(CreateLabel(statLine.value, _valueUssClassName));
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
