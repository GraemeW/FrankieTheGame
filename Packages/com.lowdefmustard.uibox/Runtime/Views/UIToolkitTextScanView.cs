using System;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Core;

namespace LowDefMustard.UIBox
{
    public sealed class UIToolkitTextScanView : UIToolkitBoxView
    {
        // UXML requirements:  a TextEntryContainer; optionally a ChoiceEntryContainer and BackExitButton
        
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
                LDMLog.Warning(LDMLogCategory.UI, $"Text scan view '{name}' failed to bind: its UXML requires a TextEntryContainer.", this);
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

        #region TextScanMethods
        // Note:  Entries are created hidden -> TextScanBox reveals them in print-queue order
        public TextEntryHandle CreateTextEntry(TextEntryType textEntryType)
        {
            var textEntryHandle = new TextEntryHandle(this, textEntryType);
            AddEntry(textEntryHandle);
            return textEntryHandle;
        }

        public ChoiceEntryHandle CreateChoiceEntry(string text, Action onChoose)
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
