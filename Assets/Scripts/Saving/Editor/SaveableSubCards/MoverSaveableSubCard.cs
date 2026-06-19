using UnityEngine.UIElements;
using Frankie.Control;

namespace Frankie.Saving.Editor
{
    public class MoverSaveableSubCard : SaveableSubCardData
    {
        public MoverSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not Mover mover) { return; }
            
            SerializableVector2 savedPosition = mover.ManualGetDataFromState(saveState);
            if (savedPosition == null)
            {
                // TODO:  Add label noting no position currently saved
                // Note - slightly different behaviour for this component, because we don't want to have edit capabilities for Mover position if state not already set
                return;
            }
            
            // TODO:  Add editable fields
            
            // TODO:  Update editable field callback to update saveState via
            // Vector3 updatedPosition = new(xUpdated, yUpdated, 0f);
            // SerializableVector2 serializablePosition = new SerializableVector2(updatedPosition);
            // saveState = vector2Saveable.ManualGetStateFromData(serializablePosition);
        }
    }
}