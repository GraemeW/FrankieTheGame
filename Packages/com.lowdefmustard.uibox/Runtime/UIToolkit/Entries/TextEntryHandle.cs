using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    public sealed class TextEntryHandle : EntryHandle, ITextScanEntry
    {
        // State
        private readonly TextEntryModel model = new();
        private readonly TextEntryType textEntryType;

        // Constructor
        public TextEntryHandle(UIToolkitBoxView view, TextEntryType textEntryType) : base(view, typeof(TextEntryContainer))
        {
            this.textEntryType = textEntryType;
        }

        // Overrides
        protected override VisualElement CreateElement() => new TextEntryElement(textEntryType) { dataSource = model };

        #region ITextScanEntry
        public bool canDisplayText => true;
        public void Reveal() => model.isRevealed = true;
        public void SetText(string text) => model.text = text;
        #endregion
    }
}
