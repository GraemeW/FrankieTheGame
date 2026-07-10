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
        
        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        { 
            if (saveable is not CombatParticipant combatParticipant) { return; }
            if (!combatParticipant.TryManualGetDataFromState(saveState, out CombatParticipantSaveData saveData))
            {
                subCardView.Add(new Label("No CombatParticipant save data available"));
                return;
            }
            
            bool isDead = saveData.isDead;
            float currentHP = saveData.hpRatio;
            float currentAP = saveData.apRatio;
            
            var isDeadRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(isDeadRow);
            isDeadRow.Add(new Label("Is Dead:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var isDeadField = new Toggle { value = isDead, style = { flexGrow = 1 } };
            isDeadRow.Add(isDeadField);

            var currentHPRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(currentHPRow);
            currentHPRow.Add(new Label("Current HP Ratio:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var currentHPField = new FloatField { value = currentHP, isDelayed = true, style = { flexGrow = 1 } };
            currentHPRow.Add(currentHPField);
            var currentHPSlider = new Slider(0f, 1f) { value = currentHP, style = { flexGrow = 2, marginLeft = 4 } };
            currentHPRow.Add(currentHPSlider);

            var currentAPRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(currentAPRow);
            currentAPRow.Add(new Label("Current A Ratio:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var currentAPField = new FloatField { value = currentAP, isDelayed = true, style = { flexGrow = 1 } };
            currentAPRow.Add(currentAPField);
            var currentAPSlider = new Slider(0f, 1f) { value = currentAP, style = { flexGrow = 2, marginLeft = 4 } };
            currentAPRow.Add(currentAPSlider);

            if (float.IsPositiveInfinity(currentAP))
            {
                currentAPField.SetEnabled(false);
                currentAPSlider.SetEnabled(false);
            }
            
            var setHPAPToMaxButton = new Button { text = "HP/AP -> Max", style = { width = smallButtonWidth } };
            subCardView.Add(setHPAPToMaxButton);

            // Callbacks
            isDeadField.RegisterValueChangedCallback(changeEvent =>
            {
                isDead = changeEvent.newValue;
                var updatedSaveData = new CombatParticipantSaveData(isDead, currentHP, currentAP);
                saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });

            currentHPField.RegisterValueChangedCallback(changeEvent =>
            {
                currentHP = Mathf.Clamp01(changeEvent.newValue);
                if (!Mathf.Approximately(currentHP, changeEvent.newValue))
                {
                    currentHPField.SetValueWithoutNotify(currentHP);
                }
                currentHPSlider.SetValueWithoutNotify(currentHP);

                var updatedSaveData = new CombatParticipantSaveData(isDead, currentHP, currentAP);
                saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });

            currentAPField.RegisterValueChangedCallback(changeEvent =>
            {
                currentAP = Mathf.Clamp01(changeEvent.newValue);
                if (!Mathf.Approximately(currentAP, changeEvent.newValue))
                {
                    currentAPField.SetValueWithoutNotify(currentAP);
                }
                currentAPSlider.SetValueWithoutNotify(currentAP);

                var updatedSaveData = new CombatParticipantSaveData(isDead, currentHP, currentAP);
                saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });

            currentHPSlider.RegisterValueChangedCallback(changeEvent =>
            {
                currentHP = changeEvent.newValue;
                currentHPField.SetValueWithoutNotify(currentHP);

                var updatedSaveData = new CombatParticipantSaveData(isDead, currentHP, currentAP);
                saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });

            currentAPSlider.RegisterValueChangedCallback(changeEvent =>
            {
                currentAP = changeEvent.newValue;
                currentAPField.SetValueWithoutNotify(currentAP);

                var updatedSaveData = new CombatParticipantSaveData(isDead, currentHP, currentAP);
                saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });
            
            setHPAPToMaxButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (saveableEntityCardData == null || !saveableEntityCardData.TryGetSaveableSubCardData(out BaseStatsSubCard baseStatsSubCard)) { return; }
                
                float maxHP = baseStatsSubCard.GetMaxHP();
                float maxAP = baseStatsSubCard.GetMaxAP();
                if (Mathf.Approximately(currentHP, maxHP) && Mathf.Approximately(currentAP, maxAP)) { return; }
                
                if (maxHP > 0f)
                {
                    currentHP = Mathf.Clamp01(maxHP);
                    currentHPField.SetValueWithoutNotify(currentHP);
                    currentHPSlider.SetValueWithoutNotify(currentHP);
                }
                
                if (maxAP > 0f && !float.IsPositiveInfinity(currentAP))
                {
                    currentAP = Mathf.Clamp01(maxAP);
                    currentAPField.SetValueWithoutNotify(currentAP);
                    currentAPSlider.SetValueWithoutNotify(currentAP);
                }
                
                var updatedSaveData = new CombatParticipantSaveData(isDead, currentHP, currentAP);
                saveState = combatParticipant.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });
        }
    }
}
