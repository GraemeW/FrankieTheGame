using Frankie.Combat;
using Frankie.Core;
using Frankie.Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class Inventory : MonoBehaviour, IPredicateEvaluator, ISaveable
    {
        // Tunables
        [SerializeField] int inventorySize = 16;

        // State
        [SerializeField] InventoryItem[] slots; // Serialized for test purposes

        // Static
        static string[] PREDICATES_ARRAY = { "HasInventoryItem" };

        // Events
        public event Action inventoryUpdated;

        private void Awake()
        {
            //slots = new InventoryItem[inventorySize];
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

        public bool HasItem(InventoryItem inventoryItem)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (object.ReferenceEquals(slots[i], inventoryItem))
                {
                    return true;
                }
            }
            return false;
        }

        public InventoryItem GetItemInSlot(int slot)
        {
            return slots[slot];
        }

        public bool AddToFirstEmptySlot(InventoryItem inventoryItem)
        {
            int slot = FindEmptySlot();
            if (slot < 0) { return false; }

            slots[slot] = inventoryItem;

            if (inventoryUpdated != null)
            {
                inventoryUpdated();
            }
            return true;
        }

        public bool AddItemToSlotOrFirstEmptySlot(InventoryItem inventoryItem, int slot)
        {
            if (AddItemToSlot(inventoryItem, slot)) { return true; }
            if (AddToFirstEmptySlot(inventoryItem)) { return true; }
            return false;
        }

        public void RemoveFromSlot(int slot)
        {
            slots[slot] = null;

            if (inventoryUpdated != null)
            {
                inventoryUpdated();
            }
        }

        public bool RemoveItem(InventoryItem inventoryItem, bool removeAll = true)
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

            if (inventoryUpdated != null)
            {
                inventoryUpdated();
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

        private bool AddItemToSlot(InventoryItem inventoryItem, int slot)
        {
            if (slots[slot] != null) { return false; }

            slots[slot] = inventoryItem;

            if (inventoryUpdated != null)
            {
                inventoryUpdated();
            }
            return true;
        }

        private List<int> FindSlotsWithItem(InventoryItem inventoryItem)
        {
            List<int> slotsWithItem = new List<int>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    slotsWithItem.Add(i);
                }
            }
            return slotsWithItem;
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
        object ISaveable.CaptureState()
        {
            string[] slotsStrings = new string[inventorySize];
            for (int i = 0; i < inventorySize; i++)
            {
                if (slots[i] != null)
                {
                    slotsStrings[i] = slots[i].GetItemID();
                }
            }
            return slotsStrings;
        }

        void ISaveable.RestoreState(object state)
        {
            string[] slotStrings = (string[])state;
            for (int i = 0; i < inventorySize; i++)
            {
                slots[i] = InventoryItem.GetFromID(slotStrings[i]);
            }

            if (inventoryUpdated != null)
            {
                inventoryUpdated();
            }
        }
        #endregion
    }

}
