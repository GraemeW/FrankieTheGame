using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Frankie.Inventory;

namespace Frankie.Saving.Editor
{
    public class EquipmentSaveableSubCard : SaveableSubCardData
    {
        public EquipmentSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not Equipment equipment) { return; }
            
            Dictionary<EquipLocation, EquipableItem> equippedItems = equipment.ManualGetDataFromState(saveState);
            if (equippedItems == null || equippedItems.Count == 0)
            {
                subCardView.Add(new Label("No Equipment save data found"));
                return;
            }
            
            foreach (KeyValuePair<EquipLocation, EquipableItem> item in equippedItems)
            {
                var equipRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                subCardView.Add(equipRow);

                equipRow.Add(new Label($"{item.Key}:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });

                var equipField = new ObjectField { objectType = typeof(EquipableItem), value = item.Value, style = { flexGrow = 1 } };
                equipRow.Add(equipField);
                
                equipField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newEquipableItem = changeEvent.newValue as EquipableItem;
                    
                    Debug.Log($"Equipping {newEquipableItem} to {item.Key}");
                    if (!CanUpdateEquipment(newEquipableItem, item.Key))
                    {
                        equipField.SetValueWithoutNotify(equippedItems[item.Key]);
                        return;
                    }
                    equippedItems[item.Key] = newEquipableItem;
                    saveState = equipment.ManualGetStateFromData(equippedItems);
                    RaiseSaveStateChanged();
                });
            }
        }

        private bool CanUpdateEquipment(EquipableItem newEquipableItem, EquipLocation equipLocation)
        {
            if (newEquipableItem == null) { return true; } // Equip nothing
            
            if (newEquipableItem.GetEquipLocation() != equipLocation)
            {
                Debug.Log($"Invalid item {newEquipableItem} :: EquipLocation[{newEquipableItem.GetEquipLocation()}]");
                return false;
            }
                    
            if (saveableEntityCardData != null && saveableEntityCardData.TryGetSaveableSubCardData<Knapsack>(out SaveableSubCardData knapsackSubCardData))
            {
                // TODO:  Add inventory checks, pending inventory saveable subCard
                Debug.Log("Found the inventory!");
            }
            return true;
        }
    }
}
