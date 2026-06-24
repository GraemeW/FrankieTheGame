using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Stats;

namespace Frankie.Saving.Editor
{
    public class BaseStatsSubCard : SaveableSubCardData
    {
        public BaseStatsSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }
        
        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not BaseStats baseStats) { return; }
            
            // Level Increment
            BaseStatsSaveData baseStatsSaveData = baseStats.ManualGetDataFromState(saveState);
            if (baseStatsSaveData == null)
            {
                subCardView.Add(new Label("No BaseStats save data found"));
                return;
            }
            
            var incrementLevelButton = new Button{ text = "Increment Level" , style = { width = standardButtonWidth } };
            subCardView.Add(incrementLevelButton);
            
            var spacer = new VisualElement { style = { height = 20 } };
            subCardView.Add(spacer);
            
            // Stat Sheet
            int level = baseStatsSaveData.level;
            Dictionary<Stat, float> statSheet = baseStatsSaveData.statSheet ?? new Dictionary<Stat, float>();

            var levelRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(levelRow);
            levelRow.Add(new Label("Level:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var levelField = new IntegerField { value = level, isDelayed = true, style = { flexGrow = 1 } };
            levelRow.Add(levelField);

            levelField.RegisterValueChangedCallback(changeEvent =>
            {
                level = changeEvent.newValue;
                var updatedSaveData = new BaseStatsSaveData(level, statSheet);
                saveState = baseStats.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });

            Dictionary<Stat, FloatField> statFloatFields = new Dictionary<Stat, FloatField>();
            foreach (Stat stat in new List<Stat>(statSheet.Keys))
            {
                var statRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                subCardView.Add(statRow);

                statRow.Add(new Label($"{stat}:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });

                statFloatFields[stat] = new FloatField { value = statSheet[stat], isDelayed = true, style = { flexGrow = 1 } };
                statRow.Add(statFloatFields[stat]);

                statFloatFields[stat].RegisterValueChangedCallback(changeEvent =>
                {
                    statSheet[stat] = changeEvent.newValue;
                    var updatedSaveData = new BaseStatsSaveData(level, statSheet);
                    saveState = baseStats.ManualGetStateFromData(updatedSaveData);
                    RaiseSaveStateChanged();
                });
            }
            
            incrementLevelButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (baseStats == null) { return; }
                Dictionary<Stat, float> levelUpSheet = baseStats.ManualGetLevelUpSheet(level, statSheet);

                level++;
                levelField.SetValueWithoutNotify(level);
                foreach (KeyValuePair<Stat, float> statValuePair in levelUpSheet)
                {
                    statSheet[statValuePair.Key] += statValuePair.Value;
                    statFloatFields[statValuePair.Key].SetValueWithoutNotify(statSheet[statValuePair.Key]);
                }
                
                var updatedSaveData = new BaseStatsSaveData(level, statSheet);
                saveState = baseStats.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });
        }
    }
}
