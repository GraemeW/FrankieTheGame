using System;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // Base for selectable entries:  subclasses supply the element (and its model); click, choose and highlight handling are shared
    // Note:  Button elements are clickable (click -> Choose); other elements (e.g. slider rows) handle their own pointer input
    public abstract class ChoiceHandle : EntryHandle, IUIChoice
    {
        // State
        private readonly Action onChoose;
        private readonly Action onHighlight;

        // Constructor
        protected ChoiceHandle(UIToolkitBoxView view, Action onChoose, Action onHighlight = null, Type containerType = null) : base(view, containerType ?? typeof(ChoiceEntryContainer))
        {
            this.onChoose = onChoose;
            this.onHighlight = onHighlight;
        }

        // Abstract/Virtual
        protected abstract VisualElement CreateChoiceElement(); // Note:  Buttons should be non-focusable (input handled via IInputReceiver)
        protected abstract void SetHighlighted(bool enable); // Visual state only - Highlight(...) adds the highlight action
        public abstract void SetText(string text);
        protected virtual void Choose() => onChoose?.Invoke();

        #region EntryHandle
        protected sealed override VisualElement CreateElement()
        {
            VisualElement choiceElement = CreateChoiceElement();
            if (choiceElement is Button button) { button.clicked += HandleClicked; }
            return choiceElement;
        }

        protected override void UnhookElement(VisualElement detachingElement)
        {
            if (detachingElement is Button button) { button.clicked -= HandleClicked; }
        }
        #endregion

        #region IUIChoice
        public void UseChoice() => HandleClicked();

        public void Highlight(bool enable)
        {
            SetHighlighted(enable);
            if (enable && !isRemoved) { onHighlight?.Invoke(); }
        }
        #endregion

        #region PrivateMethods
        private void HandleClicked()
        {
            if (isRemoved) { return; }
            Choose();
        }
        #endregion
    }
}
