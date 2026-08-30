using UnityEngine;

namespace LowDefMustard.Saving.Tests
{
    // Test double for ISaveableBase -- for CaptureState/RestoreState/ApplyFinishingTouches test behaviour
    // Note:  captureStateReturns == null means CaptureState() itself returns null - component gets skipped
    
    public class TestSaveableComponent : MonoBehaviour, ISaveableBase
    {
        public LoadPriority loadPriority = LoadPriority.ObjectProperty;
        public bool isCorePlayerState = false;
        public object captureStateReturns = null;
        public bool restoreStateWasCalled = false;
        public SaveState restoreStateReceivedValue = null;
        public bool applyFinishingTouchesWasCalled = false;

        public LoadPriority GetLoadPriority() => loadPriority;
        public bool IsCorePlayerState() => isCorePlayerState;
        public void ApplyFinishingTouches() => applyFinishingTouchesWasCalled = true;

        public SaveState CaptureState() =>
            captureStateReturns == null ? null : new SaveState(loadPriority, captureStateReturns);

        public void RestoreState(SaveState saveState)
        {
            restoreStateWasCalled = true;
            restoreStateReceivedValue = saveState;
        }
    }
}
