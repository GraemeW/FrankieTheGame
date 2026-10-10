using System;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class FrameFlavourHandle : ChoiceHandle
    {
        // State
        private readonly FrameFlavourChoiceModel model;

        // Constructor
        public FrameFlavourHandle(UIToolkitBoxView view, FrameFlavourChoiceModel model, Action onChoose, Action onHighlight) : base(view, onChoose, onHighlight)
        {
            this.model = model;
        }

        // Overrides
        protected override VisualElement CreateChoiceElement() => new FrameFlavourChoiceElement { dataSource = model };
        protected override void SetHighlighted(bool enable) => model.isHighlighted = enable;
        public override void SetText(string text) => model.text = text;
    }
}
