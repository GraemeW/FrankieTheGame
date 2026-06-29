using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Inventory;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Has Item Predicate", menuName = "Predicates/Knapsack/Has Item", order = 5)]
    public class HasItemPredicate : PredicateKnapsack
    {
        [SerializeField] private List<InventoryItem> inventoryItems = new();
        
        public override bool? Evaluate(PartyKnapsackConduit partyKnapsackConduit)
        {
            if (partyKnapsackConduit == null) { return null; }
            return partyKnapsackConduit.GetKnapsacks().Any(knapsack => inventoryItems.Any(knapsack.HasItem));
        }
    }
}
