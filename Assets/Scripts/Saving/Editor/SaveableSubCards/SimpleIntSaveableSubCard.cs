using UnityEngine.UIElements;

namespace Frankie.Saving.Editor
{
    public class SimpleIntSaveableSubCard : SaveableSubCardData
    {
        public SimpleIntSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not ISaveable<int> intSaveable) { return; }
            
            int value = intSaveable.ManualGetDataFromState(saveState);
            
            // TODO:  Add simple int editable field
            
            // Update editable field based on callback to update saveState via:
            // saveState = intSaveable.ManualGetStateFromData(newValue);
        }
    }
}
