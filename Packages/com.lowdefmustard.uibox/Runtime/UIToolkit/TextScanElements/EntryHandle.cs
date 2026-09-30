using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    public sealed class EntryHandle : ITextScanChoiceEntry, IUIChoice
    {
        // State
        private readonly UIToolkitTextScanView view;
        private readonly TextEntryModel model;
        private readonly TextEntryType textEntryType;
        private readonly Action onChoose;
        private VisualElement element;
        public bool isRemoved { get; set; } = false;
        public bool isChoice => model is ChoiceEntryModel;

        public EntryHandle(UIToolkitTextScanView view, TextEntryModel model, TextEntryType textEntryType, Action onChoose)
        {
            this.view = view;
            this.model = model;
            this.textEntryType = textEntryType;
            this.onChoose = onChoose;
        }

        #region ITextScanChoiceEntry
        public bool isAlive => !isRemoved && view != null;
        public bool canDisplayText => true;
        public void Reveal() => model.isRevealed = true;
        public void SetText(string text) => model.text = text;
        public void Highlight(bool enable)
        {
            if (model is ChoiceEntryModel choiceEntryModel) { choiceEntryModel.isHighlighted = enable; }
        }

        public void Remove()
        {
            if (isRemoved) { return; }
            isRemoved = true;
            if (view != null) { view.RemoveEntry(this); }
        }
        #endregion

        #region IUIChoice
        public void UseChoice() => HandleClicked();

        public bool TryGetScreenRect(Camera renderCamera, out Rect screenRect)
        {
            // Note:  Panel space (y down) flipped to screen convention (y up) - uniform panel scaling preserves relative geometry
            screenRect = default;
            if (element?.panel == null) { return false; }
            Rect worldBound = element.worldBound;
            screenRect = new Rect(worldBound.x, -worldBound.yMax, worldBound.width, worldBound.height);
            return true;
        }
        #endregion

        #region ElementHandling
        public VisualElement BuildElement()
        {
            DetachElement();
            if (isChoice)
            {
                var choiceEntryElement = new ChoiceEntryElement();
                choiceEntryElement.clicked += HandleClicked;
                element = choiceEntryElement;
            }
            else
            {
                element = new TextEntryElement(textEntryType);
            }
            element.dataSource = model;
            return element;
        }

        public void DetachElement()
        {
            if (element == null) { return; }
            if (element is ChoiceEntryElement choiceEntryElement) { choiceEntryElement.clicked -= HandleClicked; }
            element.RemoveFromHierarchy();
            element = null;
        }

        private void HandleClicked()
        {
            if (isRemoved) { return; }
            onChoose?.Invoke();
        }
        #endregion
    }
}
