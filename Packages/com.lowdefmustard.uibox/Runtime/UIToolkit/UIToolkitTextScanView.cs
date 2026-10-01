using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // UXML requirements:  a TextEntryContainer; optionally a ChoiceEntryContainer and BackExitButton
    public sealed class UIToolkitTextScanView : UIToolkitBoxView, ITextScanView
    {
        // State
        private ChoiceLayout choiceLayout = ChoiceLayout.Horizontal;

        // Cached References
        private TextEntryContainer textEntryContainer;

        #region UIToolkitBoxView
        protected override bool TryBindContent(VisualElement rootElement)
        {
            textEntryContainer = rootElement.Q<TextEntryContainer>();
            if (textEntryContainer == null)
            {
                Debug.LogWarning($"UIToolkitTextScanView[{name}] failed to bind:  UXML requires a TextEntryContainer.");
                return false;
            }
            ApplyChoiceLayout();
            return true;
        }

        protected override void UnbindContent()
        {
            textEntryContainer = null;
        }

        #endregion

        #region ITextScanView
        public ITextScanEntry CreateTextEntry(TextEntryType textEntryType)
        {
            var textEntryHandle = new TextEntryHandle(this, textEntryType);
            AddEntry(textEntryHandle);
            return textEntryHandle;
        }

        public ITextScanChoiceEntry CreateChoiceEntry(string text, int choiceOrder, Action onChoose)
        {
            var choiceEntryHandle = new ChoiceEntryHandle(this, text, false, onChoose);
            AddEntry(choiceEntryHandle);
            return choiceEntryHandle;
        }

        public void SetChoiceLayout(ChoiceLayout setChoiceLayout)
        {
            choiceLayout = setChoiceLayout;
            ApplyChoiceLayout();
        }
        #endregion

        #region PrivateMethods
        private void ApplyChoiceLayout()
        {
            choiceEntryContainer?.SetLayout(choiceLayout);
        }
        #endregion
    }
}
