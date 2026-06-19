using UnityEngine.UIElements;

namespace Frankie.Saving.Editor
{
    public class SimpleFloatSaveableSubCard : SaveableSubCardData
    {
        public SimpleFloatSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }
        
        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not ISaveable<float> floatSaveable) { return; }
            
            float value = floatSaveable.ManualGetDataFromState(saveState);
            
            // TODO:  Add simple float editable field
            
            // Update editable field based on callback to update saveState via:
            // saveState = floatSaveable.ManualGetStateFromData(newValue);
        }
    }
}
