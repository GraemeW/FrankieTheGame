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
            
            BaseStatsSaveData baseStatsSaveData = baseStats.ManualGetDataFromState(saveState);
            if (baseStatsSaveData == null)
            {
                subCardView.Add(new Label("No BaseStats save data found"));
                return;
            }

            int level = baseStatsSaveData.level;
            Dictionary<Stat, float> statSheet = baseStatsSaveData.statSheet ?? new Dictionary<Stat, float>();

            var levelRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(levelRow);
            levelRow.Add(new Label("Level:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var levelField = new IntegerField { value = level, style = { flexGrow = 1 } };
            levelRow.Add(levelField);

            levelField.RegisterValueChangedCallback(changeEvent =>
            {
                level = changeEvent.newValue;
                var updatedSaveData = new BaseStatsSaveData(level, statSheet);
                saveState = baseStats.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });

            foreach (Stat stat in new List<Stat>(statSheet.Keys))
            {
                var statRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                subCardView.Add(statRow);

                statRow.Add(new Label($"{stat}:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });

                var statField = new FloatField { value = statSheet[stat], style = { flexGrow = 1 } };
                statRow.Add(statField);

                statField.RegisterValueChangedCallback(changeEvent =>
                {
                    statSheet[stat] = changeEvent.newValue;
                    var updatedSaveData = new BaseStatsSaveData(level, statSheet);
                    saveState = baseStats.ManualGetStateFromData(updatedSaveData);
                    RaiseSaveStateChanged();
                });
            }
        }
    }
}
