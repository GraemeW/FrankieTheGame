using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Saving.Editor
{
    public class SimpleBoolSaveableSubCard : SaveableSubCardData
    {
        public SimpleBoolSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }
        
        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not ISaveable<bool> boolSaveable) { return; }
            bool setEnabled = boolSaveable.ManualGetDataFromState(saveState);

            var boolRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(boolRow);

            boolRow.Add(new Label("Value:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            
            var boolField = new Toggle { value = setEnabled, style = { flexGrow = 1 } };
            boolRow.Add(boolField);

            boolField.RegisterValueChangedCallback(changeEvent =>
            {
                saveState = boolSaveable.ManualGetStateFromData(changeEvent.newValue);
            });
        }
    }
}
