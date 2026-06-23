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
                    var oldEquipableItem = changeEvent.previousValue as EquipableItem;
                    var newEquipableItem = changeEvent.newValue as EquipableItem;
                    
                    if (!CanUpdateEquipment(oldEquipableItem, newEquipableItem, item.Key))
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

        public void UnequipItemFromLocation(EquipLocation equipLocation)
        {
            if (saveable is not Equipment equipment) { return; }
            
            Dictionary<EquipLocation, EquipableItem> equippedItems = equipment.ManualGetDataFromState(saveState);
            equippedItems[equipLocation] = null;
            
            saveState = equipment.ManualGetStateFromData(equippedItems);
            RaiseSaveStateChanged();
            Redraw();
        }

        private void ReconcileKnapsackUnequip(EquipableItem equipableItemToRemove)
        {
            if (saveableEntityCardData == null) { return; }
            if (!saveableEntityCardData.TryGetSaveableSubCardData(out KnapsackSaveableSubCard knapsackSaveableSubCard)) { return; }
            
            knapsackSaveableSubCard.UnequipItem(equipableItemToRemove);
        }

        private bool CanUpdateEquipment(EquipableItem oldEquipableItem, EquipableItem newEquipableItem, EquipLocation equipLocation)
        {
            if (newEquipableItem == null)
            {
                if (oldEquipableItem != null) { ReconcileKnapsackUnequip(oldEquipableItem); }
                return true;
            }
            
            if (newEquipableItem.GetEquipLocation() != equipLocation)
            {
                Debug.Log($"Invalid item {newEquipableItem} :: EquipLocation[{newEquipableItem.GetEquipLocation()}]");
                return false;
            }
                    
            if (saveableEntityCardData != null && saveableEntityCardData.TryGetSaveableSubCardData(out KnapsackSaveableSubCard knapsackSubCardData))
            {
                bool couldEquipItem = knapsackSubCardData.TryEquipItem(newEquipableItem);
                if (!couldEquipItem) { Debug.Log($"Could not find or add item {newEquipableItem} to inventory.");}
                return couldEquipItem;
            }
            return true;
        }
    }
}
