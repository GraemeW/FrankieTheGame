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
        
        private readonly Dictionary<Stat, float> statSheet = new();
        public float GetMaxHP() => statSheet.GetValueOrDefault(Stat.HP, -1f);
        public float GetMaxAP() => statSheet.GetValueOrDefault(Stat.AP, -1f);
        
        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not BaseStats baseStats) { return; }
            
            BaseStatsSaveData baseStatsSaveData = baseStats.ManualGetDataFromState(saveState);
            if (baseStatsSaveData == null)
            {
                subCardView.Add(new Label("No BaseStats save data found"));
                return;
            }
            
            int level = baseStatsSaveData.level;
            statSheet.Clear();
            foreach (var pair in baseStatsSaveData.statSheet) { statSheet[pair.Key] = pair.Value; }
            
            // Level Adjustment
            int resetLevel = 1;
            var resetLevelRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            resetLevelRow.Add(new Label("Reset Level:") { style = { width = 100, unityTextAlign = TextAnchor.MiddleLeft } });
            subCardView.Add(resetLevelRow);
            var resetLevelButton = new Button { text = "Reset To:", style = { width = smallButtonWidth }};
            resetLevelRow.Add(resetLevelButton);
            var resetLevelField = new IntegerField { value = resetLevel, isDelayed = true, style = { width = 100 } };
            resetLevelRow.Add(resetLevelField);
            
            var incrementLevelRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            incrementLevelRow.Add(new Label("Increment Level:") { style = { width = 100, unityTextAlign = TextAnchor.MiddleLeft } });
            subCardView.Add(incrementLevelRow);
            var incrementLevelButton = new Button{ text = "Increment" , style = { width = smallButtonWidth } };
            incrementLevelRow.Add(incrementLevelButton);
            
            var spacer = new VisualElement { style = { height = 20 } };
            subCardView.Add(spacer);
            
            // Stat Sheet
            var levelRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(levelRow);
            levelRow.Add(new Label("Level:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var levelField = new IntegerField { value = level, isDelayed = true, style = { flexGrow = 1 } };
            levelRow.Add(levelField);

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
            
            // Callbacks
            resetLevelField.RegisterValueChangedCallback(changeEvent => resetLevel = changeEvent.newValue);

            resetLevelButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (baseStats == null) { return; }
                ResetStats();

                for (int currentLevel = 1; currentLevel < resetLevel; currentLevel++)
                {
                    Dictionary<Stat, float> levelUpSheet = baseStats.ManualGetLevelUpSheet(level, statSheet);
                    IncrementLevel(levelUpSheet);
                }
                
                var updatedSaveData = new BaseStatsSaveData(level, statSheet);
                saveState = baseStats.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });
            
            levelField.RegisterValueChangedCallback(changeEvent =>
            {
                level = changeEvent.newValue;
                var updatedSaveData = new BaseStatsSaveData(level, statSheet);
                saveState = baseStats.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });
            
            incrementLevelButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (baseStats == null) { return; }
                
                Dictionary<Stat, float> levelUpSheet = baseStats.ManualGetLevelUpSheet(level, statSheet);
                IncrementLevel(levelUpSheet);
                
                var updatedSaveData = new BaseStatsSaveData(level, statSheet);
                saveState = baseStats.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });

            return;

            // Local Functions
            void ResetStats()
            {
                if (baseStats == null) { return; }
                Dictionary<Stat, float> baseStatSheet = baseStats.ManualGetBaseStatSheet();
                
                level = 1;
                levelField.SetValueWithoutNotify(level);
                foreach (KeyValuePair<Stat, float> statValuePair in baseStatSheet)
                {
                    statSheet[statValuePair.Key] = statValuePair.Value;
                    statFloatFields[statValuePair.Key].SetValueWithoutNotify(statSheet[statValuePair.Key]);
                }
            }
            
            void IncrementLevel(Dictionary<Stat, float> levelUpSheet)
            {
                level++;
                levelField.SetValueWithoutNotify(level);
                foreach (KeyValuePair<Stat, float> statValuePair in levelUpSheet)
                {
                    statSheet[statValuePair.Key] += statValuePair.Value;
                    statFloatFields[statValuePair.Key].SetValueWithoutNotify(statSheet[statValuePair.Key]);
                }
            }
        }
    }
}
