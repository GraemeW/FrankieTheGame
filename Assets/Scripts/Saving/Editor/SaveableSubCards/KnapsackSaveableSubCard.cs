using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Frankie.Inventory;
using UnityEngine;

namespace Frankie.Saving.Editor
{
    public class KnapsackSaveableSubCard : SaveableSubCardData
    {
        public KnapsackSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not Knapsack knapsack) { return; }
            
            ActiveInventoryItem[] itemsInKnapsack = knapsack.ManualGetDataFromState(saveState);
            if (itemsInKnapsack == null || itemsInKnapsack.Length == 0)
            {
                subCardView.Add(new Label("No Knapsack save data found"));
                return;
            }
            
            for (int i = 0; i < itemsInKnapsack.Length; i++)
            {
                int slotIndex = i;
                ActiveInventoryItem activeInventoryItem = itemsInKnapsack[slotIndex];

                InventoryItem inventoryItem = null;
                bool isEquipped = false;
                if (activeInventoryItem != null)
                {
                    inventoryItem = activeInventoryItem.GetInventoryItem();
                    isEquipped = activeInventoryItem.IsEquipped();
                }

                var slotRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                subCardView.Add(slotRow);

                slotRow.Add(new Label($"Slot {slotIndex}:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });

                var itemField = new ObjectField { objectType = typeof(InventoryItem), value = inventoryItem, style = { flexGrow = 1 } };
                slotRow.Add(itemField);

                var equippedField = new Toggle { value = isEquipped, style = { width = 80 } };
                slotRow.Add(equippedField);

                itemField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newInventoryItem = changeEvent.newValue as InventoryItem;
                    var updatedItem = newInventoryItem != null ? new ActiveInventoryItem(newInventoryItem) : null;
                    if (updatedItem != null) { updatedItem.SetEquipped(equippedField.value); }
                    itemsInKnapsack[slotIndex] = updatedItem;
                    saveState = knapsack.ManualGetStateFromData(itemsInKnapsack);
                    RaiseSaveStateChanged();
                });

                equippedField.RegisterValueChangedCallback(changeEvent =>
                {
                    if (itemsInKnapsack[slotIndex] == null)
                    {
                        equippedField.SetValueWithoutNotify(false);
                        return;
                    }
                    itemsInKnapsack[slotIndex].SetEquipped(changeEvent.newValue);
                    saveState = knapsack.ManualGetStateFromData(itemsInKnapsack);
                    RaiseSaveStateChanged();
                });
            }
        }
    }
}
