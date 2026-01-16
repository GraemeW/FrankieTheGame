using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Combat;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Party))]
    public class PartyKnapsackConduit : MonoBehaviour, IPredicateEvaluator
    {
        // State
        private readonly List<Knapsack> knapsacks = new();

        // Cached References
        private Party party;

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
            knapsacks.Clear();
            foreach (BaseStats character in party.GetParty())
            {
                if (character.TryGetComponent(out Knapsack knapsack))
                {
                    knapsacks.Add(knapsack);
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

        public bool AddToFirstEmptyPartySlot(InventoryItem inventoryItem) => AddToFirstEmptyPartySlot(inventoryItem, out CombatParticipant receivingCharacter);
        
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
