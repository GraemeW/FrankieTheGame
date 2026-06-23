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
                equippedField.SetEnabled(false);
                slotRow.Add(equippedField);

                itemField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newInventoryItem = changeEvent.newValue as InventoryItem;
                    ActiveInventoryItem updatedItem = newInventoryItem != null ? new ActiveInventoryItem(newInventoryItem) : null;

                    if (equippedField.value)
                    {
                        // Unequip item from Equipment
                        var oldInventoryItem = changeEvent.previousValue as EquipableItem;
                        if (oldInventoryItem != null) { ReconcileEquipmentRemoval(oldInventoryItem.GetEquipLocation()); }
                    }
                    
                    updatedItem?.SetEquipped(false); // New item adds are always unequipped, can equip via Equipment
                    equippedField.SetValueWithoutNotify(false);
                    
                    itemsInKnapsack[slotIndex] = updatedItem;
                    saveState = knapsack.ManualGetStateFromData(itemsInKnapsack);
                    RaiseSaveStateChanged();
                });

                // Note:  Equipped field effectively unused, since it mirrors from Equipment
                // Option here kept for posterity in case of future refactors
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

        public void UnequipItem(EquipableItem equipableItem)
        {
            if (equipableItem == null) { return; }
            
            if (saveable is not Knapsack knapsack) { return; }
            ActiveInventoryItem[] itemsInKnapsack = knapsack.ManualGetDataFromState(saveState);
            
            foreach (ActiveInventoryItem testItem in itemsInKnapsack)
            {
                if (testItem == null || testItem.GetInventoryItem() == null) { continue; }
                if (testItem.GetInventoryItem() is not EquipableItem testEquipableItem) { continue; }
                if (testEquipableItem.GetGUID() != equipableItem.GetGUID()) { continue;  }
                
                if (testItem.IsEquipped()) { testItem.SetEquipped(false); }
            }
            
            saveState = knapsack.ManualGetStateFromData(itemsInKnapsack);
            RaiseSaveStateChanged();
            Redraw();
        }
        
        public bool TryEquipItem(EquipableItem equipableItem, bool addIfNotPresent = true)
        {
            if (saveable is not Knapsack knapsack) { return false; }
            
            ActiveInventoryItem[] itemsInKnapsack = knapsack.ManualGetDataFromState(saveState);
            int matchSlot = -1;
            int emptySlot = -1;
            for (int i = 0; i < itemsInKnapsack.Length; i++)
            {
                if (emptySlot < 0 && (itemsInKnapsack[i] == null || itemsInKnapsack[i].GetInventoryItem() == null)) { emptySlot = i; }
                if (itemsInKnapsack[i] == null) { continue; }
                
                if (itemsInKnapsack[i].GetInventoryItem().GetGUID() == equipableItem.GetGUID())
                {
                    itemsInKnapsack[i].SetEquipped(true);
                    matchSlot = i;
                }
            }
            
            // Add inventory item if no match found
            if (matchSlot < 0 && addIfNotPresent && emptySlot >= 0)
            {
                itemsInKnapsack[emptySlot] = new ActiveInventoryItem(equipableItem);
                itemsInKnapsack[emptySlot].SetEquipped(true);
                matchSlot = emptySlot;
            }
            
            if (matchSlot < 0) { return false; }
            
            
            // Note:  Remove other unequipped items must be done after match to avoid unequip and then fail to add
            EquipLocation equipLocation = equipableItem.GetEquipLocation();
            for (int i = 0; i < itemsInKnapsack.Length; i++)
            {
                if (i == matchSlot) { continue; }
                if (itemsInKnapsack[i] == null || itemsInKnapsack[i].GetInventoryItem() == null) { continue; }
                if (itemsInKnapsack[i].GetInventoryItem() is not EquipableItem testEquipableItem) { continue; }

                if (testEquipableItem.GetEquipLocation() == equipLocation) { itemsInKnapsack[i].SetEquipped(false); }
            }
                
            saveState = knapsack.ManualGetStateFromData(itemsInKnapsack);
            RaiseSaveStateChanged();
            Redraw();
            return true;
        }
        
        private void ReconcileEquipmentRemoval(EquipLocation equipLocation)
        {
            if (saveableEntityCardData == null) { return; }
            if (!saveableEntityCardData.TryGetSaveableSubCardData(out EquipmentSaveableSubCard equipmentSaveableSubCard)) { return; }
            
            equipmentSaveableSubCard.UnequipItemFromLocation(equipLocation);
        }
    }
}
