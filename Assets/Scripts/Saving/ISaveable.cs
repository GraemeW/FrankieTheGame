namespace Frankie.Saving
{
    public interface ISaveable<T> : ISaveableBase
    {
        SaveState ManualGetStateFromData(T data);
        public T ManualGetDataFromState(SaveState saveState);
    }
}
