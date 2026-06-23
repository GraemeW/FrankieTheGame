using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Saving.Editor
{
    public class SimpleIntSaveableSubCard : SaveableSubCardData
    {
        public SimpleIntSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not ISaveable<int> intSaveable) { return; }
            int value = intSaveable.ManualGetDataFromState(saveState);
            
            var intRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(intRow);
            
            intRow.Add(new Label("Value:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            
            var intField = new IntegerField { value = value, style = { flexGrow = 1 } };
            intRow.Add(intField);
            
            intField.RegisterValueChangedCallback(changeEvent =>
            {
                saveState = intSaveable.ManualGetStateFromData(changeEvent.newValue);
                RaiseSaveStateChanged();
            });
        }
    }
}
