using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Inventory;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Has Item Predicate", menuName = "Predicates/Knapsack/Has Item")]
    public class HasItemPredicate : PredicateKnapsack
    {
        public override bool? Evaluate(PartyKnapsackConduit partyKnapsackConduit)
        {
            foreach (Knapsack knapsack in partyKnapsackConduit.GetKnapsacks())
            {
                // Match on ANY of the items present in parameters
                foreach (InventoryItem inventoryItem in inventoryItems)
                {
                    if (knapsack.HasItem(inventoryItem))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
