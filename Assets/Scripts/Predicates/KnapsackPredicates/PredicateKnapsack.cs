using Frankie.Inventory;

namespace Frankie.Core.Predicates
{
    public abstract class PredicateKnapsack : Predicate
    {
        public abstract bool? Evaluate(PartyKnapsackConduit partyKnapsackConduit);
    }
}
