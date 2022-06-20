namespace Frankie.Saving
{
    public interface ISaveable
    {
        LoadPriority GetLoadPriority();
        SaveState CaptureState();
        void RestoreState(SaveState saveState);
    }
}