using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Combat;
using Frankie.Core;
using Frankie.Quests;
using Frankie.Saving;
using Frankie.Utils;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Equipment))]
    [RequireComponent(typeof(CombatParticipant))]
    public class Knapsack : MonoBehaviour, ISaveable, IQuestEvaluator
    {
        // Tunables
        [SerializeField] private int inventorySize = 16;

        // State
        private ActiveInventoryItem[] slots;

        // Cached References
        private ReInitLazyValue<QuestList> questList;
        private CombatParticipant character;
        private Equipment equipment;

        // Events
        public event Action knapsackUpdated;

        #region UnityMethods
        private void Awake()
        {
            slots = new ActiveInventoryItem[inventorySize];

            questList = new ReInitLazyValue<QuestList>(SetupQuestList);
            equipment = GetComponent<Equipment>();
            character = GetComponent<CombatParticipant>();
        }

        private void Start()
        {
            questList.ForceInit();
        }

        private void OnEnable()
        {
            knapsackUpdated += CompleteObjective;
            equipment.equipmentUpdated += HandleEquipmentUpdated;
        }

        private void OnDisable()
        {
            knapsackUpdated -= CompleteObjective;
            equipment.equipmentUpdated -= HandleEquipmentUpdated;
        }

        private static QuestList SetupQuestList()
        {
            GameObject playerGameObject = Player.FindPlayerObject();
            return (playerGameObject != null) ? playerGameObject.GetComponent<QuestList>() : null;
        }
        #endregion

        #region CheckKnapsack
        public int GetSize() => slots.Length;
        public bool IsEmpty() => slots.All(t => t == null);
        public bool HasFreeSpace() => GetNumberOfFreeSlots() > 0;
        public bool HasItem(InventoryItem inventoryItem)
        {
            return slots.Where(t => t != null).Any(t => t.GetInventoryItem().GetItemID() == inventoryItem.GetItemID());
        }
        public bool HasItemInSlot(int slot) =>  slots[slot] != null;
        public bool IsItemInSlotEquipped(int slot) => slots[slot] != null && slots[slot].IsEquipped();
        public bool HasAnyEquipableItem(EquipLocation equipLocation)
        {
            return (from inventoryItem in slots where inventoryItem != null select inventoryItem.GetInventoryItem()).OfType<EquipableItem>().Any(equipableItem => equipableItem.GetEquipLocation() == equipLocation);
        }

        public bool HasEquipableItemInSlot(int slot, EquipLocation equipLocation)
        {
            if (slots[slot] == null) { return false; }
            if (slots[slot].GetInventoryItem() is not EquipableItem equipableItem) { return false; }
            return (equipableItem.GetEquipLocation() == equipLocation);
        }
        #endregion

        #region RetrieveFromKnapsack
        public int GetNumberOfFreeSlots() => slots.Count(x => x == null);
        public InventoryItem GetItemInSlot(int slot) => slots[slot] == null ? null : slots[slot].GetInventoryItem();
        private IEnumerable<KeyItem> GetKeyItems()
        {
            return (from inventoryItem in slots where inventoryItem != null select inventoryItem.GetInventoryItem()).OfType<KeyItem>();
        }

        private int FindEmptySlot()
        {
            // Returns slot index of first empty slot
            // Otherwise returns -1 to indicate no empty slot
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) { return i; }
            }
            return -1;
        }

        private int FindSlotWithItem(InventoryItem inventoryItem)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) { continue; }
                if (slots[i].GetInventoryItem().GetItemID() == inventoryItem.GetItemID()) { return i; }
            }
            return -1;
        }

        private List<int> FindSlotsWithItem(InventoryItem inventoryItem)
        {
            var matchedSlots = new List<int>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) { continue; }
                if (slots[i].GetInventoryItem().GetItemID() == inventoryItem.GetItemID()) { matchedSlots.Add(i); }
            }
            return matchedSlots;
        }
        #endregion

        #region ModifyKnapsack
        // Base Functions
        // Each employs announce update flag to allow for batching without a billion events
        private bool AddToSlot(InventoryItem inventoryItem, int slot, bool announceUpdate)
        {
            if (slots[slot] != null || inventoryItem == null) { return false; }
            slots[slot] = new ActiveInventoryItem(inventoryItem);

            if (announceUpdate) { knapsackUpdated?.Invoke(); }
            return true;
        }

        public bool AddToFirstEmptySlot(InventoryItem inventoryItem, bool announceUpdate)
        {
            int slot = FindEmptySlot();
            if (slot < 0) { return false; }

            return AddToSlot(inventoryItem, slot, announceUpdate);
        }

        public void RemoveFromSlot(int slot, bool announceUpdate)
        {
            if (slots[slot] == null) { return; }
            if (slots[slot].IsEquipped())
            {
                var equipableItem = slots[slot].GetInventoryItem() as EquipableItem;
                if (equipableItem == null) { return; }
                
                EquipLocation equipLocation = equipableItem.GetEquipLocation();
                equipment.RemoveEquipment(equipLocation, announceUpdate);
            }
            slots[slot] = null;
            if (announceUpdate) { knapsackUpdated?.Invoke(); }
        }

        public bool RemoveItem(InventoryItem inventoryItem, bool announceUpdate)
        {
            // Prioritize unequipped items
            List<int> matchedSlots = FindSlotsWithItem(inventoryItem).OrderBy(slot => slots[slot].IsEquipped()).ToList();
            if (matchedSlots.Count == 0) { return false; }
            
            RemoveFromSlot(matchedSlots.FirstOrDefault(), announceUpdate);
            return true;
        }

        public void SquishItemsInKnapsack()
        {
            var knapsackQueue = new Queue<ActiveInventoryItem>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (!HasItemInSlot(i)) continue;
                knapsackQueue.Enqueue(slots[i]);
                slots[i] = null;
            }

            int itemIndex = 0;
            while (knapsackQueue.Count > 0)
            {
                slots[itemIndex] = knapsackQueue.Dequeue();
                itemIndex++;
            }
            knapsackUpdated?.Invoke();
        }

        // Complex & Combination Functions
        // Include one or multiple calls to base functions
        public bool UseItemInSlot(int slot, IEnumerable<BattleEntity> battleEntities)
        {
            InventoryItem inventoryItem = GetItemInSlot(slot);
            var actionItem = inventoryItem as ActionItem;
            if (actionItem == null) { return false; }

            var battleActionData = new BattleActionData(character);
            battleActionData.SetTargets(battleEntities);
            actionItem.Use(battleActionData, null);
            // Note:  item removal handled via ActionItem
            return true;
        }

        public void DropItem(int slot)
        {
            if (slots[slot] == null) { return; }
            if (!slots[slot].GetInventoryItem().IsDroppable()) { return; }
            RemoveFromSlot(slot, false);
            SquishItemsInKnapsack();
        }

        public void MoveItem(int sourceSlot, Knapsack destinationKnapsack, int destinationSlot)
        {
            if (slots[sourceSlot] == null) { return; }
            if (this == destinationKnapsack && sourceSlot == destinationSlot) { return; } // move from same position to same position

            InventoryItem swapItem = null;
            bool preserveSourceEquippedState = false;
            bool preserveDestinationEquippedState = false;

            // Check if swapping or only simple move
            if (this == destinationKnapsack && slots[sourceSlot].IsEquipped()) { preserveSourceEquippedState = true; }
            if (destinationKnapsack.HasItemInSlot(destinationSlot))
            {
                swapItem = destinationKnapsack.GetItemInSlot(destinationSlot);
                if (this == destinationKnapsack && slots[destinationSlot] != null && slots[destinationSlot].IsEquipped()) { preserveDestinationEquippedState = true; }
                destinationKnapsack.RemoveFromSlot(destinationSlot, false);
            }

            // Remove item from source
            InventoryItem sourceItem = GetItemInSlot(sourceSlot);
            RemoveFromSlot(sourceSlot, false);

            // Add item to destination
            destinationKnapsack.AddToSlot(sourceItem, destinationSlot, false);
            var sourceEquipableItem = sourceItem as EquipableItem;
            if (sourceEquipableItem != null && preserveSourceEquippedState) { equipment.AddEquipment(sourceEquipableItem, false); }

            // Complete swap if swapping
            if (swapItem != null)
            {
                AddToSlot(swapItem, sourceSlot, false);
                var swapEquipableItem = swapItem as EquipableItem;
                if (swapEquipableItem != null && preserveDestinationEquippedState) { equipment.AddEquipment(swapEquipableItem, false); }
            }

            // Re-order items in knapsack & reconcile equipment
            SquishItemsInKnapsack();
            equipment.ReconcileEquipment(true);
            if (this != destinationKnapsack)
            {
                destinationKnapsack.SquishItemsInKnapsack();
                destinationKnapsack.GetComponent<Equipment>()?.ReconcileEquipment(true);
            }
        }
        #endregion

        #region ActionHandling
        private void HandleEquipmentUpdated(EquipableItem itemUpdated)
        {
            ResetEquippedFlags();
            foreach (EquipLocation equipLocation in equipment.GetAllPopulatedSlots())
            {
                EquipableItem equipableItemInSlot = equipment.GetItemInSlot(equipLocation);
                int equippedItemSlot = FindSlotWithItem(equipableItemInSlot); // First item in knapsack matching criteria
                if (equippedItemSlot != -1) { slots[equippedItemSlot].SetEquipped(true); }
            }
            knapsackUpdated?.Invoke();
        }

        private void ResetEquippedFlags()
        {
            foreach (ActiveInventoryItem inventoryItem in slots)
            {
                inventoryItem?.SetEquipped(false);
            }
        }
        #endregion

        #region Interfaces
        // Quest Evaluator
        public void CompleteObjective()
        {
            foreach (KeyItem keyItem in GetKeyItems())
            {
                if (keyItem == null) { continue; }

                foreach (QuestObjective questObjective in keyItem.GetQuestObjectives())
                {
                    if (questObjective == null) { continue; }
                    questList.value.CompleteObjective(questObjective);
                }
            }
        }

        // Saving System
        [Serializable]
        private struct SaveableActiveItem
        {
            public string inventoryItemID;
            public bool equipped;
        }

        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        SaveState ISaveable.CaptureState()
        {
            slots ??= new ActiveInventoryItem[inventorySize];
            SaveableActiveItem[] slotsActiveItemStrings = new SaveableActiveItem[inventorySize];
            for (int i = 0; i < inventorySize; i++)
            {
                if (slots[i] == null) continue;
                
                var saveableActiveItem = new SaveableActiveItem
                {
                    inventoryItemID = slots[i].GetInventoryItem().GetItemID(),
                    equipped = slots[i].IsEquipped()
                };
                slotsActiveItemStrings[i] = saveableActiveItem;
            }
            var saveState = new SaveState(GetLoadPriority(), slotsActiveItemStrings);

            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            slots ??= new ActiveInventoryItem[inventorySize];
            if (saveState.GetState(typeof(SaveableActiveItem[])) is not SaveableActiveItem[] slotsActiveItemStrings) { return; }

            for (int i = 0; i < inventorySize; i++)
            {
                if (string.IsNullOrEmpty(slotsActiveItemStrings[i].inventoryItemID)) { continue; }

                string inventoryItemID = slotsActiveItemStrings[i].inventoryItemID;
                var activeInventoryItem = new ActiveInventoryItem(InventoryItem.GetFromID(inventoryItemID));
                activeInventoryItem.SetEquipped(slotsActiveItemStrings[i].equipped);

                slots[i] = activeInventoryItem;
            }

            knapsackUpdated?.Invoke();
        }
        #endregion
    }
}
