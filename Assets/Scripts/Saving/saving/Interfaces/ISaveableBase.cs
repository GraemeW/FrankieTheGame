namespace LowDefMustard.Saving
{
    public interface ISaveableBase
    {
        LoadPriority GetLoadPriority();
        bool IsCorePlayerState() => false;
        void ApplyFinishingTouches() { }
        SaveState CaptureState();
        void RestoreState(SaveState saveState);
}
}
