using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    public sealed class TextEntryHandle : EntryHandle
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

        // Note:  Internal for test visibility
        internal string text => model.text;
        internal bool isRevealed => model.isRevealed;

        #region PublicMethods
        public void Reveal() => model.isRevealed = true;
        public void SetText(string setText) => model.text = setText;
        #endregion
    }
}
