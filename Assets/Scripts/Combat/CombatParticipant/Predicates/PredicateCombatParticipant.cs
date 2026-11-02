using LowDefMustard.Utils;

namespace Frankie.Combat
{
    public abstract class PredicateCombatParticipant : Predicate
    {
        public abstract bool? Evaluate(CombatParticipant combatParticipant);
    }
}
