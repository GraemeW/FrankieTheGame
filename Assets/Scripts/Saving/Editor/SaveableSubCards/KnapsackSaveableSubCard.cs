using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Frankie.Inventory;

namespace Frankie.Saving.Editor
{
    public class KnapsackSaveableSubCard : SaveableSubCardData
    {
        private readonly List<ObjectField> itemFields = new();
        private readonly List<Toggle> equippedFields = new();

        public KnapsackSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not Knapsack knapsack) { return; }
            
            if (!knapsack.TryManualGetDataFromState(saveState, out ActiveInventoryItem[] itemsInKnapsack))
            {
                subCardView.Add(new Label("No Knapsack save data available"));
                return;
            }
            
            itemFields.Clear();
            equippedFields.Clear();
            for (int i = 0; i < itemsInKnapsack.Length; i++)
            {
                int slotIndex = i;
                CreateSlotRow(subCardView, knapsack, itemsInKnapsack, slotIndex);
            }
            AddAdditionalTools(subCardView, knapsack, itemsInKnapsack);
        }

        private void CreateSlotRow(Box subCardView, Knapsack knapsack, ActiveInventoryItem[] itemsInKnapsack, int slotIndex)
        {
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
            itemFields.Add(itemField);

            var equippedField = new Toggle { value = isEquipped, style = { width = 80 } };
            equippedField.SetEnabled(false);
            slotRow.Add(equippedField);
            equippedFields.Add(equippedField);

            itemField.RegisterValueChangedCallback(changeEvent =>
            {
                var newInventoryItem = changeEvent.newValue as InventoryItem;
                ActiveInventoryItem updatedItem = newInventoryItem != null ? new ActiveInventoryItem(newInventoryItem) : null;

                if (equippedField.value)
                {
                    // Unequip item from Equipment
                    var oldInventoryItem = changeEvent.previousValue as EquipableItemBase;
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

        private void AddAdditionalTools(Box subCardView, Knapsack knapsack, ActiveInventoryItem[] itemsInKnapsack)
        {
            subCardView.Add(new Label("Additional Knapsack Tools") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8 } });
            
            var fillRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(fillRow);
            var fillEmptySlotsButton = new Button { text = "Fill Empty Slots", style = { width = standardButtonWidth }};
            fillRow.Add(fillEmptySlotsButton);
            var fillItemField = new ObjectField { objectType = typeof(InventoryItem), style = { flexGrow = 1 } };
            fillRow.Add(fillItemField);

            var clearRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(clearRow);
            var clearAllItemsButton = new Button { text = "Clear All Items", style = { width = standardButtonWidth } };
            clearRow.Add(clearAllItemsButton);

            fillEmptySlotsButton.RegisterCallback<ClickEvent>(_ => FillEmptySlots(knapsack, itemsInKnapsack, fillItemField.value as InventoryItem));
            clearAllItemsButton.RegisterCallback<ClickEvent>(_ => ClearAllItems(knapsack, itemsInKnapsack));
        }

        private void FillEmptySlots(Knapsack knapsack, ActiveInventoryItem[] itemsInKnapsack, InventoryItem fillItem)
        {
            if (fillItem == null) { return; }

            bool anyFilled = false;
            for (int i = 0; i < itemsInKnapsack.Length; i++)
            {
                if (itemsInKnapsack[i] != null && itemsInKnapsack[i].GetInventoryItem() != null) { continue; }

                var newItem = new ActiveInventoryItem(fillItem);
                newItem.SetEquipped(false); // New item adds are always unequipped, can equip via Equipment
                itemsInKnapsack[i] = newItem;

                itemFields[i].SetValueWithoutNotify(fillItem);
                equippedFields[i].SetValueWithoutNotify(false);

                anyFilled = true;
            }
            if (!anyFilled) { return; }

            saveState = knapsack.ManualGetStateFromData(itemsInKnapsack);
            RaiseSaveStateChanged();
        }

        private void ClearAllItems(Knapsack knapsack, ActiveInventoryItem[] itemsInKnapsack)
        {
            bool anyCleared = false;
            for (int i = 0; i < itemsInKnapsack.Length; i++)
            {
                if (itemsInKnapsack[i] == null || itemsInKnapsack[i].GetInventoryItem() == null) { continue; }

                if (itemsInKnapsack[i].IsEquipped() && itemsInKnapsack[i].GetInventoryItem() is EquipableItemBase equipableItem)
                {
                    ReconcileEquipmentRemoval(equipableItem.GetEquipLocation());
                }

                itemsInKnapsack[i] = null;
                itemFields[i].SetValueWithoutNotify(null);
                equippedFields[i].SetValueWithoutNotify(false);

                anyCleared = true;
            }
            if (!anyCleared) { return; }

            saveState = knapsack.ManualGetStateFromData(itemsInKnapsack);
            RaiseSaveStateChanged();
        }

        public void UnequipItem(EquipableItemBase equipableItem)
        {
            if (equipableItem == null) { return; }
            
            if (saveable is not Knapsack knapsack) { return; }
            if (!knapsack.TryManualGetDataFromState(saveState, out ActiveInventoryItem[] itemsInKnapsack)) { return; }
            
            foreach (ActiveInventoryItem testItem in itemsInKnapsack)
            {
                if (testItem == null || testItem.GetInventoryItem() == null) { continue; }
                if (testItem.GetInventoryItem() is not EquipableItemBase testEquipableItem) { continue; }
                if (testEquipableItem.GetGUID() != equipableItem.GetGUID()) { continue;  }
                
                if (testItem.IsEquipped()) { testItem.SetEquipped(false); }
            }
            
            saveState = knapsack.ManualGetStateFromData(itemsInKnapsack);
            RaiseSaveStateChanged();
            Redraw();
        }
        
        public bool TryEquipItem(EquipableItemBase equipableItem, bool addIfNotPresent = true)
        {
            if (saveable is not Knapsack knapsack) { return false; }
            
            if (!knapsack.TryManualGetDataFromState(saveState, out ActiveInventoryItem[] itemsInKnapsack)) { return false; }
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
                if (itemsInKnapsack[i].GetInventoryItem() is not EquipableItemBase testEquipableItem) { continue; }

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
