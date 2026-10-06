using System;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    public sealed class ChoiceEntryHandle : ChoiceHandle, ITextScanChoiceEntry
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

        #region ITextScanChoiceEntry
        public bool canDisplayText => true;
        public void Reveal() => model.isRevealed = true;
        public override void SetText(string text) => model.text = text;
        protected override void SetHighlighted(bool enable) => model.isHighlighted = enable;
        #endregion
    }
}
