using System;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Test double for ISaveable<T>:  enables data<->SaveState conversion (e.g. for Simple*SaveableSubCard editor views) without a real gameplay component
    public class TestGenericSaveable<T> : ISaveable<T>
    {
        public LoadPriority loadPriority = LoadPriority.ObjectProperty;
        public Func<SaveState, (bool success, T value)> tryManualGetDataFromState = _ => (true, default);
        public Func<T, SaveState> manualGetStateFromDataOverride = null;

        public LoadPriority GetLoadPriority() => loadPriority;
        public SaveState CaptureState() => new SaveState(loadPriority, default(T));
        public void RestoreState(SaveState saveState) { }

        public SaveState ManualGetStateFromData(T data) =>
            manualGetStateFromDataOverride != null ? manualGetStateFromDataOverride(data) : new SaveState(loadPriority, data);

        public bool TryManualGetDataFromState(SaveState saveState, out T value)
        {
            (bool success, T result) = tryManualGetDataFromState(saveState);
            value = result;
            return success;
        }
    }
}
