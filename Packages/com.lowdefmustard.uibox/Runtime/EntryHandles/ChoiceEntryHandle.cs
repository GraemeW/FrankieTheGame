using System;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    public sealed class ChoiceEntryHandle : ChoiceHandle
    {
        // State
        private readonly ChoiceEntryModel model;
        private readonly bool isMultiline;

        // Constructor
        public ChoiceEntryHandle(UIToolkitBoxView view, string text, bool isRevealed, Action onChoose, Action onHighlight = null, Type containerType = null, bool isMultiline = false) : base(view, onChoose, onHighlight, containerType)
        {
            model = new ChoiceEntryModel { text = text, isRevealed = isRevealed };
            this.isMultiline = isMultiline;
        }

        // Overrides
        protected override VisualElement CreateChoiceElement() => new ChoiceEntryElement { dataSource = model, multiline = isMultiline };
        
        // Note:  Internal for test visibility
        internal string text => model.text;
        internal bool isRevealed => model.isRevealed;
        internal bool isHighlighted => model.isHighlighted;

        #region PublicMethods
        public void Reveal() => model.isRevealed = true;
        public override void SetText(string setText) => model.text = setText;
        protected override void SetHighlighted(bool enable) => model.isHighlighted = enable;
        #endregion
    }
}
