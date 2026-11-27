using UnityEngine;

namespace LowDefMustard.Saving.Tests
{
    public class TestTypedSaveableComponent : MonoBehaviour, ISaveable<int>
    {
        public int value = 0;

        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
        public SaveState CaptureState() => ManualGetStateFromData(value);
        public void RestoreState(SaveState saveState) { if (TryManualGetDataFromState(saveState, out int restoredValue)) { value = restoredValue; } }

        public SaveState ManualGetStateFromData(int data) => new(GetLoadPriority(), data);
        public bool TryManualGetDataFromState(SaveState saveState, out int data) => saveState.TryGetState(out data);
    }
}
