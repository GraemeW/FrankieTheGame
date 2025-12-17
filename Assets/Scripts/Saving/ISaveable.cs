namespace Frankie.Saving
{
    public interface ISaveable
    {
        LoadPriority GetLoadPriority();
        bool IsCorePlayerState() => false;
        SaveState CaptureState();
        void RestoreState(SaveState saveState);
    }
}
