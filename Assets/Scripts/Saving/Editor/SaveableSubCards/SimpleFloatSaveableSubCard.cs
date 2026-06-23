using UnityEngine;
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
            
            var floatRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(floatRow);
            
            floatRow.Add(new Label("Value:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            
            var floatField = new FloatField { value = value, style = { flexGrow = 1 } };
            floatRow.Add(floatField);

            floatField.RegisterValueChangedCallback(changeEvent =>
            {
                saveState = floatSaveable.ManualGetStateFromData(changeEvent.newValue);
                RaiseSaveStateChanged();
            });
        }
    }
}
