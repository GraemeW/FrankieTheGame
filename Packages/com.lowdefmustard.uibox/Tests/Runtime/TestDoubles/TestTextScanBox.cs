using UnityEngine;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests
{
    public class TestTextScanBox : TextScanBox
    {
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
        public Transform dialogueParentForTest => dialogueParent;

        // Setters
        public void SetIsInitialInputBlocked(bool value) => isInitialInputBlocked = value;
        public void SetDialogueParent(Transform value) => dialogueParent = value;
        public void SetOptionParent(Transform value) => optionParent = value;

        // Public Passthroughs
        public bool PublicTryFastForwardActiveText() => TryFastForwardActiveText();
        public void PublicSkipToEndOfPage() => SkipToEndOfPage();
        public void PublicQueueTextForPrinting(GameObject textObject, string text, bool isChoice) => QueueTextForPrinting(textObject, text, isChoice);
        public bool PublicPrepareChooseAction(ControllerInputType input) => PrepareChooseAction(input);
    }
}
