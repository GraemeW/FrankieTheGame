namespace Frankie.Saving.Editor
{
    public class SaveableSubCardData
    {
        public ISaveable saveable { get; private set; }
        public SaveState saveState { get; private set; }

        public SaveableSubCardData(ISaveable saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }
        
        public void UpdateSaveState(SaveState newSaveState)
        {
            saveState = newSaveState;
        }
    }
}
