using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Combat;
using Frankie.Core.Predicates;
using Frankie.Stats;

namespace Frankie.Inventory
{
    public class PartyKnapsackConduit : MonoBehaviour, IPredicateEvaluator
    {
        // State
        private readonly List<Knapsack> knapsacks = new();
        private readonly List<Equipment> equipments = new();

        #region UnityMethods
        private void OnEnable()
        {
            if (TryGetComponent(out Party party))
            {
                party.SubscribeToMembersAlteredUpdates(true , RefreshCache);
                RefreshCache(party.GetMembers());
            }
        }

        private void OnDisable()
        {
            if (TryGetComponent(out Party party)) { party.SubscribeToMembersAlteredUpdates(false , RefreshCache); }
        }

        #endregion

        #region PrivateMethods
        private void RefreshCache(List<BaseStats> members)
        {
            knapsacks.Clear();
            equipments.Clear();
            foreach (BaseStats character in members)
            {
                if (character.TryGetComponent(out Knapsack knapsack)) { knapsacks.Add(knapsack); }

                if (character.TryGetComponent(out Equipment equipment))
                {
                    equipment.ReconcileEquipment(true);
                    equipments.Add(equipment);
                }
            }
        }

        private void RemoveItem(InventoryItem inventoryItem, bool removeAllItems)
        {
            bool itemRemoved = GetKnapsacks().Any(knapsack => knapsack.RemoveItem(inventoryItem, true));
            if (removeAllItems && itemRemoved) { RemoveItem(inventoryItem, true); } // Recursion until item not removed
        }
        #endregion

        #region PublicMethods
        public IEnumerable<Knapsack> GetKnapsacks() => knapsacks;
        public int GetNumberOfFreeSlotsInParty() => knapsacks.Sum(knapsack => knapsack.GetNumberOfFreeSlots());
        public bool HasFreeSpace() => GetNumberOfFreeSlotsInParty() > 0;

        public bool AddToFirstEmptyPartySlot(InventoryItem inventoryItem) => AddToFirstEmptyPartySlot(inventoryItem, out CombatParticipant _);
        
        public bool AddToFirstEmptyPartySlot(InventoryItem inventoryItem, out CombatParticipant receivingCharacter)
        {
            // Returns character who received item on success,
            // Returns null on knapsacks full
            foreach (Knapsack knapsack in GetKnapsacks())
            {
                if (knapsack == null) { continue; }
                if (!knapsack.AddToFirstEmptySlot(inventoryItem, true)) continue;
                receivingCharacter = knapsack.GetComponent<CombatParticipant>();
                return true;
            }
            receivingCharacter = null;
            return false;
        }

        public void RemoveSingleItem(InventoryItem inventoryItem)
        {
            RemoveItem(inventoryItem, false);
        }

        public void RemoveAllItems(InventoryItem inventoryItem)
        {
            RemoveItem(inventoryItem, true);
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
