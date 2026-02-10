using System.Collections.Generic;
using UnityEngine;
using Frankie.Inventory;

namespace Frankie.Core
{
    public abstract class PredicateKnapsack : Predicate
    {
        [SerializeField][Tooltip("Optional, depending on implementation")] protected List<InventoryItem> inventoryItems = new();

        public abstract bool? Evaluate(PartyKnapsackConduit partyKnapsackConduit);
    }
}
