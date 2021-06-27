using Frankie.Saving;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Inventory
{
    public class Equipment : MonoBehaviour, ISaveable, IModifierProvider
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
            Stat[] nonModifyingStats = BaseStats.GetNonModifyingStats();

            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                if (nonModifyingStats.Contains(stat)) { continue; }

                comparisonStatSheet[stat] = 0f;
            }

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

        public void AddSwapOrRemoveItem(EquipLocation equipLocation, EquipableItem equipableItem)
        {
            if (equipableItem == null)
            {
                if (!equippedItems.ContainsKey(equipLocation)) { return; }
                equippedItems.Remove(equipLocation);
            }
            else
            {
                if (equipLocation == EquipLocation.None || equipableItem.GetEquipLocation() != equipLocation) { return; }

                if (HasItemInSlot(equipLocation))
                {
                    RemoveItem(equipLocation);
                }
                AddItem(equipLocation, equipableItem);
            }

            if (equipmentUpdated != null)
            {
                equipmentUpdated.Invoke();
            }
        }

        private void AddItem(EquipLocation equipLocation, EquipableItem equipableItem)
        {
            equippedItems[equipLocation] = equipableItem;
        }

        private void RemoveItem(EquipLocation equipLocation)
        {
            equippedItems.Remove(equipLocation);
        }

        public IEnumerable<EquipLocation> GetAllPopulatedSlots()
        {
            return equippedItems.Keys;
        }


        #region Interfaces
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            foreach (EquipLocation slot in GetAllPopulatedSlots())
            {
                IModifierProvider item = GetItemInSlot(slot) as IModifierProvider;
                if (item == null) { continue; }

                foreach (float modifier in item.GetAdditiveModifiers(stat))
                {
                    yield return modifier;
                }
            }
        }

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
