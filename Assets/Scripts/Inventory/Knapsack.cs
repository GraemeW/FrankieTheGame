using Frankie.Combat;
using Frankie.Core;
using Frankie.Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class Knapsack : MonoBehaviour, IPredicateEvaluator, ISaveable
    {
        // Tunables
        [SerializeField] int inventorySize = 16;

        // State
        ActiveInventoryItem[] slots;

        // Static
        static string[] PREDICATES_ARRAY = { "HasInventoryItem" };

        // Events
        public event Action knapsackUpdated;

        private void Awake()
        {
            slots = new ActiveInventoryItem[inventorySize];
        }

        #region PublicFunctions
        public int GetSize()
        {
            return slots.Length;
        }

        public bool HasSpace()
        {
            return FindEmptySlot() >= 0;
        }

        public bool IsEmpty()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) { return false; }
            }
            return true;
        }

        public bool HasItem(InventoryItem inventoryItem)
        {
            for (int i = 0; i < slots.Length; i++)
            {
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

        public InventoryItem GetItemInSlot(int slot)
        {
            return slots[slot].GetInventoryItem();
        }

        public bool AddToFirstEmptySlot(InventoryItem inventoryItem, bool announceUpdate = true)
        {
            int slot = FindEmptySlot();
            if (slot < 0) { return false; }

            slots[slot] = new ActiveInventoryItem(inventoryItem);

            if (announceUpdate && knapsackUpdated != null)
            {
                knapsackUpdated.Invoke(); ;
            }
            return true;
        }

        public bool AddItemToSlotOrFirstEmptySlot(InventoryItem inventoryItem, int slot)
        {
            if (AddItemToSlot(inventoryItem, slot)) { return true; }
            if (AddToFirstEmptySlot(inventoryItem)) { return true; }
            return false;
        }

        public void RemoveFromSlot(int slot, bool announceUpdate = true)
        {
            slots[slot] = null;

            if (announceUpdate && knapsackUpdated != null)
            {
                knapsackUpdated.Invoke();
            }
        }

        public bool RemoveItem(InventoryItem inventoryItem, bool announceUpdate = true, bool removeAll = true)
        {
            List<int> slotsWithItem = FindSlotsWithItem(inventoryItem);
            if (slotsWithItem.Count == 0) { return false; }

            if (removeAll)
            {
                foreach (int slot in slotsWithItem)
                {
                    slots[slot] = null;
                }
            }
            else
            {
                // Remove only first instance
                slots[slotsWithItem[0]] = null;
            }

            if (announceUpdate && knapsackUpdated != null)
            {
                knapsackUpdated.Invoke();
            }
            return true;
        }

        public bool UseItemInSlot(int slot, CombatParticipant combatParticipant)
        {
            InventoryItem inventoryItem = GetItemInSlot(slot);
            ActionItem actionItem = inventoryItem as ActionItem;
            if (actionItem == null) { return false; }

            actionItem.Use(combatParticipant);
            if (actionItem.IsConsumable()) { RemoveFromSlot(slot); }
            return true;
        }

        public bool UseItem(InventoryItem inventoryItem, CombatParticipant combatParticipant)
        {
            // Uses the first instance of the item
            int slot = FindSlotWithItem(inventoryItem);
            if (slot < 0) { return false; }

            return UseItemInSlot(slot, combatParticipant);
        }

        public void MoveItem(int sourceSlot, Knapsack destinationKnapsack, int destinationSlot, bool announceUpdate = true)
        {
            InventoryItem swapItem = null;
            bool preserveSourceEquippedState = false;
            bool preserveDestinationEquippedState = false;

            // Check if swapping or only simple move
            if (this == destinationKnapsack) { if (slots[sourceSlot] != null && slots[sourceSlot].IsEquipped()) { preserveSourceEquippedState = true; } }
            if (destinationKnapsack.HasItemInSlot(destinationSlot))
            {
                swapItem = destinationKnapsack.GetItemInSlot(destinationSlot);
                if (this == destinationKnapsack) { if (slots[destinationSlot] != null && slots[destinationSlot].IsEquipped()) { preserveSourceEquippedState = true; } }
                destinationKnapsack.RemoveFromSlot(destinationSlot, false);
            }

            // Remove item from source
            InventoryItem sourceItem = GetItemInSlot(sourceSlot);
            RemoveFromSlot(sourceSlot, false);

            // Add item to destination
            destinationKnapsack.AddItemToSlot(sourceItem, destinationSlot, false);
            slots[destinationSlot].SetEquipped(preserveSourceEquippedState);

            // Complete swap if swapping
            if (swapItem != null)
            {
                AddItemToSlot(swapItem, sourceSlot, false);
                slots[sourceSlot].SetEquipped(preserveDestinationEquippedState);
            }

            // Re-order items in knapsack
            SquishItemsInKnapsack(true);
            if (this != destinationKnapsack)
            {
                destinationKnapsack.SquishItemsInKnapsack(true);
            }
        }
        #endregion

        #region PrivateFunctions
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

        private bool AddItemToSlot(InventoryItem inventoryItem, int slot, bool announceUpdate = true)
        {
            if (slots[slot] != null) { return false; }

            slots[slot] = new ActiveInventoryItem(inventoryItem); ;

            if (announceUpdate && knapsackUpdated != null)
            {
                knapsackUpdated.Invoke();
            }
            return true;
        }

        private List<int> FindSlotsWithItem(InventoryItem inventoryItem)
        {
            List<int> slotsWithItem = new List<int>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (object.ReferenceEquals(slots[i].GetInventoryItem(), inventoryItem))
                {
                    slotsWithItem.Add(i);
                }
            }
            return slotsWithItem;
        }

        private int FindSlotWithItem(InventoryItem inventoryItem)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (object.ReferenceEquals(slots[i].GetInventoryItem(), inventoryItem))
                {
                    return i;
                }
            }
            return -1;
        }

        public void SquishItemsInKnapsack(bool announceUpdate = true)
        {
            Queue<ActiveInventoryItem> knapsackQueue = new Queue<ActiveInventoryItem>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (HasItemInSlot(i))
                {
                    knapsackQueue.Enqueue(slots[i]);
                    RemoveFromSlot(i);
                }
            }

            int itemIndex = 0;
            while (knapsackQueue.Count > 0)
            {
                slots[itemIndex] = knapsackQueue.Dequeue();
                itemIndex++;
            }

            if (announceUpdate && knapsackUpdated != null)
            {
                knapsackUpdated.Invoke();
            }
        }
        #endregion

        #region Interfaces
        // Predicate Evaluator
        public bool? Evaluate(string predicate, string[] parameters)
        {
            string matchingPredicate = this.MatchToPredicates(predicate, PREDICATES_ARRAY);
            if (string.IsNullOrWhiteSpace(matchingPredicate)) { return null; }

            if (predicate == PREDICATES_ARRAY[0])
            {
                return PredicateEvaluateHasItem(parameters);
            }
            return null;
        }

        string IPredicateEvaluator.MatchToPredicatesTemplate()
        {
            // Not evaluated -> PredicateEvaluatorExtension
            return null;
        }

        private bool PredicateEvaluateHasItem(string[] parameters)
        {
            // Match on ANY of the items present in parameters
            foreach (string itemID in parameters)
            {
                if (HasItem(InventoryItem.GetFromID(itemID)))
                {
                    return true;
                }
            }
            return false;
        }

        // Saving System
        [System.Serializable]
        struct SaveableActiveItem
        {
            public string inventoryItemID;
            public bool equipped;
        }

        object ISaveable.CaptureState()
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

            return slotsActiveItemStrings;
        }

        void ISaveable.RestoreState(object state)
        {
            SaveableActiveItem[] slotsActiveItemStrings = (SaveableActiveItem[])state;

            for (int i = 0; i < inventorySize; i++)
            {
                if (string.IsNullOrEmpty(slotsActiveItemStrings[i].inventoryItemID)) { continue; }

                string inventoryItemID = slotsActiveItemStrings[i].inventoryItemID;
                ActiveInventoryItem activeInventoryItem = new ActiveInventoryItem(InventoryItem.GetFromID(inventoryItemID));
                activeInventoryItem.SetEquipped(slotsActiveItemStrings[i].equipped);

                slots[i] = activeInventoryItem;
            }

            if (knapsackUpdated != null)
            {
                knapsackUpdated();
            }
        }
        #endregion
    }

}
