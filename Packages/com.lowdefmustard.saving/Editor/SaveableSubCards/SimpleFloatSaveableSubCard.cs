using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Saving.Editor
{
    public class SimpleFloatSaveableSubCard : SaveableSubCardData
    {
        public SimpleFloatSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not ISaveable<float> floatSaveable) { return; }

            if (!floatSaveable.TryManualGetDataFromState(saveState, out float value))
            {
                subCardView.Add(new Label("No save data available"));
                return;
            }
            
            var floatRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(floatRow);
            
            floatRow.Add(new Label("Value:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            
            var floatField = new FloatField { value = value, isDelayed = true, style = { flexGrow = 1 } };
            floatRow.Add(floatField);

            floatField.RegisterValueChangedCallback(changeEvent =>
            {
                saveState = floatSaveable.ManualGetStateFromData(changeEvent.newValue);
                RaiseSaveStateChanged();
            });
        }
    }
}
