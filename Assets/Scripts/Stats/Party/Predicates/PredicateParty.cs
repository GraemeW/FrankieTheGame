using LowDefMustard.Utils;

namespace Frankie.Stats
{
    public abstract class PredicateParty : Predicate
    {
        public abstract bool? Evaluate(PartyBehaviour partyBehaviour);
    }
}
