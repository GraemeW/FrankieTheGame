namespace Frankie.Saving
{
    public interface ISaveable<T> : ISaveableBase
    {
        SaveState ManualGetStateFromData(T data);
        public bool TryManualGetDataFromState(SaveState saveState, out T value);
            // bool return::  signifies valid data on value (including default data)
            // Only set to false on null or invalid data -- see Mover, BaseStats e.g.
    }
}
