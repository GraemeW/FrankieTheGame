using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Control;
using Frankie.Utils;

namespace Frankie.Saving.Editor
{
    public class MoverSaveableSubCard : SaveableSubCardData
    {
        public MoverSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not Mover mover) { return; }
            
            SerializableVector2 savedPosition = mover.ManualGetDataFromState(saveState);
            if (savedPosition == null)
            {
                // Note - slightly different behaviour for this component
                // We don't want to have edit capabilities for Mover position if state not already set
                subCardView.Add(new Label("No position currently saved"));
                return;
            }

            float xPosition = savedPosition.x;
            float yPosition = savedPosition.y;

            var xRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(xRow);
            xRow.Add(new Label("X:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var xField = new FloatField { value = xPosition, style = { flexGrow = 1 } };
            xRow.Add(xField);

            var yRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(yRow);
            yRow.Add(new Label("Y:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var yField = new FloatField { value = yPosition, style = { flexGrow = 1 } };
            yRow.Add(yField);

            xField.RegisterValueChangedCallback(changeEvent =>
            {
                xPosition = changeEvent.newValue;
                Vector3 updatedPosition = new(xPosition, yPosition, 0f);
                var serializablePosition = new SerializableVector2(updatedPosition);
                saveState = mover.ManualGetStateFromData(serializablePosition);
            });

            yField.RegisterValueChangedCallback(changeEvent =>
            {
                yPosition = changeEvent.newValue;
                Vector3 updatedPosition = new(xPosition, yPosition, 0f);
                var serializablePosition = new SerializableVector2(updatedPosition);
                saveState = mover.ManualGetStateFromData(serializablePosition);
            });
        }
    }
}
