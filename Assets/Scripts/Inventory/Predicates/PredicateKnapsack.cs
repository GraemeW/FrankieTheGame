using LowDefMustard.Utils;

namespace Frankie.Inventory
{
    public abstract class PredicateKnapsack : Predicate
    {
        public abstract bool? Evaluate(PartyKnapsackConduit partyKnapsackConduit);
    }
}
