using UnityEngine.UIElements;
using Frankie.Combat;

namespace Frankie.Saving.Editor
{
    public class CombatParticipantSaveableSubCard : SaveableSubCardData
    {
        public CombatParticipantSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        { 
            if (saveable is not CombatParticipant combatParticipant) { return; }
            
            CombatParticipantSaveData saveData = combatParticipant.ManualGetDataFromState(saveState);
            if (saveData == null)
            {
                // TODO:  Add label to note issue in loading combatParticipant save
                return;
            }
            
            bool isDead = saveData.isDead;
            float currentHP = saveData.currentHP;
            float currentAP = saveData.currentAP;
            
            // TODO:  Add editable fields for isDead, currentHP, currentAP
            
            // Update editable field callbacks to update saveState via:
            // var updatedSaveData = new CombatParticipantSaveData(newIsDead, newCurrentHP, newCurrentAP);
            // saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
        }
    }
}