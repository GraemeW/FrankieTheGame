using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Saving.Editor
{
    public class SimpleBoolSaveableSubCard : SaveableSubCardData
    {
        public SimpleBoolSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not ISaveable<bool> boolSaveable) { return; }

            if (!boolSaveable.TryManualGetDataFromState(saveState, out bool setEnabled))
            {
                subCardView.Add(new Label("No save data available"));
                return;
            }

            var boolRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(boolRow);

            boolRow.Add(new Label("Value:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            
            var boolField = new Toggle { value = setEnabled, style = { flexGrow = 1 } };
            boolRow.Add(boolField);

            boolField.RegisterValueChangedCallback(changeEvent =>
            {
                saveState = boolSaveable.ManualGetStateFromData(changeEvent.newValue);
                RaiseSaveStateChanged();
            });
        }
    }
}
