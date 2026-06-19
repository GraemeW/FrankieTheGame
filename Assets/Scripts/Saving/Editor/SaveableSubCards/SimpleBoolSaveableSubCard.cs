using UnityEngine.UIElements;

namespace Frankie.Saving.Editor
{
    public class SimpleBoolSaveableSubCard : SaveableSubCardData
    {
        public SimpleBoolSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }
        
        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not ISaveable<bool> boolSaveable) { return; }
            
            bool setEnabled = boolSaveable.ManualGetDataFromState(saveState);
            
            // TODO:  Add simple bool editable field
            
            // Update editable field callback to update saveState via:
            // saveState = boolSaveable.ManualGetStateFromData(newValue);
        }
    }
}
