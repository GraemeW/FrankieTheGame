using UnityEngine;
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
                subCardView.Add(new Label("No CombatParticipant save data found"));
                return;
            }
            
            bool isDead = saveData.isDead;
            float currentHP = saveData.currentHP;
            float currentAP = saveData.currentAP;

            var isDeadRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(isDeadRow);
            isDeadRow.Add(new Label("Is Dead:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var isDeadField = new Toggle { value = isDead, style = { flexGrow = 1 } };
            isDeadRow.Add(isDeadField);

            var currentHPRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(currentHPRow);
            currentHPRow.Add(new Label("Current HP:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var currentHPField = new FloatField { value = currentHP, style = { flexGrow = 1 } };
            currentHPRow.Add(currentHPField);

            var currentAPRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(currentAPRow);
            currentAPRow.Add(new Label("Current AP:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var currentAPField = new FloatField { value = currentAP, style = { flexGrow = 1 } };
            currentAPRow.Add(currentAPField);

            isDeadField.RegisterValueChangedCallback(changeEvent =>
            {
                isDead = changeEvent.newValue;
                var updatedSaveData = new CombatParticipantSaveData(isDead, currentHP, currentAP);
                saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
            });

            currentHPField.RegisterValueChangedCallback(changeEvent =>
            {
                currentHP = changeEvent.newValue;
                var updatedSaveData = new CombatParticipantSaveData(isDead, currentHP, currentAP);
                saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
            });

            currentAPField.RegisterValueChangedCallback(changeEvent =>
            {
                currentAP = changeEvent.newValue;
                var updatedSaveData = new CombatParticipantSaveData(isDead, currentHP, currentAP);
                saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
            });
        }
    }
}
