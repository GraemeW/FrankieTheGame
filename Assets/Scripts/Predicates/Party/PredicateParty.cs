using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    public abstract class PredicateParty : Predicate
    {
        public abstract bool? Evaluate(PartyBehaviour partyBehaviour);
    }
}
