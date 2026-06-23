using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Frankie.Inventory;

namespace Frankie.Saving.Editor
{
    public class WearablesLinkSubCard : SaveableSubCardData
    {
        public WearablesLinkSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }
        
        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not WearablesLink wearablesLink) { return; }
            
            List<WearableItem> wearableItems = wearablesLink.ManualGetDataFromState(saveState);
            wearableItems ??= new List<WearableItem>();

            var listContainer = new VisualElement();
            subCardView.Add(listContainer);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(buttonRow);

            var addButton = new Button { text = "+ Add Slot" };
            buttonRow.Add(addButton);

            DrawWearableItemList(listContainer, wearablesLink, wearableItems);

            addButton.RegisterCallback<ClickEvent>(_ =>
            {
                wearableItems.Add(null);
                saveState = wearablesLink.ManualGetStateFromData(wearableItems);
                RaiseSaveStateChanged();
                DrawWearableItemList(listContainer, wearablesLink, wearableItems);
            });
        }

        private void DrawWearableItemList(VisualElement listContainer, WearablesLink wearablesLink, List<WearableItem> wearableItems)
        {
            listContainer.Clear();

            if (wearableItems.Count == 0)
            {
                listContainer.Add(new Label("No wearablesLink save data found"));
                return;
            }

            for (int i = 0; i < wearableItems.Count; i++)
            {
                int slotIndex = i;
                WearableItem wornItem = wearableItems[slotIndex];

                var slotRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                listContainer.Add(slotRow);

                slotRow.Add(new Label($"Slot {slotIndex}:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });

                var wornItemField = new ObjectField { objectType = typeof(WearableItem), value = wornItem, style = { flexGrow = 1 } };
                slotRow.Add(wornItemField);

                var removeSlotButton = new Button { text = "- Remove Slot" };
                slotRow.Add(removeSlotButton);

                wornItemField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newWearableItem = changeEvent.newValue as WearableItem;
                    wearableItems[slotIndex] = newWearableItem;
                    saveState = wearablesLink.ManualGetStateFromData(wearableItems);
                    RaiseSaveStateChanged();
                });

                removeSlotButton.RegisterCallback<ClickEvent>(_ =>
                {
                    wearableItems.RemoveAt(slotIndex);
                    saveState = wearablesLink.ManualGetStateFromData(wearableItems);
                    RaiseSaveStateChanged();
                    DrawWearableItemList(listContainer, wearablesLink, wearableItems);
                });
            }
        }
    }
}
