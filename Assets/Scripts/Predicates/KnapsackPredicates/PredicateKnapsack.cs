using Frankie.Inventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public abstract class PredicateKnapsack : Predicate
    {
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected InventoryItem[] inventoryItems = null;

        public abstract bool? Evaluate(Knapsack knapsack);
    }
}