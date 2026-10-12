using System;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    public sealed class ChoiceEntryHandle : ChoiceHandle
    {
        // State
        private readonly ChoiceEntryModel model;

        // Constructor
        public ChoiceEntryHandle(UIToolkitBoxView view, string text, bool isRevealed, Action onChoose, Action onHighlight = null, Type containerType = null) : base(view, onChoose, onHighlight, containerType)
        {
            model = new ChoiceEntryModel { text = text, isRevealed = isRevealed };
        }

        // Overrides
        protected override VisualElement CreateChoiceElement() => new ChoiceEntryElement { dataSource = model };
        
        // Note:  Internal for test visibility
        internal string text => model.text;
        internal bool isRevealed => model.isRevealed;
        internal bool isHighlighted => model.isHighlighted;

        #region PublicMethods
        public void Reveal() => model.isRevealed = true;
        public override void SetText(string text) => model.text = text;
        protected override void SetHighlighted(bool enable) => model.isHighlighted = enable;
        #endregion
    }
}
