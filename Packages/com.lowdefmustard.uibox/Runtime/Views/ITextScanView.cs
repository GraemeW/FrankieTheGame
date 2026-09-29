using System;

namespace LowDefMustard.UIBox
{
    // Note:  If no ITextScanView component is present, TextScanBoxes fall back to the legacy uGUI implementation
    public interface ITextScanView
    {
        // Note:  Entries are created hidden -- TextScanBox reveals them in print-queue order
        ITextScanEntry CreateTextEntry(TextEntryType textEntryType);
        ITextScanChoiceEntry CreateChoiceEntry(string text, int choiceOrder, Action onChoose);
        void SetChoiceLayout(ChoiceLayout choiceLayout);
        void ClearEntries();
    }
}
