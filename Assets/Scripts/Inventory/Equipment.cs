using System;
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
        private readonly Dictionary<EquipLocation, EquipableItem> equippedItems = new();

        // Cached References
        private Knapsack knapsack;

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
        public bool HasItemInSlot(EquipLocation equipLocation) => equippedItems.ContainsKey(equipLocation);
        public EquipableItem GetItemInSlot(EquipLocation equipLocation) => equippedItems.GetValueOrDefault(equipLocation);
        public IEnumerable<EquipLocation> GetAllPopulatedSlots() => equippedItems.Keys;

        public Dictionary<Stat, float> CompareEquipableItem(EquipLocation equipLocation, EquipableItem equipableItem)
        {
            var comparisonStatSheet = new Dictionary<Stat, float>();

            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                if (BaseStats.GetNonModifyingStats().Contains(stat)) { continue; }

                comparisonStatSheet[stat] = 0f;
            }

            if (equippedItems.TryGetValue(equipLocation, out EquipableItem currentlyEquippedItem))
            {
                foreach (BaseStatModifier baseStatModifier in currentlyEquippedItem.GetBaseStatModifiers())
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
                if (!equippedItems.TryGetValue(equipLocation, out EquipableItem item)) { continue; }
                if (!knapsack.HasItem(item))
                {
                    RemoveEquipment(equipLocation, false);
                }
            }
            if (announceUpdate) { equipmentUpdated?.Invoke(null); }
        }
        #endregion

        #region ModifierInterface
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            return GetAllPopulatedSlots().Select(GetItemInSlot).Where(item => (IModifierProvider)item != null).SelectMany(item => ((IModifierProvider)item).GetAdditiveModifiers(stat));
        }
        #endregion

        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
        public SaveState CaptureState() => PackSaveData(equippedItems, GetLoadPriority());

        public void RestoreState(SaveState saveState)
        {
            equippedItems.Clear();
            foreach (KeyValuePair<EquipLocation, EquipableItem> pair in UnpackSaveData(saveState))
            {
                equippedItems[pair.Key] = pair.Value;
            }
        }

        public SaveState ManualGetStateFromData(Dictionary<EquipLocation, EquipableItem> data)
        {
            Dictionary<EquipLocation, EquipableItem> filteredSaveData = data.Where(pair => pair.Value != null).ToDictionary(pair => pair.Key, pair => pair.Value);
            return PackSaveData(filteredSaveData, GetLoadPriority());
        }

        public Dictionary<EquipLocation, EquipableItem> ManualGetDataFromState(SaveState saveState)
        {
            Dictionary<EquipLocation, EquipableItem> dataSet = new Dictionary<EquipLocation, EquipableItem>();
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                if (equipLocation == EquipLocation.None) { continue; }
                dataSet[equipLocation] = null;
            }
            if (saveState == null) { return dataSet; }
            
            foreach (KeyValuePair<EquipLocation, EquipableItem> pair in UnpackSaveData(saveState))
            {
                dataSet[pair.Key] = pair.Value;
            }
            return dataSet;
        }
        
        private static SaveState PackSaveData(Dictionary<EquipLocation, EquipableItem> saveData, LoadPriority loadPriority)
        {
            var equippedItemsForSerialization = new Dictionary<EquipLocation, string>();
            foreach (KeyValuePair<EquipLocation, EquipableItem> pair in saveData)
            {
                equippedItemsForSerialization[pair.Key] = pair.Value.GetGUID();
            }
            return new SaveState(loadPriority, equippedItemsForSerialization);
        }

        private Dictionary<EquipLocation, EquipableItem> UnpackSaveData(SaveState saveState)
        {
            Dictionary<EquipLocation, EquipableItem> saveData = new Dictionary<EquipLocation, EquipableItem>();
            if (saveState.GetState(typeof(Dictionary<EquipLocation, string>)) is not Dictionary<EquipLocation, string> equippedItemsForSerialization) { return saveData; }

            foreach (KeyValuePair<EquipLocation, string> pair in equippedItemsForSerialization)
            {
                var equipableItem = InventoryItem.GetFromID(pair.Value) as EquipableItem;
                if (equipableItem != null)
                {
                    saveData[pair.Key] = equipableItem;
                }
            }
            return saveData;
        }
        #endregion
    }
}
