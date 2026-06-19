namespace Frankie.Saving
{
    public interface ISaveableBase
    {
        LoadPriority GetLoadPriority();
        bool IsCorePlayerState() => false;
        SaveState CaptureState();
        void RestoreState(SaveState saveState);
    }
}
