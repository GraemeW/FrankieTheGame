using Frankie.Saving;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class Equipment : MonoBehaviour, ISaveable
    {
        // State
        Dictionary<EquipLocation, EquipableItem> equippedItems = new Dictionary<EquipLocation, EquipableItem>();

        // Events
        public event Action equipmentUpdated;

        public bool HasItemInSlot(EquipLocation equipLocation)
        {
            return equippedItems.ContainsKey(equipLocation);
        }

        public EquipableItem GetItemInSlot(EquipLocation equipLocation)
        {
            if (!equippedItems.ContainsKey(equipLocation)) { return null; }

            return equippedItems[equipLocation];
        }

        public Dictionary<Stat, float> CompareEquipableItem(EquipLocation equipLocation, EquipableItem equipableItem)
        {
            Dictionary<Stat, float> comparisonStatSheet = new Dictionary<Stat, float>();
            comparisonStatSheet[Stat.HP] = 0f;
            comparisonStatSheet[Stat.AP] = 0f;
            comparisonStatSheet[Stat.Brawn] = 0f;
            comparisonStatSheet[Stat.Beauty] = 0f;
            comparisonStatSheet[Stat.Smarts] = 0f;
            comparisonStatSheet[Stat.Nimble] = 0f;
            comparisonStatSheet[Stat.Luck] = 0f;
            comparisonStatSheet[Stat.Pluck] = 0f;
            comparisonStatSheet[Stat.Stoic] = 0f;
            if (equippedItems.ContainsKey(equipLocation))
            {
                foreach (BaseStatModifier baseStatModifier in equippedItems[equipLocation].GetBaseStatModifiers())
                {
                    comparisonStatSheet[baseStatModifier.stat] -= (baseStatModifier.minValue + baseStatModifier.maxValue) / 2;
                }
            }

            foreach (BaseStatModifier baseStatModifier in equipableItem.GetBaseStatModifiers())
            {
                comparisonStatSheet[baseStatModifier.stat] += (baseStatModifier.minValue + baseStatModifier.maxValue) / 2;
            }
            return comparisonStatSheet;
        }

        public void AddItem(EquipLocation equipLocation, EquipableItem equipableItem)
        {
            if (equipableItem.GetEquipLocation() != equipLocation) { return; }

            equippedItems[equipLocation] = equipableItem;

            if (equipmentUpdated != null)
            {
                equipmentUpdated.Invoke();
            }
        }

        public void RemoveItem(EquipLocation equipLocation)
        {
            equippedItems.Remove(equipLocation);

            if (equipmentUpdated != null)
            {
                equipmentUpdated.Invoke();
            }
        }

        public IEnumerable<EquipLocation> GetAllPopulatedSlots()
        {
            return equippedItems.Keys;
        }


        #region Interfaces
        public object CaptureState()
        {
            Dictionary<EquipLocation, string> equippedItemsForSerialization = new Dictionary<EquipLocation, string>();
            foreach (KeyValuePair<EquipLocation, EquipableItem> pair in equippedItems)
            {
                equippedItemsForSerialization[pair.Key] = pair.Value.GetItemID();
            }
            return equippedItemsForSerialization;
        }

        public void RestoreState(object state)
        {
            equippedItems = new Dictionary<EquipLocation, EquipableItem>();

            Dictionary<EquipLocation, string> equippedItemsForSerialization = (Dictionary<EquipLocation, string>)state;

            foreach (KeyValuePair<EquipLocation, string> pair in equippedItemsForSerialization)
            {
                EquipableItem equipableItem = InventoryItem.GetFromID(pair.Value) as EquipableItem;
                if (equipableItem != null)
                {
                    equippedItems[pair.Key] = equipableItem;
                }
            }
        }
        #endregion
    }
}
