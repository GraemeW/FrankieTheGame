using UnityEngine;
using Frankie.Inventory;

namespace Frankie.Core
{
    public abstract class PredicateKnapsack : Predicate
    {
        [SerializeField][Tooltip("Optional, depending on implementation")] protected InventoryItem[] inventoryItems = null;

        public abstract bool? Evaluate(PartyKnapsackConduit partyKnapsackConduit);
    }
}
