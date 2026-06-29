using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Saving;
using Frankie.Stats;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Knapsack))]
    [RequireComponent(typeof(WearablesLink))]
    public class Equipment : MonoBehaviour, ISaveable<Dictionary<EquipLocation, EquipableItemBase>>, IModifierProvider
    {
        // State
        private readonly Dictionary<EquipLocation, EquipableItemBase> equippedItems = new();

        // Cached References
        private Knapsack knapsack;
        private WearablesLink wearablesLink;

        // Events
        public event Action<EquipableItemBase> equipmentUpdated;

        private void Awake()
        {
            knapsack = GetComponent<Knapsack>();
            wearablesLink = GetComponent<WearablesLink>();
        }

        private void Start()
        {
            ReconcileEquipment(true);
        }

        #region CheckEquipment
        public bool HasItemInSlot(EquipLocation equipLocation) => equippedItems.ContainsKey(equipLocation);
        public EquipableItemBase GetItemInSlot(EquipLocation equipLocation) => equippedItems.GetValueOrDefault(equipLocation);
        public IEnumerable<EquipLocation> GetAllPopulatedSlots() => equippedItems.Keys;

        public Dictionary<Stat, float> CompareEquipableItem(EquipLocation equipLocation, EquipableItemBase equipableItem)
        {
            var comparisonStatSheet = new Dictionary<Stat, float>();

            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                if (BaseStats.GetNonModifyingStats().Contains(stat)) { continue; }

                comparisonStatSheet[stat] = 0f;
            }

            if (equippedItems.TryGetValue(equipLocation, out EquipableItemBase currentlyEquippedItem))
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
        public bool AddEquipment(EquipableItemBase equipableItem, bool announceUpdate)
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
            if (equipableItem is WearableItem wearableItem) { wearablesLink.SpawnWearable(wearableItem); }

            if (announceUpdate) { equipmentUpdated?.Invoke(equipableItem); }
            return true;
        }

        public void RemoveEquipment(EquipLocation equipLocation, bool announceUpdate)
        {
            if (equippedItems[equipLocation] is WearableItem wearableItem) { wearablesLink.RemoveWearable(wearableItem); }
            equippedItems.Remove(equipLocation);
            if (announceUpdate) { equipmentUpdated?.Invoke(null); }
        }

        public void ReconcileEquipment(bool announceUpdate)
        {
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                if (equipLocation == EquipLocation.None) { continue; }
                if (!equippedItems.TryGetValue(equipLocation, out EquipableItemBase item)) { continue; }
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
        public SaveState CaptureState() => PackSaveData(GetLoadPriority(), equippedItems);

        public void RestoreState(SaveState saveState)
        {
            if (wearablesLink == null) { wearablesLink = GetComponent<WearablesLink>(); }
            
            equippedItems.Clear();
            foreach (KeyValuePair<EquipLocation, EquipableItemBase> pair in UnpackSaveData(saveState))
            {
                equippedItems[pair.Key] = pair.Value;
                if (pair.Value is WearableItem wearableItem) { wearablesLink.SpawnWearable(wearableItem); }
            }
        }

        public SaveState ManualGetStateFromData(Dictionary<EquipLocation, EquipableItemBase> data)
        {
            Dictionary<EquipLocation, EquipableItemBase> filteredSaveData = data.Where(pair => pair.Value != null).ToDictionary(pair => pair.Key, pair => pair.Value);
            return PackSaveData(GetLoadPriority(), filteredSaveData);
        }

        public Dictionary<EquipLocation, EquipableItemBase> ManualGetDataFromState(SaveState saveState)
        {
            Dictionary<EquipLocation, EquipableItemBase> dataSet = new Dictionary<EquipLocation, EquipableItemBase>();
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                if (equipLocation == EquipLocation.None) { continue; }
                dataSet[equipLocation] = null;
            }
            if (saveState == null) { return dataSet; }
            
            foreach (KeyValuePair<EquipLocation, EquipableItemBase> pair in UnpackSaveData(saveState))
            {
                dataSet[pair.Key] = pair.Value;
            }
            return dataSet;
        }
        
        private static SaveState PackSaveData(LoadPriority loadPriority, Dictionary<EquipLocation, EquipableItemBase> saveData)
        {
            var equippedItemsForSerialization = new Dictionary<EquipLocation, string>();
            foreach (KeyValuePair<EquipLocation, EquipableItemBase> pair in saveData)
            {
                equippedItemsForSerialization[pair.Key] = pair.Value.GetGUID();
            }
            return new SaveState(loadPriority, equippedItemsForSerialization);
        }

        private static Dictionary<EquipLocation, EquipableItemBase> UnpackSaveData(SaveState saveState)
        {
            Dictionary<EquipLocation, EquipableItemBase> saveData = new Dictionary<EquipLocation, EquipableItemBase>();
            if (saveState?.GetState(typeof(Dictionary<EquipLocation, string>)) is not Dictionary<EquipLocation, string> equippedItemsForSerialization) { return saveData; }

            foreach (KeyValuePair<EquipLocation, string> pair in equippedItemsForSerialization)
            {
                var equipableItem = InventoryItem.GetFromID(pair.Value) as EquipableItemBase;
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
