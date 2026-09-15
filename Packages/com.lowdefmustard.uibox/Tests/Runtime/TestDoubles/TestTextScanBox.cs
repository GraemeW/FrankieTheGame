using System.Reflection;
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
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = typeof(TextScanBox).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) { field.SetValue(target, value); }
        }
        
        // Note - simpleTextPrefab/speechTextPrefab/initialInputDelay/delayBetweenCharacters are all private, access via reflection
        public void SetTunables(GameObject simpleTextPrefabRef = null, GameObject speechTextPrefabRef = null, float initialInputDelay = 0f, float delayBetweenCharacters = 0f)
        {
            SetPrivateField(this, "simpleTextPrefab", simpleTextPrefabRef);
            SetPrivateField(this, "speechTextPrefab", speechTextPrefabRef);
            SetPrivateField(this, "initialInputDelay", initialInputDelay);
            SetPrivateField(this, "delayBetweenCharacters", delayBetweenCharacters);
        }
        
        // Note - printedJobs is private, where PrintText populates it one character per frame
        //  -> Tests that only care about ClearOldDialogue's cleanup can seed it directly instead of running a full typewriter pass
        public void SetPrintedJobsDirectly(System.Collections.Generic.List<GameObject> jobs) => SetPrivateField(this, "printedJobs", jobs);
        
        // Public Passthroughs
        public bool PublicTryFastForwardActiveText() => TryFastForwardActiveText();
        public void PublicSkipToEndOfPage() => SkipToEndOfPage();
        public void PublicQueueTextForPrinting(GameObject textObject, string text, bool isChoice) => QueueTextForPrinting(textObject, text, isChoice);
        public bool PublicPrepareChooseAction(ControllerInputType input) => PrepareChooseAction(input);
        



        
        
    }
}
