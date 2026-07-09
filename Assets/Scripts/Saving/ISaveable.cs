namespace Frankie.Saving
{
    public interface ISaveable<T> : ISaveableBase
    {
        SaveState ManualGetStateFromData(T data);
        public bool TryManualGetDataFromState(SaveState saveState, out T value);
    }
}
