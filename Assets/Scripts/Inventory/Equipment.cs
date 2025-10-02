using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Saving;
using Frankie.Stats;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Knapsack))]
    public class Equipment : MonoBehaviour, ISaveable, IModifierProvider
    {
        // State
        Dictionary<EquipLocation, EquipableItem> equippedItems = new Dictionary<EquipLocation, EquipableItem>();

        // Cached References
        Knapsack knapsack;

        // Events
        public event Action<EquipableItem> equipmentUpdated;

        private void Awake()
        {
            knapsack = GetComponent<Knapsack>();
        }

        private void Start()
        {
            ReconcileEquipment(true);
        }

        #region CheckEquipment
        public bool HasItemInSlot(EquipLocation equipLocation)
        {
            return equippedItems.ContainsKey(equipLocation);
        }

        public EquipableItem GetItemInSlot(EquipLocation equipLocation)
        {
            if (!equippedItems.ContainsKey(equipLocation)) { return null; }

            return equippedItems[equipLocation];
        }

        public IEnumerable<EquipLocation> GetAllPopulatedSlots()
        {
            return equippedItems.Keys;
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
        #endregion

        #region ModifyEquipment
        public bool AddEquipment(EquipableItem equipableItem, bool announceUpdate)
        {
            if (knapsack == null || !knapsack.HasItem(equipableItem)) { return false; }
            if (!equipableItem.CanUseItem(this)) { return false; }
            EquipLocation equipLocation = equipableItem.GetEquipLocation();

            // Swap
            if (HasItemInSlot(equipLocation))
            {
                RemoveEquipment(equipLocation, false);
            }
            // Add
            equippedItems[equipLocation] = equipableItem;

            if (announceUpdate) { equipmentUpdated?.Invoke(equipableItem); }
            return true;
        }

        public void RemoveEquipment(EquipLocation equipLocation, bool announceUpdate)
        {
            equippedItems.Remove(equipLocation);

            if (announceUpdate) { equipmentUpdated?.Invoke(null); }
        }

        public void ReconcileEquipment(bool announceUpdate)
        {
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                if (equipLocation == EquipLocation.None) { continue; }

                if (equippedItems.ContainsKey(equipLocation))
                {
                    if (!knapsack.HasItem(equippedItems[equipLocation]))
                    {
                        RemoveEquipment(equipLocation, false);
                    }
                }
            }

            if (announceUpdate) { equipmentUpdated?.Invoke(null); }
        }
        #endregion

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

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            Dictionary<EquipLocation, string> equippedItemsForSerialization = new Dictionary<EquipLocation, string>();
            foreach (KeyValuePair<EquipLocation, EquipableItem> pair in equippedItems)
            {
                equippedItemsForSerialization[pair.Key] = pair.Value.GetItemID();
            }
            SaveState saveState = new SaveState(GetLoadPriority(), equippedItemsForSerialization);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            equippedItems = new Dictionary<EquipLocation, EquipableItem>();
            Dictionary<EquipLocation, string> equippedItemsForSerialization = saveState.GetState(typeof(Dictionary<EquipLocation, string>)) as Dictionary<EquipLocation, string>;
            if (equippedItemsForSerialization == null) { return; }

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
