using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LowDefMustard.GameStateModifiers;

namespace Frankie.Inventory
{
    [CreateAssetMenu(fileName = "New Has Item Predicate", menuName = "Predicates/Knapsack/Has Item", order = 5)]
    public class HasItemPredicate : PredicateKnapsack, IGameStateModifierPredicate
    {
        [SerializeField] private List<InventoryItem> inventoryItems = new();

        // Editor Field References
        internal const string inventoryItemsField = nameof(inventoryItems);
        
        public override bool? Evaluate(PartyKnapsackConduit partyKnapsackConduit)
        {
            if (partyKnapsackConduit == null) { return null; }
            return partyKnapsackConduit.GetKnapsacks().Any(knapsack => inventoryItems.Any(knapsack.HasItem));
        }

        public IEnumerable<GameStateModifier> GetGameStateModifiers() => inventoryItems.OfType<KeyItem>();
    }
}
