using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox
{
    // One navigable choice holding a horizontal row of sub-choices:  left/right moves between them, choosing uses the highlighted one
    public sealed class ChoiceGroupHandle : EntryHandle, IUIChoice, IUIMoveInterceptor
    {
        // State
        private readonly List<ChoiceEntryHandle> subChoices = new();
        private ChoiceEntryHandle highlightedSubChoice;

        // Constructor
        public ChoiceGroupHandle(UIToolkitBoxView view, Type containerType) : base(view, containerType) { }

        #region PublicMethods
        public IUIChoice AddSubChoice(string text, Action onChoose) // Note:  Public so boxes can add custom sub-choices
        {
            var subChoice = new ChoiceEntryHandle(view, text, true, onChoose); // lives in this group's element, not a view container
            subChoices.Add(subChoice);
            element?.Add(subChoice.BuildElement());
            return subChoice;
        }
        #endregion

        #region EntryHandle
        protected override VisualElement CreateElement()
        {
            var choiceGroupElement = new ChoiceGroupElement();
            foreach (ChoiceEntryHandle subChoice in subChoices) { choiceGroupElement.Add(subChoice.BuildElement()); }
            return choiceGroupElement;
        }

        protected override void UnhookElement(VisualElement detachingElement)
        {
            foreach (ChoiceEntryHandle subChoice in subChoices) { subChoice.DetachElement(); }
        }
        #endregion

        #region IUIChoice
        public void Highlight(bool enable)
        {
            foreach (ChoiceEntryHandle subChoice in subChoices) { subChoice.Highlight(false); }
            highlightedSubChoice = enable ? subChoices.FirstOrDefault() : null;
            highlightedSubChoice?.Highlight(true);
        }

        public void UseChoice() => highlightedSubChoice?.UseChoice();
        public void SetText(string text) { } // Groups have no text of their own (sub-choices do)
        #endregion

        #region IUIMoveInterceptor
        public bool TryMove(ControllerInputType controllerInputType, out bool isHighlightMove)
        {
            isHighlightMove = false;
            if (subChoices.Count == 0) { return false; }

            int direction = controllerInputType switch
            {
                ControllerInputType.NavigateLeft => -1,
                ControllerInputType.NavigateRight => 1,
                _ => 0
            };
            if (direction == 0) { return false; }

            int currentIndex = highlightedSubChoice != null ? subChoices.IndexOf(highlightedSubChoice) : -1;
            int nextIndex = currentIndex < 0 ? 0 : (currentIndex + direction + subChoices.Count) % subChoices.Count;
            highlightedSubChoice?.Highlight(false);
            highlightedSubChoice = subChoices[nextIndex];
            highlightedSubChoice.Highlight(true);
            isHighlightMove = true;
            return true;
        }
        #endregion
    }
}
