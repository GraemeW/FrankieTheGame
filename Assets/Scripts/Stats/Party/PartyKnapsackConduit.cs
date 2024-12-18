using Frankie.Combat;
using Frankie.Core;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Party))]
    public class PartyKnapsackConduit : MonoBehaviour, IPredicateEvaluator
    {
        // State
        List<Knapsack> knapsacks = new List<Knapsack>();

        // Cached References
        Party party = null;

        // Events
        public event Action partyKnapsackUpdated;

        #region UnityMethods
        private void Awake()
        {
            party = GetComponent<Party>();
        }

        private void Start()
        {
            RefreshKnapsacks();
        }

        private void OnEnable()
        {
            party.partyUpdated += RefreshKnapsacks;
        }

        private void OnDisable()
        {
            party.partyUpdated -= RefreshKnapsacks;
        }

        #endregion

        #region PrivateMethods
        private void RefreshKnapsacks()
        {
            SubscribeToKnapsackEvents(false);
            knapsacks.Clear();
            foreach (BaseStats character in party.GetParty())
            {
                if (character.TryGetComponent(out Knapsack knapsack))
                {
                    knapsacks.Add(knapsack);
                }
            }
            SubscribeToKnapsackEvents(true);
        }

        private void SubscribeToKnapsackEvents(bool enable)
        {
            if (enable)
            {
                foreach (Knapsack knapsack in knapsacks)
                {
                    knapsack.knapsackUpdated += HandleKnapsackUpdates;
                }
            }
            else
            {
                foreach (Knapsack knapsack in knapsacks)
                {
                    knapsack.knapsackUpdated -= HandleKnapsackUpdates;
                }
            }
        }

        private void HandleKnapsackUpdates()
        {
            partyKnapsackUpdated?.Invoke();
        }

        private void RemoveItem(InventoryItem inventoryItem, bool removeAllItems)
        {
            bool itemRemoved = false;
            foreach (Knapsack knapsack in GetKnapsacks())
            {
                if (knapsack.RemoveItem(inventoryItem, true)) { itemRemoved = true; break; }
            }

            if (removeAllItems && itemRemoved) { RemoveItem(inventoryItem, true); } // Recursion until item not removed
        }
        #endregion

        #region PublicMethods
        public IEnumerable<Knapsack> GetKnapsacks()
        {
            return knapsacks;
        }

        public CombatParticipant AddToFirstEmptyPartySlot(InventoryItem inventoryItem)
        {
            // Returns character who received item on success,
            // Returns null on knapsacks full
            foreach (Knapsack knapsack in GetKnapsacks())
            {
                if (knapsack.AddToFirstEmptySlot(inventoryItem, true))
                {
                    return knapsack?.GetComponent<CombatParticipant>();
                }
            }
            return null;
        }

        public void RemoveSingleItem(InventoryItem inventoryItem)
        {
            RemoveItem(inventoryItem, false);
        }

        public void RemoveAllItems(InventoryItem inventoryItem)
        {
            RemoveItem(inventoryItem, true);
        }

        public int GetNumberOfFreeSlotsInParty()
        {
            int freeSlots = 0;
            foreach (Knapsack knapsack in knapsacks)
            {
                freeSlots += knapsack.GetNumberOfFreeSlots();
            }
            return freeSlots;
        }

        public bool HasFreeSpace()
        {
            return GetNumberOfFreeSlotsInParty() > 0;
        }

        // Predicate Evaluator
        public bool? Evaluate(Predicate predicate)
        {
            PredicateKnapsack predicateKnapsack = predicate as PredicateKnapsack;
            return predicateKnapsack != null ? predicateKnapsack.Evaluate(this) : null;
        }
        #endregion
    }
}
