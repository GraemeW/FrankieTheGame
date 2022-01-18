using Frankie.Combat;
using Frankie.Core;
using Frankie.Quests;
using Frankie.Saving;
using Frankie.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Equipment))]
    [RequireComponent(typeof(CombatParticipant))]
    public class Knapsack : MonoBehaviour, ISaveable, IQuestEvaluator
    {
        // Tunables
        [SerializeField] int inventorySize = 16;

        // State
        ActiveInventoryItem[] slots;

        // Cached References
        GameObject player = null;
        ReInitLazyValue<QuestList> questList = null;
        CombatParticipant character = null;
        Equipment equipment = null;

        // Events
        public event Action knapsackUpdated;

        #region UnityMethods
        private void Awake()
        {
            slots = new ActiveInventoryItem[inventorySize];

            player = GameObject.FindGameObjectWithTag("Player");
            questList = new ReInitLazyValue<QuestList>(() => QuestList.GetQuestList(ref player));

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
        #endregion

        #region CheckKnapsack
        public int GetSize()
        {
            return slots.Length;
        }

        public bool IsEmpty()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) { return false; }
            }
            return true;
        }

        public bool HasFreeSpace()
        {
            return GetNumberOfFreeSlots() > 0;
        }

        public bool HasItem(InventoryItem inventoryItem)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) { continue; }

                if (object.ReferenceEquals(slots[i].GetInventoryItem(), inventoryItem))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasItemInSlot(int slot)
        {
            if (slots[slot] == null) { return false; }
            return true;
        }

        public bool IsItemInSlotEquipped(int slot)
        {
            if (slots[slot] != null) { return slots[slot].IsEquipped(); }
            return false;
        }

        public bool HasAnyEquipableItem(EquipLocation equipLocation)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].GetInventoryItem().GetType() == typeof(EquipableItem))
                {
                    EquipableItem equipableItem = slots[i].GetInventoryItem() as EquipableItem;
                    if (equipableItem.GetEquipLocation() == equipLocation)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool HasEquipableItemInSlot(int slot)
        {
            if (slots[slot] == null) { return false; }

            return (slots[slot].GetInventoryItem().GetType() == typeof(EquipableItem));
        }

        public bool HasEquipableItemInSlot(int slot, EquipLocation equipLocation)
        {
            if (!HasEquipableItemInSlot(slot)) { return false; }

            EquipableItem equipableItem = slots[slot].GetInventoryItem() as EquipableItem;
            return (equipableItem.GetEquipLocation() == equipLocation);
        }
        #endregion

        #region RetrieveFromKnapsack

        public int GetNumberOfFreeSlots()
        {
            return slots.Where(x => x == null).Count();
        }

        public InventoryItem GetItemInSlot(int slot)
        {
            if (slots[slot] == null) { return null; }
            return slots[slot].GetInventoryItem();
        }

        public IEnumerable<KeyItem> GetKeyItems()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) { continue; }

                KeyItem keyItem = slots[i].GetInventoryItem() as KeyItem;
                if (keyItem != null)
                {
                    yield return keyItem;
                }
            }
        }

        private int FindEmptySlot()
        {
            // Returns slot index of first empty slot
            // Otherwise returns -1 to indicate no empty slot
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindSlotWithItem(InventoryItem inventoryItem)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) { continue; }

                if (object.ReferenceEquals(slots[i].GetInventoryItem(), inventoryItem))
                {
                    return i;
                }
            }
            return -1;
        }
        #endregion

        #region ModifyKnapsack

        // Base Functions
        // Each employs announce update flag to allow for batching without a billion events
        private bool AddToSlot(InventoryItem inventoryItem, int slot, bool announceUpdate)
        {
            if (slots[slot] != null || inventoryItem == null) { return false; }
            slots[slot] = new ActiveInventoryItem(inventoryItem); ;

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
                EquipableItem equipableItem = slots[slot].GetInventoryItem() as EquipableItem;
                EquipLocation equipLocation = equipableItem.GetEquipLocation();
                equipment.RemoveEquipment(equipLocation, announceUpdate);
            }
            slots[slot] = null;

            if (announceUpdate) { knapsackUpdated?.Invoke(); }
        }

        public void RemoveItem(InventoryItem inventoryItem, bool announceUpdate)
        {
            int slot = FindSlotWithItem(inventoryItem);
            if (slot < 0) { return; }

            RemoveFromSlot(slot, announceUpdate);
        }

        public void SquishItemsInKnapsack()
        {
            Queue<ActiveInventoryItem> knapsackQueue = new Queue<ActiveInventoryItem>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (HasItemInSlot(i))
                {
                    knapsackQueue.Enqueue(slots[i]);
                    slots[i] = null;
                }
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
        public bool UseItemInSlot(int slot, IEnumerable<CombatParticipant> combatParticipants)
        {
            InventoryItem inventoryItem = GetItemInSlot(slot);
            ActionItem actionItem = inventoryItem as ActionItem;
            if (actionItem == null) { return false; }

            BattleActionData battleActionData = new BattleActionData(character);
            battleActionData.SetTargets(combatParticipants);
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
            EquipableItem sourceEquipableItem = sourceItem as EquipableItem;
            if (sourceEquipableItem != null && preserveSourceEquippedState) { equipment.AddEquipment(sourceEquipableItem, false); }

            // Complete swap if swapping
            if (swapItem != null)
            {
                AddToSlot(swapItem, sourceSlot, false);
                EquipableItem swapEquipableItem = swapItem as EquipableItem;
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
                if (equippedItemSlot != -1)
                {
                    slots[equippedItemSlot].SetEquipped(true);
                }
            }

            knapsackUpdated?.Invoke();
        }

        private void ResetEquippedFlags()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) { continue; }
                slots[i].SetEquipped(false);
            }
        }
        #endregion

        #region Interfaces
        // Quest Evaluator
        public void CompleteObjective()
        {
            foreach (KeyItem keyItem in GetKeyItems())
            {
                foreach (QuestObjectivePair questObjectivePair in keyItem.GetQuestObjectivePairs())
                {
                    questList.value.CompleteObjective(questObjectivePair.quest, questObjectivePair.objective);
                }
            }
        }

        // Saving System
        [System.Serializable]
        struct SaveableActiveItem
        {
            public string inventoryItemID;
            public bool equipped;
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        SaveState ISaveable.CaptureState()
        {
            SaveableActiveItem[] slotsActiveItemStrings = new SaveableActiveItem[inventorySize];
            for (int i = 0; i < inventorySize; i++)
            {
                if (slots[i] != null)
                {
                    SaveableActiveItem saveableActiveItem = new SaveableActiveItem
                    {
                        inventoryItemID = slots[i].GetInventoryItem().GetItemID(),
                        equipped = slots[i].IsEquipped()
                    };
                    slotsActiveItemStrings[i] = saveableActiveItem;
                }
            }
            SaveState saveState = new SaveState(GetLoadPriority(), slotsActiveItemStrings);

            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            SaveableActiveItem[] slotsActiveItemStrings = (SaveableActiveItem[])saveState.GetState();

            for (int i = 0; i < inventorySize; i++)
            {
                if (string.IsNullOrEmpty(slotsActiveItemStrings[i].inventoryItemID)) { continue; }

                string inventoryItemID = slotsActiveItemStrings[i].inventoryItemID;
                ActiveInventoryItem activeInventoryItem = new ActiveInventoryItem(InventoryItem.GetFromID(inventoryItemID));
                activeInventoryItem.SetEquipped(slotsActiveItemStrings[i].equipped);

                slots[i] = activeInventoryItem;
            }

            knapsackUpdated?.Invoke();
        }
        #endregion
    }

}
