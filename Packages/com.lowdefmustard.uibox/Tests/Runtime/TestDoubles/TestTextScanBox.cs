using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests
{
    public class TestTextScanBox : TextScanBox
    {
        // Note:  TextScanBox requires a UIToolkitTextScanView - without a loaded UI the view stays unbound
        //  - so entries exist as handles + models only, which is all the box logic needs
        
        // UI Testing Properties + Methods
        public int onBusyWritingCallCount;
        public bool? lastOnBusyWritingValue;

        protected override void OnBusyWriting(bool enable)
        {
            onBusyWritingCallCount++;
            lastOnBusyWritingValue = enable;
        }

        // Attributes
        public bool publicIsWriting => isWriting;
        public bool publicIsInitialInputBlocked => isInitialInputBlocked;
        public bool publicClearChoiceOptionsOnEnable => clearChoiceOptionsOnEnable;

        // Setters
        public void SetIsInitialInputBlocked(bool value) => isInitialInputBlocked = value;

        // Public Passthroughs
        public bool PublicTryFastForwardActiveText() => TryFastForwardActiveText();
        public void PublicSkipToEndOfPage() => SkipToEndOfPage();
        public bool PublicPrepareChooseAction(ControllerInputType input) => PrepareChooseAction(input);

        public TextEntryHandle PublicAddTextEntry(string text) // As AddText, handing back the entry it prints into
        {
            TextEntryHandle textEntry = textScanView.CreateTextEntry(TextEntryType.Simple);
            QueueTextForPrinting(textEntry, text);
            return textEntry;
        }

        public ChoiceEntryHandle PublicAddChoiceEntry(string text)
        {
            ChoiceEntryHandle choiceEntry = textScanView.CreateChoiceEntry(text, null);
            QueueChoiceForPrinting(choiceEntry);
            return choiceEntry;
        }
    }
}
