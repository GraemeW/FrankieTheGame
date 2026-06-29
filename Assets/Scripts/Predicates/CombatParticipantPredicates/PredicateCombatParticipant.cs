using Frankie.Combat;

namespace Frankie.Core.Predicates
{
    public abstract class PredicateCombatParticipant : Predicate
    {
        public abstract bool? Evaluate(CombatParticipant combatParticipant);
    }
}
